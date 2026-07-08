using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gán vào Wheel_FrontLeft (cha).
/// Dùng Physics.OverlapSphere với layer "Wheels" để detect WheelItem đang được cầm.
/// Hỗ trợ chỉ định vị trí attach tùy chỉnh và bánh xe mặc định ban đầu.
/// </summary>
public class WheelSocket : MonoBehaviour
{
    // ─── Cấu hình ────────────────────────────────────────────────────────────

    public enum WheelSide { Left, Right }

    [Header("Side (tự detect từ tên, có thể override)")]
    [SerializeField] private WheelSide side;

    [Header("Rotation theo Side")]
    [SerializeField] private float rightSideRotationY = 0f;
    [SerializeField] private float leftSideRotationY  = 180f;

    [Header("Attach Position")]
    [Tooltip("Vị trí localPosition của bánh xe khi gắn vào socket")]
    [SerializeField] private Vector3 attachLocalPosition = Vector3.zero;

    [Tooltip("Gán Transform nếu muốn lấy vị trí từ điểm cụ thể. Ưu tiên hơn attachLocalPosition.")]
    [SerializeField] private Transform attachPoint;

    [Header("Initial Wheel")]
    [Tooltip("WheelItem đã gắn sẵn từ đầu game. Gán bánh xe mặc định vào đây.")]
    [SerializeField] private WheelItem initialWheel;

    [Header("Snap Settings")]
    [Tooltip("Bán kính (m) để detect WheelItem đang được cầm")]
    [SerializeField] private float snapRadius = 0.2f;

    [Header("Events")]
    public UnityEvent<WheelItem> onWheelAttached;
    public UnityEvent<WheelItem> onWheelDetached;

    [Header("Debug (Read Only)")]
    [SerializeField] private WheelItem attachedWheel;

    // ─── Private ─────────────────────────────────────────────────────────────

    private int wheelLayer = -1;

    // ─── Properties ──────────────────────────────────────────────────────────

    public WheelSide Side          => side;
    public WheelItem AttachedWheel => attachedWheel;
    public bool      HasWheel      => attachedWheel != null;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        AutoDetectSide();
        wheelLayer = LayerMask.GetMask("Wheels");

        if (wheelLayer == 0)
            Debug.LogWarning($"[WheelSocket: {name}] Không tìm thấy layer 'Wheels'. " +
                             "Vào Project Settings → Tags and Layers để thêm.");
    }

    private void Start()
    {
        // Trong garage: loadout (qua CarLoadoutSlot) là nguồn sự thật. KIỂM TRA TRƯỚC cả attachedWheel
        // — vì scene có thể baked sẵn attachedWheel/child, nếu check attachedWheel trước sẽ return sớm
        // và bỏ qua việc xóa. Loadout KHÔNG có bánh cho socket này → tự XÓA SẠCH child.
        if (GarageSaveManager.Instance != null)
        {
            var loadoutSlot = GetComponentInParent<CarLoadoutSlot>();
            if (loadoutSlot != null && !loadoutSlot.LoadoutHasWheelFor(this))
                ClearAllChildren();
            return;
        }

        if (attachedWheel != null)
            return;

        // Scene khác (không có GarageSaveManager): attach bánh ban đầu như cũ.
        if (initialWheel != null)
        {
            AttachWheel(initialWheel);
            Debug.Log($"[WheelSocket: {name}] Initial wheel → {initialWheel.name}");
        }
    }

    private void Update()
    {
        CheckForNearbyGrabbedWheel();
    }

    public void SetInitialWheel(WheelItem wheel)
    {
        initialWheel = wheel;

        if (Application.isPlaying && wheel != null && attachedWheel != wheel)
            AttachWheel(wheel);
    }

    // ─── Auto Detect Side ─────────────────────────────────────────────────────

    private void AutoDetectSide()
    {
        string n = gameObject.name.ToLower();

        if (n.Contains("left"))
            side = WheelSide.Left;
        else if (n.Contains("right"))
            side = WheelSide.Right;
        else
        {
            side = transform.localPosition.x < 0f ? WheelSide.Left : WheelSide.Right;
            Debug.LogWarning($"[WheelSocket: {name}] Không detect được Left/Right từ tên, " +
                             $"dùng localPosition.x → {side}");
        }

        Debug.Log($"[WheelSocket: {name}] Side = {side} | AttachRotation Y = {GetAttachRotation().eulerAngles.y}");
    }

    // ─── Core: OverlapSphere check ────────────────────────────────────────────

    private void CheckForNearbyGrabbedWheel()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, snapRadius, wheelLayer);

        foreach (Collider hit in hits)
        {
            WheelItem wheel = hit.GetComponent<WheelItem>();
            if (wheel == null) continue;
            if (wheel == attachedWheel) continue;
            if (!wheel.IsBeingGrabbed) continue;

            AttachWheel(wheel);
            break;
        }
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Gắn WheelItem vào socket tại vị trí chỉ định.
    /// Tháo bánh cũ nếu đang có.
    /// </summary>
    public void AttachWheel(WheelItem wheel)
    {
        if (wheel == null) return;

        // Tháo bánh cũ nếu khác bánh mới
        if (attachedWheel != null && attachedWheel != wheel)
        {
            WheelItem old = attachedWheel;
            attachedWheel = null;
            old.Detach();
            onWheelDetached?.Invoke(old);
        }

        attachedWheel = wheel;
        wheel.RegisterToSocket(this);
        wheel.transform.localPosition = GetAttachLocalPosition();

        onWheelAttached?.Invoke(wheel);
        Debug.Log($"[WheelSocket: {name}] Attached → {wheel.name} tại {wheel.transform.localPosition}");
    }

    /// <summary>
    /// WheelItem gọi khi tháo ra → socket trống, sẵn sàng nhận bánh mới.
    /// </summary>
    public void NotifyWheelDetached(WheelItem wheel)
    {
        if (attachedWheel != wheel) return;

        attachedWheel = null;
        onWheelDetached?.Invoke(wheel);

        Debug.Log($"[WheelSocket: {name}] Trống — sẵn sàng nhận bánh mới");
    }

    /// <summary>Tháo bánh hiện tại (gọi từ bên ngoài nếu cần)</summary>
    public void DetachCurrentWheel()
    {
        if (attachedWheel != null)
            attachedWheel.Detach();
    }

    /// <summary>
    /// Xóa SẠCH mọi child của socket (bánh baked + visual bất kỳ) và reset trạng thái về trống.
    /// Dùng khi loadout không có bánh cho socket này → socket trống hoàn toàn, đồng bộ với data.
    /// </summary>
    public void ClearAllChildren()
    {
        initialWheel = null;
        attachedWheel = null;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    /// <summary>
    /// Tìm bánh rời gần nhất và gắn vào socket này.
    /// Dùng cho PC interaction khi muốn thao tác trực tiếp trên socket.
    /// </summary>
    public bool TryAttachNearestDetachedWheel(float maxDistance = float.PositiveInfinity)
    {
        WheelItem[] allWheels = FindObjectsByType<WheelItem>(FindObjectsSortMode.None);
        WheelItem best = null;
        float bestDist = maxDistance;

        foreach (WheelItem wheel in allWheels)
        {
            if (wheel == null || wheel.IsAttached)
                continue;

            float dist = Vector3.Distance(transform.position, wheel.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = wheel;
            }
        }

        if (best == null)
            return false;

        AttachWheel(best);
        return true;
    }

    /// <summary>
    /// Nếu socket đang có bánh thì tháo ra, nếu đang trống thì thử gắn bánh rời gần nhất.
    /// </summary>
    public bool ToggleAttachState(bool attachNearestWheelWhenEmpty = true)
    {
        if (HasWheel)
        {
            DetachCurrentWheel();
            return true;
        }

        return attachNearestWheelWhenEmpty && TryAttachNearestDetachedWheel();
    }

    /// <summary>Rotation đúng theo side: Left → Y=180, Right → Y=0</summary>
    public Quaternion GetAttachRotation()
    {
        float y = side == WheelSide.Left ? leftSideRotationY : rightSideRotationY;
        return Quaternion.Euler(0f, y, 0f);
    }

    private Vector3 GetAttachLocalPosition()
    {
        if (attachPoint != null)
            return transform.InverseTransformPoint(attachPoint.position);

        return attachLocalPosition;
    }

#if UNITY_EDITOR
    [ContextMenu("Force Detect Side")]    void DbgDetect() => AutoDetectSide();
    [ContextMenu("Detach Current Wheel")] void DbgDetach() => DetachCurrentWheel();

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = HasWheel ? new Color(0, 1, 0, 0.2f) : new Color(1, 1, 0, 0.2f);
        Gizmos.DrawSphere(transform.position, snapRadius);
        Gizmos.color = HasWheel ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, snapRadius);

        Vector3 attachWorldPos = attachPoint != null
            ? attachPoint.position
            : transform.TransformPoint(attachLocalPosition);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(attachWorldPos, 0.03f);
        Gizmos.DrawLine(transform.position, attachWorldPos);

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (snapRadius + 0.05f),
            HasWheel ? $"● {attachedWheel.name}" : "○ Empty"
        );

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(attachWorldPos, GetAttachRotation() * Vector3.forward * 0.3f);
    }
#endif
}
