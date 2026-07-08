using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Gắn vào brake caliper (con của BrakeSocket).
/// Quản lý trạng thái phanh: Attached / Detached.
/// Khi grab → tháo ra. Khi thả gần BrakeSocket → gắn vào.
/// Pattern giống WheelItem: [RequireComponent(typeof(XRGrabInteractable))] đảm bảo
/// Unity serialize sẵn XRGrabInteractable + Rigidbody vào scene — không race condition với Awake.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class BrakeItem : MonoBehaviour
{
    public enum BrakeState { Attached, Detached }

    [Header("State")]
    [SerializeField] private BrakeState state = BrakeState.Attached;

    [Header("Snap")]
    [SerializeField] private float snapDistance = 1.2f;

    [Header("Events")]
    public UnityEvent onAttached;
    public UnityEvent onDetached;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private BrakeSocket currentSocket;
    private BrakeSocket lastSocket;

    public bool IsAttached   => state == BrakeState.Attached;
    public bool IsDetached   => state == BrakeState.Detached;
    public bool IsBeingGrabbed => grabInteractable != null && grabInteractable.isSelected;
    public BrakeSocket CurrentSocket => currentSocket;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        // [RequireComponent] đảm bảo XRGrabInteractable + Rigidbody đã có sẵn → GetComponent luôn thành công
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (IsAttached && currentSocket != null) return;

        BrakeSocket parentSocket = GetComponentInParent<BrakeSocket>();
        if (parentSocket != null)
            RegisterToSocket(parentSocket);
        else if (state == BrakeState.Attached)
            state = BrakeState.Detached;
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

    private void OnGrabbed(SelectEnterEventArgs args) => Detach();

    private void OnReleased(SelectExitEventArgs args)
    {
        BrakeSocket nearest = FindNearestSocket(snapDistance, requireEmpty: true);
        if (nearest != null)
            nearest.AttachBrake(this);
        else
            SetRigidbodyFree(true);
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    public void RegisterToSocket(BrakeSocket socket)
    {
        currentSocket = socket;
        state = BrakeState.Attached;

        transform.SetParent(socket.transform);
        transform.localPosition = socket.GetAttachLocalPosition();
        transform.localRotation = socket.GetAttachRotation();

        SetRigidbodyFree(false);

        onAttached?.Invoke();
        Debug.Log($"[BrakeItem: {name}] Attached to socket: {socket.name}");
    }

    public void Detach()
    {
        if (currentSocket != null)
        {
            lastSocket = currentSocket;
            currentSocket.NotifyBrakeDetached(this);
            currentSocket = null;
        }

        state = BrakeState.Detached;
        transform.SetParent(null);
        SetRigidbodyFree(true);

        onDetached?.Invoke();
        Debug.Log($"[BrakeItem: {name}] Detached");
    }

    public bool ToggleAttachState(bool ignoreSnapDistanceLimitWhenAttaching = false)
    {
        if (IsAttached)
        {
            Detach();
            return true;
        }
        return TryAttachNearestSocket(ignoreSnapDistanceLimitWhenAttaching);
    }

    public bool TryAttachNearestSocket(bool ignoreSnapDistanceLimit = false)
    {
        float maxDistance = ignoreSnapDistanceLimit ? float.PositiveInfinity : snapDistance;

        if (lastSocket != null && !lastSocket.HasBrake && lastSocket.gameObject.activeInHierarchy)
        {
            float dist = Vector3.Distance(transform.position, lastSocket.transform.position);
            if (dist <= maxDistance)
            {
                lastSocket.AttachBrake(this);
                return true;
            }
        }

        BrakeSocket nearest = FindNearestSocket(maxDistance, requireEmpty: true);
        if (nearest == null) return false;

        nearest.AttachBrake(this);
        return true;
    }

    public BrakeSocket FindNearestAvailableSocket(float maxDistance)
        => FindNearestSocket(maxDistance, requireEmpty: true);

    /// <summary>
    /// Giữ lại cho BrakeRuntimeBootstrap và GarageSaveManager vẫn gọi được.
    /// Với [RequireComponent] thì XRGrabInteractable + Rigidbody đã có → các GetComponent đều thành công.
    /// </summary>
    public void EnsureRuntimeComponents()
    {
        if (grabInteractable == null)
            grabInteractable = GetComponent<XRGrabInteractable>();
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private BrakeSocket FindNearestSocket(float maxDistance, bool requireEmpty = true)
    {
        BrakeSocket[] allSockets = FindObjectsByType<BrakeSocket>(FindObjectsSortMode.None);
        BrakeSocket best = null;
        float bestDist = maxDistance;
        Transform activeCar = GarageCarManager.Instance?.ActiveCarTransform;

        foreach (BrakeSocket socket in allSockets)
        {
            if (socket == null || !socket.enabled || !socket.gameObject.activeInHierarchy) continue;
            if (requireEmpty && socket.HasBrake) continue;
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
