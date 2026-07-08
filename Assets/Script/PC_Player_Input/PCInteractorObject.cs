using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// PC interaction gắn trực tiếp lên object.
/// Khi player nhìn vào object và bấm phím tương tác, script sẽ tự gọi hành vi phù hợp
/// dựa trên component đi kèm: CarPaintCan, WheelItem hoặc WheelSocket.
/// </summary>
public class PCInteractorObject : MonoBehaviour
{
    public enum InteractionType
    {
        AutoDetect,
        CarPaintCan,
        WheelItem,
        WheelSocket,
        BrakeItem,
        BrakeSocket
    }

    [Header("PC Interaction")]
    [SerializeField] private InteractionType interactionType = InteractionType.AutoDetect;
    [SerializeField] private bool allowDirectInput = false;
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float maxInteractDistance = 3f;
    [SerializeField] private LayerMask raycastMask = Physics.DefaultRaycastLayers;
    [SerializeField] private bool requireCenterRaycast = true;

    [Header("Pickup Settings")]
    [SerializeField] private bool allowPickupWithLeftClick = true;
    [SerializeField] private int pickupMouseButton = 0;
    [SerializeField] private float holdDistance = 2.0f;
    [SerializeField] private Vector3 holdPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 holdRotationOffsetEuler = Vector3.zero;
    [SerializeField] private float holdMoveSpeed = 18.0f;
    [SerializeField] private float holdRotateSpeed = 18.0f;

    [Header("Wheel Settings")]
    [SerializeField] private bool ignoreWheelSnapDistanceForPC = true;
    [SerializeField] private bool allowSocketToggleWhenEmpty = true;

    [Header("Ghost Preview")]
    [Tooltip("Material bán trong suốt dùng cho ghost (Transparent/Unlit). Để trống = tắt ghost.")]
    [SerializeField] private Material ghostMaterial;
    [Tooltip("Khoảng cách hiển thị ghost (nên lớn hơn snapDistance của WheelItem).")]
    [SerializeField] private float ghostPreviewDistance = 1.2f;

    [Header("Events")]
    public UnityEvent onInteracted;

    private CarPaintCan _paintCan;
    private WheelItem _wheelItem;
    private WheelSocket _wheelSocket;
    private BrakeItem _brakeItem;
    private BrakeSocket _brakeSocket;
    private InteractionType _resolvedInteractionType;
    private Collider[] _cachedColliders;
    private Rigidbody _rb;
    private bool _isHeld;
    private bool _originalUseGravity;
    private bool _originalIsKinematic;
    private Transform _holdCameraTransform;
    private GameObject _ghostRoot;
    private WheelSocket _currentPreviewSocket;
    private BrakeSocket _currentPreviewBrakeSocket;
    private static Material _runtimeGhostMaterial;

    public Material GhostMaterial => ghostMaterial;
    public float GhostPreviewDistance => ghostPreviewDistance;

    private void Awake()
    {
        _paintCan = GetComponent<CarPaintCan>();
        _wheelItem = GetComponent<WheelItem>();
        _wheelSocket = GetComponent<WheelSocket>();
        _brakeItem = GetComponent<BrakeItem>();
        _brakeSocket = GetComponent<BrakeSocket>();
        _rb = GetComponent<Rigidbody>();
        _cachedColliders = GetComponentsInChildren<Collider>(true);

        ResolveInteractionType();

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main;

        if (requireCenterRaycast && _cachedColliders.Length == 0)
        {
            Debug.LogWarning($"[PCInteractorObject: {name}] Không có Collider để raycast tương tác.", this);
        }
    }

    private void Update()
    {
        HandlePickupInput();

        if (!allowDirectInput)
            return;

        if (!Input.GetKeyDown(interactKey))
            return;

        TryInteract();
    }

    private void LateUpdate()
    {
        if (_isHeld)
        {
            UpdateHeldObject();
        }
    }

    private void OnDisable()
    {
        if (_isHeld)
        {
            ReleaseHeldObject();
        }
    }

    private void HandlePickupInput()
    {
        if (!allowPickupWithLeftClick || !Input.GetMouseButtonDown(pickupMouseButton))
            return;

        if (_isHeld)
        {
            ReleaseHeldObject();
            return;
        }

        Camera cam = GetInteractionCamera(null);
        if (cam == null || !CanInteractFromPlayerView(cam))
            return;

        PickupObject(cam);
    }

    private void ResolveInteractionType()
    {
        if (interactionType != InteractionType.AutoDetect)
        {
            _resolvedInteractionType = interactionType;
            return;
        }

        if (_paintCan != null)
        {
            _resolvedInteractionType = InteractionType.CarPaintCan;
            return;
        }

        if (_wheelItem != null)
        {
            _resolvedInteractionType = InteractionType.WheelItem;
            return;
        }

        if (_wheelSocket != null)
        {
            _resolvedInteractionType = InteractionType.WheelSocket;
            return;
        }

        if (_brakeItem != null)
        {
            _resolvedInteractionType = InteractionType.BrakeItem;
            return;
        }

        if (_brakeSocket != null)
        {
            _resolvedInteractionType = InteractionType.BrakeSocket;
            return;
        }

        Debug.LogWarning($"[PCInteractorObject: {name}] Không tìm thấy component tương thích để AutoDetect.", this);
        enabled = false;
    }

    public bool TryInteract(Camera sourceCamera = null)
    {
        if (!enabled || !gameObject.activeInHierarchy)
            return false;

        if (!CanInteractFromPlayerView(sourceCamera))
            return false;

        if (!ExecuteInteraction())
            return false;

        onInteracted?.Invoke();
        return true;
    }

    public bool TryTogglePickup(Camera sourceCamera = null)
    {
        if (!allowPickupWithLeftClick)
            return false;

        if (_isHeld)
        {
            ReleaseHeldObject();
            return true;
        }

        Camera cam = GetInteractionCamera(sourceCamera);
        if (cam == null || !CanInteractFromPlayerView(cam))
            return false;

        return PickupObject(cam);
    }

    public void ConfigureGhost(Material material, float previewDistance)
    {
        ghostMaterial = material;
        ghostPreviewDistance = previewDistance;
    }

    private bool CanInteractFromPlayerView(Camera sourceCamera)
    {
        Camera cam = GetInteractionCamera(sourceCamera);
        if (cam == null)
            return false;

        if (GetDistanceToCamera(cam.transform.position) > maxInteractDistance)
            return false;

        // While an object is held, its colliders are disabled to avoid physics overlap impulses.
        // Let the held object receive F-key interaction without requiring a center raycast hit.
        if (_isHeld)
            return true;

        if (!requireCenterRaycast)
            return true;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, maxInteractDistance, raycastMask, QueryTriggerInteraction.Ignore))
            return false;

        PCInteractorObject hitInteractor = hit.collider.GetComponentInParent<PCInteractorObject>();
        return hitInteractor == this;
    }

    private Camera GetInteractionCamera(Camera fallbackCamera)
    {
        if (fallbackCamera != null)
            return fallbackCamera;

        if (playerCamera != null)
            return playerCamera;

        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogWarning($"[PCInteractorObject: {name}] Không tìm thấy Camera để tương tác.", this);
        }

        return playerCamera;
    }

    private bool PickupObject(Camera sourceCamera)
    {
        if (_rb == null)
        {
            Debug.LogWarning($"[PCInteractorObject: {name}] Không có Rigidbody để nhấc vật.", this);
            return false;
        }

        if (PCInteractorManager.Instance != null)
        {
            if (!PCInteractorManager.Instance.TryRegisterHeldObject(this))
            {
                // Không thể nhấc vì đang cầm vật khác
                return false;
            }
        }
        else
        {
            Debug.LogWarning($"[PCInteractorObject: {name}] Không tìm thấy PCInteractorManager trong scene. Cần tạo 1 GameObject chứa script PCInteractorManager!");
        }

        // Đảm bảo nếu vật thể là WheelItem và đang gắn vào xe thì phải tháo ra trước khi nhấc
        if (_resolvedInteractionType == InteractionType.WheelItem && _wheelItem != null)
        {
            if (_wheelItem.IsAttached)
            {
                _wheelItem.Detach();
            }
        }
        else if (_resolvedInteractionType == InteractionType.BrakeItem && _brakeItem != null)
        {
            if (_brakeItem.IsAttached)
            {
                _brakeItem.Detach();
            }
        }

        _originalUseGravity = _rb.useGravity;
        _originalIsKinematic = _rb.isKinematic;

        _rb.useGravity = false;
        _rb.isKinematic = true;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        // Tắt collider trong lúc cầm để physics engine không tích lũy overlap với xe
        // → tránh bị văng khi thả ra gần collider của xe (depenetration impulse)
        foreach (var col in _cachedColliders)
            if (col != null) col.enabled = false;

        if (_resolvedInteractionType == InteractionType.WheelItem || _resolvedInteractionType == InteractionType.BrakeItem)
            CreateGhost();

        _holdCameraTransform = sourceCamera.transform;
        _isHeld = true;
        return true;
    }

    private void ReleaseHeldObject(bool trySnapToSocket = true)
    {
        if (_rb == null)
            return;

        _isHeld = false;
        _holdCameraTransform = null;

        WheelSocket previewWheelSocket = _currentPreviewSocket;
        BrakeSocket previewBrakeSocket = _currentPreviewBrakeSocket;
        DestroyGhost();

        // Bật lại collider TRƯỚC khi restore physics để không có overlap tích lũy → không văng
        foreach (var col in _cachedColliders)
            if (col != null) col.enabled = true;

        _rb.isKinematic = _originalIsKinematic;
        _rb.useGravity = _originalUseGravity;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        if (PCInteractorManager.Instance != null)
        {
            PCInteractorManager.Instance.UnregisterHeldObject(this);
        }

        if (trySnapToSocket && _resolvedInteractionType == InteractionType.WheelItem && _wheelItem != null)
        {
            if (!_wheelItem.IsAttached)
            {
                // Nếu ghost đang hiện → snap thẳng vào socket đó luôn
                if (previewWheelSocket != null)
                    previewWheelSocket.AttachWheel(_wheelItem);
                else
                    _wheelItem.TryAttachNearestSocket(false);
            }
        }
        else if (trySnapToSocket && _resolvedInteractionType == InteractionType.BrakeItem && _brakeItem != null)
        {
            if (!_brakeItem.IsAttached)
            {
                if (previewBrakeSocket != null)
                    previewBrakeSocket.AttachBrake(_brakeItem);
                else
                    _brakeItem.TryAttachNearestSocket(false);
            }
        }
        _currentPreviewSocket = null;
        _currentPreviewBrakeSocket = null;
    }

    private void UpdateHeldObject()
    {
        if (_rb == null || _holdCameraTransform == null)
        {
            ReleaseHeldObject();
            return;
        }

        Vector3 targetPosition =
            _holdCameraTransform.position +
            _holdCameraTransform.forward * holdDistance +
            _holdCameraTransform.TransformVector(holdPositionOffset);

        Quaternion targetRotation =
            _holdCameraTransform.rotation *
            Quaternion.Euler(holdRotationOffsetEuler);

        Vector3 nextPosition = Vector3.Lerp(
            transform.position,
            targetPosition,
            holdMoveSpeed * Time.deltaTime
        );

        Quaternion nextRotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            holdRotateSpeed * Time.deltaTime
        );

        // Sử dụng transform.position/rotation trong LateUpdate thay vì _rb.MovePosition trong FixedUpdate
        // Điều này giúp loại bỏ triệt để hiện tượng giật giật (jitter) do chênh lệch giữa frame rate và physics step
        transform.position = nextPosition;
        transform.rotation = nextRotation;

        UpdateGhostPreview();
    }

    private void UpdateGhostPreview()
    {
        if (_ghostRoot == null) return;

        if (_resolvedInteractionType == InteractionType.WheelItem && _wheelItem != null)
        {
            _currentPreviewSocket = _wheelItem.FindNearestAvailableSocket(ghostPreviewDistance);
            if (_currentPreviewSocket != null)
            {
                _ghostRoot.SetActive(true);
                _ghostRoot.transform.position = _currentPreviewSocket.transform.position;
                _ghostRoot.transform.rotation = _currentPreviewSocket.transform.rotation * _currentPreviewSocket.GetAttachRotation();
                _ghostRoot.transform.localScale = transform.lossyScale;
            }
            else
            {
                _ghostRoot.SetActive(false);
            }
        }
        else if (_resolvedInteractionType == InteractionType.BrakeItem && _brakeItem != null)
        {
            _currentPreviewBrakeSocket = _brakeItem.FindNearestAvailableSocket(ghostPreviewDistance);
            if (_currentPreviewBrakeSocket != null)
            {
                _ghostRoot.SetActive(true);
                _ghostRoot.transform.position = _currentPreviewBrakeSocket.transform.position;
                _ghostRoot.transform.rotation = _currentPreviewBrakeSocket.transform.rotation * _currentPreviewBrakeSocket.GetAttachRotation();
                _ghostRoot.transform.localScale = transform.lossyScale;
            }
            else
            {
                _ghostRoot.SetActive(false);
            }
        }
    }

    private void CreateGhost()
    {
        Material previewMaterial = ghostMaterial != null ? ghostMaterial : GetRuntimeGhostMaterial();
        if (previewMaterial == null) return;
        DestroyGhost();

        _ghostRoot = new GameObject($"[Ghost]{name}");

        foreach (MeshFilter mf in GetComponentsInChildren<MeshFilter>())
        {
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr == null) continue;

            GameObject part = new GameObject(mf.name);
            part.transform.SetParent(_ghostRoot.transform, false);
            // Giữ nguyên offset của từng mesh con so với gốc bánh xe
            part.transform.localPosition = transform.InverseTransformPoint(mf.transform.position);
            part.transform.localRotation = Quaternion.Inverse(transform.rotation) * mf.transform.rotation;
            part.transform.localScale = Vector3.one;

            part.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;

            MeshRenderer ghostMR = part.AddComponent<MeshRenderer>();
            Material[] slots = new Material[mr.sharedMaterials.Length];
            for (int i = 0; i < slots.Length; i++) slots[i] = previewMaterial;
            ghostMR.sharedMaterials = slots;
        }

        _ghostRoot.SetActive(false);
    }

    private static Material GetRuntimeGhostMaterial()
    {
        if (_runtimeGhostMaterial != null)
            return _runtimeGhostMaterial;

        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Unlit/Color") ??
            Shader.Find("Standard");

        if (shader == null)
            return null;

        _runtimeGhostMaterial = new Material(shader)
        {
            name = "Runtime Ghost Preview Material",
            color = new Color(0f, 0.8f, 1f, 0.35f)
        };

        if (_runtimeGhostMaterial.HasProperty("_BaseColor"))
            _runtimeGhostMaterial.SetColor("_BaseColor", new Color(0f, 0.8f, 1f, 0.35f));

        if (_runtimeGhostMaterial.HasProperty("_Surface"))
            _runtimeGhostMaterial.SetFloat("_Surface", 1f);

        _runtimeGhostMaterial.renderQueue = 3000;
        return _runtimeGhostMaterial;
    }

    private void DestroyGhost()
    {
        if (_ghostRoot != null)
        {
            Destroy(_ghostRoot);
            _ghostRoot = null;
        }
        _currentPreviewSocket = null;
        _currentPreviewBrakeSocket = null;
    }

    private float GetDistanceToCamera(Vector3 cameraPosition)
    {
        if (_cachedColliders == null || _cachedColliders.Length == 0)
            return Vector3.Distance(cameraPosition, transform.position);

        float bestDistance = float.PositiveInfinity;

        foreach (Collider col in _cachedColliders)
        {
            if (col == null || !col.enabled)
                continue;

            Vector3 closestPoint = col.ClosestPoint(cameraPosition);
            float distance = Vector3.Distance(cameraPosition, closestPoint);
            if (distance < bestDistance)
                bestDistance = distance;
        }

        return float.IsInfinity(bestDistance)
            ? Vector3.Distance(cameraPosition, transform.position)
            : bestDistance;
    }

    private bool ExecuteInteraction()
    {
        switch (_resolvedInteractionType)
        {
            case InteractionType.CarPaintCan:
                if (_paintCan == null)
                    return false;

                _paintCan.ApplyPaint();
                return true;

            case InteractionType.WheelItem:
                if (_wheelItem == null)
                    return false;

                // Đang cầm trên tay → F = thả xuống (không snap về socket)
                if (_isHeld)
                {
                    ReleaseHeldObject(trySnapToSocket: false);
                    return true;
                }

                bool stateChanged = _wheelItem.ToggleAttachState(ignoreWheelSnapDistanceForPC);
                if (stateChanged && _isHeld && _wheelItem.IsAttached)
                {
                    // Tự động thả tay nếu gắn thành công
                    ReleaseHeldObject();
                    if (_rb != null)
                    {
                        _rb.isKinematic = true;
                        _rb.useGravity = false;
                    }
                }
                return stateChanged;

            case InteractionType.WheelSocket:
                if (_wheelSocket == null)
                    return false;

                return _wheelSocket.ToggleAttachState(allowSocketToggleWhenEmpty);

            case InteractionType.BrakeItem:
                if (_brakeItem == null)
                    return false;

                if (_isHeld)
                {
                    ReleaseHeldObject(trySnapToSocket: false);
                    return true;
                }

                return _brakeItem.ToggleAttachState(ignoreWheelSnapDistanceForPC);

            case InteractionType.BrakeSocket:
                if (_brakeSocket == null)
                    return false;

                return _brakeSocket.ToggleAttachState(allowSocketToggleWhenEmpty);

            default:
                return false;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.75f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, maxInteractDistance);
    }
#endif
}
