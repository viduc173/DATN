using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Gán vào Car01_Wheel_FrontLeft (con).
/// Quản lý trạng thái bánh xe: Attached / Detached.
/// Khi grab → tháo ra. Khi thả gần WheelSocket → gắn vào.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class WheelItem : MonoBehaviour
{
    // ─── Trạng thái ──────────────────────────────────────────────────────────

    public enum WheelState { Attached, Detached }

    [Header("Trạng thái (Read Only)")]
    [SerializeField] private WheelState state = WheelState.Attached;

    [Header("Snap")]
    [Tooltip("Khoảng cách tối đa để snap vào WheelSocket khi thả — nên bằng ghostPreviewDistance của PCInteractorObject")]
    [SerializeField] private float snapDistance = 1.2f;

    [Header("Events")]
    public UnityEvent onAttached;
    public UnityEvent onDetached;

    // ─── Private ──────────────────────────────────────────────────────────────

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private WheelSocket currentSocket;
    private WheelSocket _lastSocket;

    // ─── Properties ───────────────────────────────────────────────────────────

    public bool IsAttached      => state == WheelState.Attached;
    public bool IsDetached      => state == WheelState.Detached;
    public WheelSocket CurrentSocket => currentSocket;

    /// <summary>True khi đang được tay XR cầm — dùng bởi WheelSocket.OverlapSphere</summary>
    public bool IsBeingGrabbed  => grabInteractable != null && grabInteractable.isSelected;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (IsAttached && currentSocket != null) return;

        WheelSocket parentSocket = GetComponentInParent<WheelSocket>();
        if (parentSocket != null)
            RegisterToSocket(parentSocket);
        else if (state == WheelState.Attached)
            state = WheelState.Detached; // sửa mismatch: bánh rời không có socket cha
    }

    // ─── HOTFIX (tạm): chống bánh đã gắn bị "tụt" ───────────────────────────────
    // Triệu chứng: bánh đã gắn vào socket nhưng Rigidbody bị non-kinematic + gravity
    // (race lúc boot khi GarageSaveManager huỷ/spawn lại bánh, hoặc XR can thiệp Rigidbody)
    // → physics kéo bánh rơi dù vẫn là con của socket. Chỉ lộ rõ ở build, thường ở xe boot (CarType_0).
    // Guard: mỗi physics step, nếu đang gắn mà Rigidbody không kinematic → ép kinematic lại.
    // Bắt trong 1 FixedUpdate (~0.02s) nên độ trôi < ~2mm, không nhìn thấy. Không đụng localPosition
    // để khỏi "đánh nhau" với offset attach của socket. Chỉ chạy khi đã gắn → không ảnh hưởng bánh đang cầm/rời.
    private void FixedUpdate()
    {
        if (state == WheelState.Attached && currentSocket != null && rb != null && !rb.isKinematic)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    // ─── Grab / Release ───────────────────────────────────────────────────────

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        Detach();
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        WheelSocket nearest = FindNearestSocket(snapDistance, true);
        if (nearest != null)
            nearest.AttachWheel(this);
        else
            SetRigidbodyFree(true);
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Gắn bánh vào socket (gọi bởi WheelSocket)</summary>
    public void RegisterToSocket(WheelSocket socket)
    {
        currentSocket = socket;
        state = WheelState.Attached;

        transform.SetParent(socket.transform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = socket.GetAttachRotation();

        SetRigidbodyFree(false);

        onAttached?.Invoke();
        Debug.Log($"[WheelItem: {name}] Gắn vào socket: {socket.name}");
    }

    /// <summary>Tháo bánh khỏi socket hiện tại</summary>
    public void Detach()
    {
        if (currentSocket != null)
        {
            _lastSocket = currentSocket;
            currentSocket.NotifyWheelDetached(this);
            currentSocket = null;
        }

        state = WheelState.Detached;
        transform.SetParent(null);
        SetRigidbodyFree(true);

        onDetached?.Invoke();
        Debug.Log($"[WheelItem: {name}] Đã tháo ra");
    }

    /// <summary>
    /// Tìm socket gần nhất và gắn bánh vào đó.
    /// ignoreSnapDistanceLimit = true phù hợp cho PC interaction vì không cần thao tác grab/release.
    /// </summary>
    public bool TryAttachNearestSocket(bool ignoreSnapDistanceLimit = false)
    {
        float maxDistance = ignoreSnapDistanceLimit ? float.PositiveInfinity : snapDistance;

        // Ưu tiên về đúng socket gốc trước (tránh gắn nhầm sang xe khác hoặc xe inactive)
        // Vẫn phải check distance — nếu ignoreSnapDistanceLimit=false (thả chuột) thì chỉ snap khi đủ gần
        if (_lastSocket != null && !_lastSocket.HasWheel && _lastSocket.gameObject.activeInHierarchy)
        {
            float dist = Vector3.Distance(transform.position, _lastSocket.transform.position);
            if (dist <= maxDistance)
            {
                _lastSocket.AttachWheel(this);
                return true;
            }
        }
        WheelSocket nearest = FindNearestSocket(maxDistance, true);
        if (nearest == null)
            return false;

        nearest.AttachWheel(this);
        return true;
    }

    /// <summary>
    /// Nếu bánh đang gắn thì tháo ra, nếu đang rời thì thử gắn vào socket gần nhất.
    /// </summary>
    public bool ToggleAttachState(bool ignoreSnapDistanceLimitWhenAttaching = false)
    {
        if (IsAttached)
        {
            Detach();
            return true;
        }

        return TryAttachNearestSocket(ignoreSnapDistanceLimitWhenAttaching);
    }

    /// <summary>Tìm socket trống gần nhất trong phạm vi maxDistance — dùng cho ghost preview.</summary>
    public WheelSocket FindNearestAvailableSocket(float maxDistance)
        => FindNearestSocket(maxDistance, requireEmpty: true);

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private WheelSocket FindNearestSocket(float maxDistance, bool requireEmpty = true)
    {
        WheelSocket[] allSockets = FindObjectsByType<WheelSocket>(FindObjectsSortMode.None);
        WheelSocket best = null;
        float bestDist = maxDistance;

        // Chỉ xét socket thuộc xe đang active (nếu GarageCarManager tồn tại)
        Transform activeCar = GarageCarManager.Instance?.ActiveCarTransform;

        foreach (WheelSocket socket in allSockets)
        {
            if (!socket.enabled) continue;
            if (!socket.gameObject.activeInHierarchy) continue;
            if (requireEmpty && socket.HasWheel) continue;
            if (activeCar != null && !socket.transform.IsChildOf(activeCar)) continue;

            float dist = Vector3.Distance(transform.position, socket.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = socket;
            }
        }

        return best;
    }

    private void SetRigidbodyFree(bool free)
    {
        if (rb == null) return;
        rb.isKinematic = !free;
        rb.useGravity  = free;
    }

#if UNITY_EDITOR
    [ContextMenu("Force Detach")] void DbgDetach() => Detach();
#endif
}
