using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Singleton quản lý xe nào đang active trong garage. Gắn lên CarPlace.
/// Tự động discover GarageCarSlot trên từng direct child.
///
/// Hierarchy mong đợi:
///   CarPlace  ← gắn script này
///   ├── CarType_1  ← GarageCarSlot sẽ được auto-add nếu chưa có
///   ├── CarType_2
///   └── CarType_3
///
/// Cách chọn xe:
///   - Bấm prevCarKey / nextCarKey để cycle qua các xe
///   - Hoặc gọi SetActiveCar(index) từ UI Button
///   - Hoặc player nhìn vào xe và bấm selectKey (raycast qua PCCameraController)
/// </summary>
[DefaultExecutionOrder(-50)]
public class GarageCarManager : MonoBehaviour
{
    public static GarageCarManager Instance { get; private set; }

    [Header("Khởi tạo")]
    [Tooltip("Index xe active khi bắt đầu (0 = xe đầu tiên)")]
    [SerializeField] private int _activeIndex = 0;

    [Header("Input chuyển xe")]
    [Tooltip("Phím chuyển sang xe trước")]
    [SerializeField] private KeyCode prevCarKey = KeyCode.Q;
    [Tooltip("Phím chuyển sang xe tiếp theo")]
    [SerializeField] private KeyCode nextCarKey = KeyCode.E;
    [Tooltip("Bật/tắt input bàn phím để chuyển xe")]
    [SerializeField] private bool enableKeyboardInput = true;

    [Header("Events")]
    [Tooltip("Gọi khi xe active thay đổi. Tham số: index xe mới. Nối vào UI để cập nhật tên xe, stats, v.v.")]
    public UnityEvent<int> onCarChanged;

    private GarageCarSlot[] _slots;
    private PlayerInventory _inventory;

    // Inventory để kiểm tra xe đã sở hữu (lazy, lấy từ GarageDisplayedCarContext trong scene).
    private PlayerInventory Inventory
    {
        get
        {
            if (_inventory == null)
            {
                var ctx = FindFirstObjectByType<GarageDisplayedCarContext>();
                if (ctx != null) _inventory = ctx.Inventory;
            }
            return _inventory;
        }
    }

    // ── Properties ──────────────────────────────────────────────────────────────

    public int ActiveIndex => _activeIndex;
    public int CarCount    => _slots?.Length ?? 0;

    public GarageCarSlot ActiveSlot =>
        (_slots != null && _activeIndex >= 0 && _activeIndex < _slots.Length)
            ? _slots[_activeIndex]
            : null;

    /// <summary>Transform của xe đang active — dùng bởi WheelItem để lọc socket.</summary>
    public Transform ActiveCarTransform => ActiveSlot?.transform;

    public string ActiveCarName => ActiveSlot != null ? ActiveSlot.name : string.Empty;

    public bool TryGetActivePaintTarget(out CarPaintTarget target, out string carTypeName)
    {
        GarageCarSlot slot = ActiveSlot;
        if (slot == null)
        {
            target = null;
            carTypeName = string.Empty;
            return false;
        }

        target = slot.GetComponentInChildren<CarPaintTarget>();
        carTypeName = target != null && !string.IsNullOrWhiteSpace(target.carTypeName)
            ? target.carTypeName
            : slot.name;

        return target != null && target.bodyRenderer != null;
    }

    public bool TryGetActivePaintTargets(out Renderer[] renderers, out int[] materialSlotIndices, out string carTypeName)
    {
        GarageCarSlot slot = ActiveSlot;
        if (slot == null)
        {
            renderers = null;
            materialSlotIndices = null;
            carTypeName = string.Empty;
            return false;
        }

        CarPaintTarget[] explicitTargets = slot.GetComponentsInChildren<CarPaintTarget>(includeInactive: true);
        carTypeName = slot.name;
        foreach (CarPaintTarget target in explicitTargets)
        {
            if (target != null && !string.IsNullOrWhiteSpace(target.carTypeName))
            {
                carTypeName = target.carTypeName;
                break;
            }
        }

        var rendererList = new List<Renderer>();
        var slotList = new List<int>();
        var seen = new HashSet<Renderer>();

        void AddRenderer(Renderer renderer, int slotIndex)
        {
            if (renderer == null || seen.Contains(renderer))
                return;

            if (slotIndex < 0 || slotIndex >= renderer.sharedMaterials.Length)
                return;

            seen.Add(renderer);
            rendererList.Add(renderer);
            slotList.Add(slotIndex);
        }

        foreach (CarPaintTarget target in explicitTargets)
        {
            if (target != null && target.bodyRenderer != null)
                AddRenderer(target.bodyRenderer, target.materialSlotIndex);
        }

        Renderer[] allRenderers = slot.GetComponentsInChildren<Renderer>(includeInactive: true);
        foreach (Renderer renderer in allRenderers)
        {
            if (renderer != null && renderer.CompareTag("PaintPart"))
                AddRenderer(renderer, renderer.sharedMaterials.Length - 1);
        }

        renderers = rendererList.ToArray();
        materialSlotIndices = slotList.ToArray();
        return renderers.Length > 0;
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        DiscoverSlots();

        // Khôi phục xe đã chọn từ session trước
        int savedIndex = ActiveLoadout.SavedCarIndex;
        if (savedIndex >= 0 && savedIndex < _slots.Length)
            _activeIndex = savedIndex;

        SetActiveCar(_activeIndex, silent: true);
    }

    private void Update()
    {
        if (!enableKeyboardInput) return;

        if (Input.GetKeyDown(prevCarKey)) PrevCar();
        if (Input.GetKeyDown(nextCarKey)) NextCar();
    }

    // ── Slot Discovery ───────────────────────────────────────────────────────────

    private void DiscoverSlots()
    {
        int count = transform.childCount;
        _slots = new GarageCarSlot[count];

        for (int i = 0; i < count; i++)
        {
            var child = transform.GetChild(i);
            var slot  = child.GetComponent<GarageCarSlot>() ?? child.gameObject.AddComponent<GarageCarSlot>();
            slot.Init(i, this);
            _slots[i] = slot;
        }

        Debug.Log($"[GarageCarManager] Found {count} car(s) under '{name}'. Keys: {prevCarKey}/{nextCarKey}");
    }

    // ── Public API ───────────────────────────────────────────────────────────────

    /// <summary>Kích hoạt xe tại index, vô hiệu hoá tất cả xe còn lại.</summary>
    public void SetActiveCar(int index, bool silent = false)
    {
        if (_slots == null || _slots.Length == 0) return;

        _activeIndex = Mathf.Clamp(index, 0, _slots.Length - 1);

        // Deactivate trước — đảm bảo không có 2 Rigidbody tồn tại cùng lúc trong physics
        for (int i = 0; i < _slots.Length; i++)
            if (i != _activeIndex) _slots[i].SetActive(false);

        // Activate sau
        _slots[_activeIndex].SetActive(true);

        // CarLoadoutSlot là driver: đồng bộ socket theo loadout (thiếu data → xóa child, có → hiện đúng).
        // Lúc boot Instance còn null → GarageSaveManager.Start tự sync tất cả xe.
        var loadoutSlot = _slots[_activeIndex].GetComponent<CarLoadoutSlot>();
        loadoutSlot?.SyncSocketsFromLoadout();

        // Lưu xe đang chọn + expose loadout cho racing scene
        ActiveLoadout.SavedCarIndex = _activeIndex;
        if (loadoutSlot != null)
            ActiveLoadout.Current = loadoutSlot.loadout;

        if (!silent)
        {
            onCarChanged?.Invoke(_activeIndex);
            Debug.Log($"[GarageCarManager] Active → [{_activeIndex}] '{_slots[_activeIndex].name}'  loadout: {ActiveLoadout.Current?.loadoutName ?? "none"}");
        }
    }

    /// <summary>Chuyển sang xe kế tiếp ĐÃ SỞ HỮU (bỏ qua xe chưa mua).</summary>
    public void NextCar() => SetActiveCar(NextOwnedIndex(+1));

    /// <summary>Chuyển về xe trước đó ĐÃ SỞ HỮU (bỏ qua xe chưa mua).</summary>
    public void PrevCar() => SetActiveCar(NextOwnedIndex(-1));

    /// <summary>Tìm index xe đã sở hữu kế tiếp theo hướng dir; nếu không có thì giữ nguyên.</summary>
    private int NextOwnedIndex(int dir)
    {
        if (_slots == null || _slots.Length == 0) return _activeIndex;

        int n = _slots.Length;
        for (int step = 1; step <= n; step++)
        {
            int idx = ((_activeIndex + dir * step) % n + n) % n;
            if (IsOwned(idx)) return idx;
        }
        return _activeIndex; // không còn xe nào khác đã sở hữu
    }

    /// <summary>Xe tại index đã được mua chưa? (loadout nằm trong PlayerInventory.ownedLoadouts).</summary>
    private bool IsOwned(int idx)
    {
        if (_slots == null || idx < 0 || idx >= _slots.Length) return false;

        PlayerInventory inv = Inventory;
        if (inv == null) return true; // fail-open: chưa có inventory thì vẫn cho chuyển

        var loadoutSlot = _slots[idx].GetComponent<CarLoadoutSlot>();
        if (loadoutSlot == null || loadoutSlot.loadout == null) return true;

        return inv.OwnsLoadout(loadoutSlot.loadout);
    }

    /// <summary>
    /// Gọi từ GarageCarSlot khi player click/interact vào thân xe để chọn xe đó.
    /// </summary>
    public void SelectSlot(GarageCarSlot slot)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == slot) { SetActiveCar(i); return; }
        }
    }

    /// <summary>Chọn xe theo loadout (UI inventory chọn xe). Đổi xe đang hiển thị của màn.</summary>
    public bool SelectByLoadout(PlayerCarLoadout loadout)
    {
        if (_slots == null || loadout == null) return false;
        for (int i = 0; i < _slots.Length; i++)
        {
            var ls = _slots[i].GetComponent<CarLoadoutSlot>();
            if (ls != null && ls.loadout == loadout) { SetActiveCar(i); return true; }
        }
        return false;
    }

#if UNITY_EDITOR
    [ContextMenu("→ Next Car")] void DbgNext() => NextCar();
    [ContextMenu("← Prev Car")] void DbgPrev() => PrevCar();
    [ContextMenu("List Slots")]
    void DbgList()
    {
        if (_slots == null) { Debug.Log("[GarageCarManager] Not initialized."); return; }
        for (int i = 0; i < _slots.Length; i++)
            Debug.Log($"  [{i}] {_slots[i].name}{(i == _activeIndex ? "  ← ACTIVE" : "")}");
    }
#endif
}
