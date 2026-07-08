using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gắn trên WheelItem GO. Liên kết bánh xe vật lý với dữ liệu CarPart (ScriptableObject).
/// Tự động cập nhật CarLoadoutSlot của xe khi bánh được lắp vào hoặc tháo ra.
///
/// Setup:
///   1. Gắn script này lên cùng GO với WheelItem.
///   2. Gán partData → CarPart asset tương ứng (slot = Tires).
///   3. Xe đích phải có CarLoadoutSlot ở trong parent hierarchy.
/// </summary>
[RequireComponent(typeof(WheelItem))]
public class WheelStats : MonoBehaviour
{
    [Header("Part Data")]
    [Tooltip("CarPart ScriptableObject định nghĩa chỉ số bánh này. Tạo tại: Race/Car Part, slot = Tires.")]
    public CarPart partData;

    [Header("Ownership")]
    [Tooltip("True = luôn available (bánh mặc định). False = chỉ hiện khi đã mua trong PlayerInventory.")]
    public bool isOwnedByDefault = false;

    [Header("Events")]
    public UnityEvent<CarPart> onEquippedToLoadout;
    public UnityEvent<CarPart> onRemovedFromLoadout;

    private WheelItem _wheelItem;
    private CarLoadoutSlot _lastLoadoutSlot;
    private string _lastWheelPosition;

    // ── Lifecycle ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _wheelItem = GetComponent<WheelItem>();
    }

    private void OnEnable()
    {
        _wheelItem.onAttached.AddListener(HandleAttached);
        _wheelItem.onDetached.AddListener(HandleDetached);
    }

    private void OnDisable()
    {
        _wheelItem.onAttached.RemoveListener(HandleAttached);
        _wheelItem.onDetached.RemoveListener(HandleDetached);
    }

    // ── Handlers ─────────────────────────────────────────────────────────────────

    private void HandleAttached()
    {
        if (partData == null) return;

        WheelSocket socket = _wheelItem.CurrentSocket;
        if (socket == null) return;

        CarLoadoutSlot slot = socket.GetComponentInParent<CarLoadoutSlot>();
        if (slot == null) return;

        _lastLoadoutSlot = slot;
        _lastWheelPosition = socket.name;

        // Bootstrap (gắn bánh initial / restore): KHÔNG ghi loadout — tránh bánh baked tự nạp lại
        // loadout.wheels khiến "Clear" vô nghĩa. RestoreTires mới là nguồn sự thật lúc load.
        if (GarageSaveManager.Instance != null && GarageSaveManager.Instance.IsBootstrapping)
            return;

        CarStatsUIManager.ReportPartChangeAnchor(socket.transform, isAttach: true);
        slot.EquipTires(partData, socket.name);
        onEquippedToLoadout?.Invoke(partData);

        // Player mounted this wheel onto the car -> take one out of inventory.
        // No-op during programmatic loadout sync (PartInventoryBridge.Suppressed).
        PartInventoryBridge.Consume(partData);
    }

    private void HandleDetached()
    {
        if (partData == null || _lastLoadoutSlot == null) return;

        if (!string.IsNullOrEmpty(_lastWheelPosition))
        {
            CarStatsUIManager.ReportPartChangeAnchor(transform, isAttach: false);
            _lastLoadoutSlot.UnequipTires(partData, _lastWheelPosition);
        }
        else
        {
            CarStatsUIManager.ReportPartChangeAnchor(transform, isAttach: false);
            _lastLoadoutSlot.UnequipTires(partData);
        }

        onRemovedFromLoadout?.Invoke(partData);

        // Wheel removed from the car -> give one back to inventory.
        PartInventoryBridge.Return(partData);

        _lastLoadoutSlot = null;
        _lastWheelPosition = null;
    }

    // ── Public API ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Trả về stat bonus của bánh này. Dùng cho UI preview khi hover trong Shop.
    /// </summary>
    public CarStats GetStatBonus() => partData != null ? partData.statBonus : default;

    /// <summary>
    /// Tên hiển thị của bánh. Fallback về tên GO nếu chưa gán partData.
    /// </summary>
    public string GetDisplayName() => partData != null ? partData.partName : name;

#if UNITY_EDITOR
    [ContextMenu("Log Effective Stats on Car")]
    void DbgLogStats()
    {
        if (_lastLoadoutSlot == null) { Debug.Log($"[WheelStats: {name}] Chưa gắn vào xe nào."); return; }
        CarStats s = _lastLoadoutSlot.GetEffectiveStats();
        Debug.Log($"[WheelStats: {name}] Effective stats → Speed:{s.maxSpeed:F1} Accel:{s.acceleration:F1} Grip:{s.grip:F1} Brake:{s.braking:F1} Handling:{s.handling:F1}");
    }
#endif
}
