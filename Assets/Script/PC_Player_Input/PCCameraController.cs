using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script điều khiển camera xoay theo chuột cho PC.
/// 
/// Cấu trúc hierarchy yêu cầu:
///   Player (gắn script này + PCPlayerMovement)
///   ├── Camera        ← Script xoay Pitch (nhìn lên/xuống) ở đây
///   └── Offset
///
/// Cách hoạt động:
///   - Mouse X (ngang) → Xoay Player transform (Yaw) → Camera theo vì là con
///   - Mouse Y (dọc)   → Xoay Camera child (Pitch) → Chỉ Camera xoay, Player không đổi
///
/// Input:
///   - Mouse X/Y          : Xoay camera (ngang/dọc)
///   - Mouse ScrollWheel  : Zoom vào/ra (nếu bật)
///   - Chuột phải (giữ)   : Xoay camera tự do (nếu chế độ requireRightClick bật)
///   - Phím Escape         : Mở/Đóng khóa chuột (toggle cursor lock)
/// </summary>
public class PCCameraController : MonoBehaviour
{
    [Header("Tham chiếu Camera")]
    [Tooltip("Transform của Camera con (child của Player). " +
             "Nếu để trống, script sẽ tự tìm Camera con trong children.")]
    [SerializeField]
    private Transform cameraChild;

    [Header("Cài đặt xoay")]
    [Tooltip("Độ nhạy chuột. KHÔNG nhân Time.deltaTime — giá trị hợp lý: 0.1–2.0")]
    public float mouseSensitivity = 0.5f;

    [Tooltip("Góc nhìn xuống tối đa (độ) - ví dụ: 70")]
    public float bottomClamp = 70.0f;

    [Tooltip("Góc nhìn lên tối đa (độ) - ví dụ: 70")]
    public float topClamp = 70.0f;

    [Tooltip("Ngưỡng tối thiểu (sqrMagnitude) để chuột được nhận — để 0 là tắt deadzone hoàn toàn. " +
             "Chỉ tăng nếu hardware bị jitter cực mạnh.")]
    public float deadZoneThreshold = 0f;

    [Tooltip("Tốc độ nội suy xoay camera (Slerp). Giá trị hợp lý: 10–20. Quá cao thì mất hiệu ứng mượt.")]
    public float rotationLerpSpeed = 12.0f;

    [Header("Chế độ hoạt động")]
    [Tooltip("Nếu true, phải giữ chuột phải mới xoay camera (phù hợp với game có UI cần chuột). " +
             "Nếu false, camera xoay liên tục theo chuột (phù hợp FPS/TPS).")]
    public bool requireRightClick = false;

    [Tooltip("Có khóa cursor khi bắt đầu game không")]
    public bool lockCursorOnStart = true;

    [Tooltip("Khóa camera không cho xoay")]
    public bool lockCameraRotation = false;

    [Header("Zoom (Cuộn chuột)")]
    [Tooltip("Bật zoom bằng cuộn chuột")]
    public bool enableZoom = false;

    [Tooltip("Tốc độ zoom")]
    public float zoomSpeed = 2.0f;

    [Tooltip("Khoảng cách zoom gần nhất")]
    public float minZoomDistance = 1.0f;

    [Tooltip("Khoảng cách zoom xa nhất")]
    public float maxZoomDistance = 15.0f;

    [Header("PC Interaction")]
    [Tooltip("Cho phép raycast từ giữa màn hình để tương tác với PCInteractorObject")]
    public bool enableInteraction = true;

    [Tooltip("Phím tương tác với object đang được nhìn vào")]
    public KeyCode interactionKey = KeyCode.F;

    [Tooltip("Khoảng cách raycast tương tác tối đa")]
    public float interactionDistance = 5.0f;

    [Tooltip("Layer mask dùng cho raycast tương tác")]
    public LayerMask interactionMask = Physics.DefaultRaycastLayers;

    // ---- Trạng thái nội bộ ----
    private float _yaw;   // Góc xoay ngang Player (trục Y)
    private float _pitch; // Góc xoay dọc Camera (trục X)
    private bool _isCursorLocked = false;
    private bool _menuOpen = false;

    #region Getters công khai
    /// <summary>
    /// Góc xoay ngang hiện tại của camera (Yaw - áp dụng lên Player)
    /// </summary>
    public float CurrentYaw => _yaw;

    /// <summary>
    /// Góc xoay dọc hiện tại của camera (Pitch - áp dụng lên Camera child)
    /// </summary>
    public float CurrentPitch => _pitch;

    /// <summary>
    /// Cursor có đang bị khóa không
    /// </summary>
    public bool IsCursorLocked => _isCursorLocked;
    #endregion

    private void Awake()
    {
        // Tự động tìm Camera con nếu chưa gán
        if (cameraChild == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cameraChild = cam.transform;
            }
        }

        // Khởi tạo góc xoay ban đầu từ transform hiện tại
        _yaw = transform.eulerAngles.y;
        _pitch = cameraChild != null ? cameraChild.localEulerAngles.x : 0f;

        // Chuẩn hóa pitch (Unity trả về 0-360, cần chuyển về -180 to 180)
        if (_pitch > 180f) _pitch -= 360f;
    }

    private void Start()
    {
        if (lockCursorOnStart && !_menuOpen)
            LockCursor();
    }

    /// <summary>
    /// Gọi khi UI menu mở/đóng. Mở menu → hiện chuột + tắt xoay camera.
    /// Đóng menu → khoá chuột + bật xoay camera trở lại.
    /// </summary>
    public void SetMenuMode(bool open)
    {
        _menuOpen = open;
        if (open)
            UnlockCursor();
        else
            LockCursor();
    }

    private void Update()
    {
        // Toggle khóa chuột bằng Escape
        HandleCursorLockToggle();

        // Đọc input chuột và tính góc xoay
        HandleMouseInput();

        // Tương tác PC bằng raycast từ camera
        HandleInteractionInput();
    }

    private void LateUpdate()
    {
        // Áp dụng xoay camera (LateUpdate để đảm bảo sau khi nhân vật di chuyển)
        ApplyCameraRotation();

        // Zoom nếu được bật
        if (enableZoom)
        {
            HandleZoom();
        }
    }

    /// <summary>
    /// Xử lý khóa/mở cursor:
    ///   - Click chuột trái vào game → Lock cursor (bắt đầu điều khiển camera)
    ///   - Escape → Unlock cursor (thả chuột ra, ví dụ để tương tác UI hoặc Editor)
    /// 
    /// Pattern chuẩn FPS: Click = lock, Escape = unlock.
    /// Giải quyết lỗi: khi phóng to màn hình hoặc alt-tab, 
    /// game mất focus → cursor bị unlock → click lại vào game sẽ tự lock lại.
    /// </summary>
    private void HandleCursorLockToggle()
    {
        // Menu đang mở → chuột tự do để dùng UI, không auto-lock khi click
        if (_menuOpen) return;

        // Escape → Unlock cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
            return;
        }

        // Click chuột trái khi cursor đang unlock → Lock cursor lại
        if (!_isCursorLocked && Input.GetMouseButtonDown(0))
        {
            LockCursor();
        }
    }

    /// <summary>
    /// Khi game được focus lại (sau alt-tab, phóng to, thu nhỏ) → tự động lock cursor.
    /// Giải quyết lỗi: phóng to màn hình xong WASD không hoạt động vì game mất focus.
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && lockCursorOnStart && !_menuOpen)
            LockCursor();
    }

    /// <summary>
    /// Đọc input chuột và tính toán góc xoay mới.
    /// </summary>
    private void HandleMouseInput()
    {
        if (lockCameraRotation || _menuOpen) return;

        // Nếu chế độ requireRightClick bật, chỉ xoay khi giữ chuột phải
        if (requireRightClick && !Input.GetMouseButton(1))
        {
            return;
        }

        // GetAxisRaw: không có Unity smoothing — phản hồi tức thì, không bị delay khi bắt đầu di chuột
        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        Vector2 lookDelta = new Vector2(mouseX, mouseY);

        // Dead zone: chỉ bỏ qua nếu threshold > 0 VÀ delta quá nhỏ
        if (deadZoneThreshold <= 0f || lookDelta.sqrMagnitude >= deadZoneThreshold)
        {
            _yaw   += lookDelta.x * mouseSensitivity;
            _pitch -= lookDelta.y * mouseSensitivity;
        }

        // Clamp góc xoay
        _yaw = ClampAngle360(_yaw);
        _pitch = Mathf.Clamp(_pitch, -topClamp, bottomClamp);
    }

    /// <summary>
    /// Bấm phím tương tác để raycast từ giữa màn hình và kích hoạt PCInteractorObject đang nhìn vào.
    /// </summary>
    private void HandleInteractionInput()
    {
        if (!enableInteraction || !Input.GetKeyDown(interactionKey))
            return;

        Camera interactionCamera = GetInteractionCamera();
        if (interactionCamera == null)
            return;

        PCInteractorObject heldObject = PCInteractorManager.Instance?.CurrentlyHeldObject;
        if (heldObject != null && heldObject.TryInteract(interactionCamera))
            return;

        Ray ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactionMask, QueryTriggerInteraction.Ignore))
            return;

        PCInteractorObject interactor = hit.collider.GetComponentInParent<PCInteractorObject>();
        if (interactor != null)
        {
            interactor.TryInteract(interactionCamera);
        }
    }

    /// <summary>
    /// Áp dụng góc xoay:
    ///   - Yaw (ngang) → xoay Player transform (trục Y)
    ///   - Pitch (dọc) → xoay Camera child (trục X)
    /// 
    /// Vì Camera là con của Player, khi Player xoay ngang thì Camera tự động xoay theo.
    /// Camera chỉ cần xoay thêm trục X (nhìn lên/xuống) bằng localRotation.
    /// </summary>
    private void ApplyCameraRotation()
    {
        // Gán trực tiếp — mouse input phải phản hồi tức thì, không lag.
        // rotationLerpSpeed giữ lại trong Inspector nhưng không dùng ở đây nữa.
        transform.rotation        = Quaternion.Euler(0f, _yaw, 0f);

        if (cameraChild != null)
            cameraChild.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    /// <summary>
    /// Zoom camera bằng cuộn chuột (scroll wheel).
    /// Hoạt động bằng cách thay đổi localPosition.z của camera child.
    /// </summary>
    private void HandleZoom()
    {
        if (cameraChild == null) return;

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            Vector3 localPos = cameraChild.localPosition;
            float newZ = localPos.z + scrollInput * zoomSpeed;
            newZ = Mathf.Clamp(newZ, -maxZoomDistance, -minZoomDistance);
            cameraChild.localPosition = new Vector3(localPos.x, localPos.y, newZ);
        }
    }

    private Camera GetInteractionCamera()
    {
        if (cameraChild != null)
        {
            Camera childCamera = cameraChild.GetComponent<Camera>();
            if (childCamera != null)
                return childCamera;
        }

        return Camera.main;
    }

    #region Cursor Management

    /// <summary>
    /// Khóa và ẩn cursor
    /// </summary>
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _isCursorLocked = true;
    }

    /// <summary>
    /// Mở khóa và hiện cursor
    /// </summary>
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _isCursorLocked = false;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Clamp góc về phạm vi 0-360 độ
    /// </summary>
    private static float ClampAngle360(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    /// <summary>
    /// Đặt góc xoay camera trực tiếp (dùng khi cần reset camera hoặc teleport)
    /// </summary>
    /// <param name="yaw">Góc ngang (áp dụng lên Player)</param>
    /// <param name="pitch">Góc dọc (áp dụng lên Camera child)</param>
    public void SetCameraAngles(float yaw, float pitch)
    {
        _yaw = yaw;
        _pitch = Mathf.Clamp(pitch, -topClamp, bottomClamp);
    }

    /// <summary>
    /// Reset camera về hướng mặc định (nhìn thẳng)
    /// </summary>
    public void ResetCamera()
    {
        _yaw = transform.eulerAngles.y;
        _pitch = 0f;
    }

    #endregion
}
