using UnityEngine;
using UnityEngine.Events;

public class BrakeSocket : MonoBehaviour
{
    public enum BrakeSide { Left, Right }

    [Header("Position")]
    [SerializeField] private BrakeSide side;

    [Header("Attach")]
    [SerializeField] private Vector3 attachLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 attachLocalRotationEuler = Vector3.zero;
    [SerializeField] private Transform attachPoint;
    [SerializeField] private BrakeItem initialBrake;

    [Header("Snap")]
    [SerializeField] private float snapRadius = 0.25f;

    [Header("Events")]
    public UnityEvent<BrakeItem> onBrakeAttached;
    public UnityEvent<BrakeItem> onBrakeDetached;

    [Header("Debug")]
    [SerializeField] private BrakeItem attachedBrake;

    public BrakeSide Side => side;
    public BrakeItem AttachedBrake => attachedBrake;
    public bool HasBrake => attachedBrake != null;

    private void Awake()
    {
        AutoDetectSide();
    }

    private void Start()
    {
        // Garage: loadout là nguồn sự thật. Không có phanh trong loadout -> xóa child.
        if (GarageSaveManager.Instance != null)
        {
            var loadoutSlot = GetComponentInParent<CarLoadoutSlot>();
            if (loadoutSlot != null && !loadoutSlot.LoadoutHasBrakeFor(this))
                ClearAllChildren();
            return;
        }

        if (attachedBrake != null)
            return;

        if (initialBrake != null)
            AttachBrake(initialBrake);
    }

    private void Update()
    {
        CheckForNearbyGrabbedBrake();
    }

    public void SetInitialBrake(BrakeItem brake)
    {
        initialBrake = brake;

        if (Application.isPlaying && brake != null && attachedBrake != brake)
            AttachBrake(brake);
    }

    public void AttachBrake(BrakeItem brake)
    {
        if (brake == null) return;

        if (attachedBrake != null && attachedBrake != brake)
        {
            BrakeItem old = attachedBrake;
            attachedBrake = null;
            old.Detach();
            onBrakeDetached?.Invoke(old);
        }

        attachedBrake = brake;
        brake.RegisterToSocket(this);
        brake.transform.localPosition = GetAttachLocalPosition();
        brake.transform.localRotation = GetAttachRotation();

        onBrakeAttached?.Invoke(brake);
        Debug.Log($"[BrakeSocket: {name}] Attached -> {brake.name}");
    }

    public void NotifyBrakeDetached(BrakeItem brake)
    {
        if (attachedBrake != brake) return;

        attachedBrake = null;
        onBrakeDetached?.Invoke(brake);
        Debug.Log($"[BrakeSocket: {name}] Empty");
    }

    public void DetachCurrentBrake()
    {
        if (attachedBrake != null)
            attachedBrake.Detach();
    }

    /// <summary>
    /// Xóa SẠCH mọi child của socket (phanh baked + visual bất kỳ) và reset trạng thái về trống.
    /// Dùng khi loadout không có phanh cho socket này → socket trống hoàn toàn, đồng bộ với data.
    /// </summary>
    public void ClearAllChildren()
    {
        initialBrake = null;
        attachedBrake = null;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    public bool ToggleAttachState(bool attachNearestBrakeWhenEmpty = true)
    {
        if (HasBrake)
        {
            DetachCurrentBrake();
            return true;
        }

        return attachNearestBrakeWhenEmpty && TryAttachNearestDetachedBrake();
    }

    public bool TryAttachNearestDetachedBrake(float maxDistance = float.PositiveInfinity)
    {
        BrakeItem[] allBrakes = FindObjectsByType<BrakeItem>(FindObjectsSortMode.None);
        BrakeItem best = null;
        float bestDist = maxDistance;

        foreach (BrakeItem brake in allBrakes)
        {
            if (brake == null || brake.IsAttached)
                continue;

            float dist = Vector3.Distance(transform.position, brake.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = brake;
            }
        }

        if (best == null)
            return false;

        AttachBrake(best);
        return true;
    }

    public Quaternion GetAttachRotation()
        => Quaternion.Euler(attachLocalRotationEuler);

    public Vector3 GetAttachLocalPosition()
    {
        if (attachPoint != null)
            return transform.InverseTransformPoint(attachPoint.position);

        return attachLocalPosition;
    }

    private void CheckForNearbyGrabbedBrake()
    {
        BrakeItem[] allBrakes = FindObjectsByType<BrakeItem>(FindObjectsSortMode.None);
        foreach (BrakeItem brake in allBrakes)
        {
            if (brake == null || brake == attachedBrake || !brake.IsBeingGrabbed)
                continue;

            if (Vector3.Distance(transform.position, brake.transform.position) > snapRadius)
                continue;

            AttachBrake(brake);
            break;
        }
    }

    private void AutoDetectSide()
    {
        string n = gameObject.name.ToLowerInvariant();
        if (n.Contains("fl") || n.Contains("rl") || n.Contains("left"))
            side = BrakeSide.Left;
        else if (n.Contains("fr") || n.Contains("rr") || n.Contains("right"))
            side = BrakeSide.Right;
        else
            side = transform.localPosition.x < 0f ? BrakeSide.Left : BrakeSide.Right;
    }
}
