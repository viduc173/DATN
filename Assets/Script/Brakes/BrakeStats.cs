using UnityEngine;
using UnityEngine.Events;

public class BrakeStats : MonoBehaviour
{
    [Header("Part Data")]
    public CarPart partData;

    [Header("Ownership")]
    public bool isOwnedByDefault = false;

    [Header("Events")]
    public UnityEvent<CarPart> onEquippedToLoadout;
    public UnityEvent<CarPart> onRemovedFromLoadout;

    private BrakeItem brakeItem;
    private CarLoadoutSlot lastLoadoutSlot;
    private string lastBrakePosition;

    private void Awake()
    {
        brakeItem = GetComponent<BrakeItem>();
    }

    private void OnEnable()
    {
        if (brakeItem != null)
        {
            brakeItem.onAttached.AddListener(HandleAttached);
            brakeItem.onDetached.AddListener(HandleDetached);
        }
    }

    private void OnDisable()
    {
        if (brakeItem != null)
        {
            brakeItem.onAttached.RemoveListener(HandleAttached);
            brakeItem.onDetached.RemoveListener(HandleDetached);
        }
    }

    private void HandleAttached()
    {
        if (partData == null || brakeItem == null) return;

        BrakeSocket socket = brakeItem.CurrentSocket;
        if (socket == null) return;

        CarLoadoutSlot slot = socket.GetComponentInParent<CarLoadoutSlot>();
        if (slot == null) return;

        lastLoadoutSlot = slot;
        lastBrakePosition = socket.name;

        // Bootstrap (gắn phanh initial / restore): KHÔNG ghi loadout — tránh phanh baked tự nạp lại
        // loadout.brakes khiến "Clear" vô nghĩa. RestoreBrakes mới là nguồn sự thật lúc load.
        if (GarageSaveManager.Instance != null && GarageSaveManager.Instance.IsBootstrapping)
            return;

        CarStatsUIManager.ReportPartChangeAnchor(socket.transform, isAttach: true);
        slot.EquipBrake(partData, socket.name);
        onEquippedToLoadout?.Invoke(partData);

        // Adapt the caliper to its socket side: left socket -> left mesh, right socket -> right mesh.
        ApplySideMesh(socket.Side);

        // Per-caliper inventory: each caliper (L or R) is one unit.
        PartInventoryBridge.Consume(partData);
    }

    private void HandleDetached()
    {
        if (partData == null || lastLoadoutSlot == null) return;

        if (!string.IsNullOrEmpty(lastBrakePosition))
        {
            CarStatsUIManager.ReportPartChangeAnchor(transform, isAttach: false);
            lastLoadoutSlot.UnequipBrake(partData, lastBrakePosition);
        }
        else
        {
            CarStatsUIManager.ReportPartChangeAnchor(transform, isAttach: false);
            lastLoadoutSlot.UnequipPart(partData);
        }

        onRemovedFromLoadout?.Invoke(partData);

        // Per-caliper inventory: removing a caliper returns one unit.
        PartInventoryBridge.Return(partData);

        lastLoadoutSlot = null;
        lastBrakePosition = null;
    }

    /// <summary>
    /// Swap this caliper's mesh to match the socket side so a left socket always shows the
    /// left caliper mesh and a right socket the right one (the two share one CarPart but have
    /// mirrored meshes). Position/rotation are handled by the BrakeSocket on attach.
    /// </summary>
    private void ApplySideMesh(BrakeSocket.BrakeSide side)
    {
        if (partData == null) return;

        GameObject sourcePrefab = side == BrakeSocket.BrakeSide.Left
            ? partData.brakePrefabLeft
            : partData.brakePrefabRight;
        if (sourcePrefab == null) return;

        MeshFilter source = sourcePrefab.GetComponentInChildren<MeshFilter>();
        MeshFilter mine = GetComponentInChildren<MeshFilter>();
        if (source != null && mine != null && source.sharedMesh != null)
            mine.sharedMesh = source.sharedMesh;
    }

    public CarStats GetStatBonus() => partData != null ? partData.statBonus : default;
    public string GetDisplayName() => partData != null ? partData.partName : name;
}
