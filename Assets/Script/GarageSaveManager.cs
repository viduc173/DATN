using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Saves/restores garage paint through PlayerPrefs.
/// Paint is stored per car: GP_{carName}.
/// Wheels and brakes are synced from PlayerCarLoadout only.
/// </summary>
[DefaultExecutionOrder(-40)]
public class GarageSaveManager : MonoBehaviour
{
    public static GarageSaveManager Instance { get; private set; }

    /// <summary>
    /// True trong lúc Awake→Start đang gắn bánh/phanh ban đầu và restore.
    /// WheelStats/BrakeStats dùng cờ này để KHÔNG ghi ngược vào loadout khi gắn bánh "initial"
    /// (nếu ghi, bánh baked sẽ tự nạp lại loadout.wheels → "Clear" vô nghĩa).
    /// </summary>
    public bool IsBootstrapping => _isBootstrapping;

    [Header("Default Visual Prefabs")]
    [Tooltip("Prefab bánh mặc định khi CarPart không gán wheelPrefab. Gán CarWheel_Normal.")]
    [SerializeField] private GameObject defaultWheelPrefab;

    [Tooltip("Prefab phanh trái mặc định khi CarPart không gán. Gán Brake_normal_L.")]
    [SerializeField] private GameObject defaultBrakePrefabLeft;

    [Tooltip("Prefab phanh phải mặc định khi CarPart không gán. Gán Brake_normal_R.")]
    [SerializeField] private GameObject defaultBrakePrefabRight;

    private const string PAINT_PREFIX = "GP_";
    private const string TIRES_PREFIX = "GW_";
    private const string BRAKE_PREFIX = "GB_";

    private Dictionary<string, Material> _matByName;
    private Material _defaultGhostMaterial;
    private float _defaultGhostPreviewDistance = 1.2f;
    private bool _isBootstrapping = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        BuildMaterialCache();
        CacheDefaultGhostSettings();
    }

    private IEnumerator Start()
    {
        // Let sockets run Start() first, then enforce the loadout state.
        yield return null;

        RestorePaint();
        RestoreTires();
        RestoreBrakes();

        _isBootstrapping = false;
    }

    private void BuildMaterialCache()
    {
        _matByName = new Dictionary<string, Material>();
        var cans = FindObjectsByType<CarPaintCan>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (CarPaintCan can in cans)
        {
            foreach (Material mat in can.GetAllMaterials())
            {
                if (mat != null && !_matByName.ContainsKey(mat.name))
                    _matByName[mat.name] = mat;
            }
        }

        Debug.Log($"[GarageSaveManager] Cached {_matByName.Count} paint material(s).");
    }

    private void CacheDefaultGhostSettings()
    {
        var interactors = FindObjectsByType<PCInteractorObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (PCInteractorObject interactor in interactors)
        {
            if (interactor == null || interactor.GhostMaterial == null)
                continue;

            _defaultGhostMaterial = interactor.GhostMaterial;
            _defaultGhostPreviewDistance = interactor.GhostPreviewDistance;
            return;
        }
    }

    public void RecordPaint(string carName, Material material)
    {
        if (string.IsNullOrEmpty(carName) || material == null) return;

        PlayerPrefs.SetString(PAINT_PREFIX + carName, material.name);
        PlayerPrefs.Save();

        // Spray-can được spawn ở RUNTIME nên material của nó thường KHÔNG có trong cache
        // (cache chỉ build từ CarPaintCan tồn tại lúc Awake — scene này không có sẵn cái nào).
        // → tự đăng ký vào cache để RestorePaint sau này resolve được theo tên.
        if (_matByName != null && !string.IsNullOrEmpty(material.name) && !_matByName.ContainsKey(material.name))
            _matByName[material.name] = material;

        // Ghi vào loadout asset để loadout là nguồn sự thật cho màu sơn.
        // Dùng thẳng Material object (không phụ thuộc cache) nên luôn ghi được.
        WriteLoadoutPaint(carName, material);
    }

    private void WriteLoadoutPaint(string carName, Material mat)
    {
        var loadoutSlots = FindObjectsByType<CarLoadoutSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (CarLoadoutSlot slot in loadoutSlots)
        {
            if (slot == null || slot.loadout == null || slot.gameObject.name != carName) continue;
            if (slot.loadout.paint == mat) return;

            slot.loadout.paint = mat;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(slot.loadout);
#endif
            return;
        }
    }

    private void RestorePaint()
    {
        var slots = FindObjectsByType<GarageCarSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GarageCarSlot slot in slots)
        {
            string key = PAINT_PREFIX + slot.name;
            Material mat = null;

            if (PlayerPrefs.HasKey(key))
                _matByName.TryGetValue(PlayerPrefs.GetString(key), out mat);

            // Fallback (kể cả khi có key nhưng cache không resolve được tên — scene không có
            // CarPaintCan sẵn): lấy thẳng Material reference từ loadout asset = nguồn sự thật.
            if (mat == null)
            {
                CarLoadoutSlot loadoutSlot = slot.GetComponent<CarLoadoutSlot>();
                if (loadoutSlot != null && loadoutSlot.loadout != null)
                    mat = loadoutSlot.loadout.paint;
            }

            if (mat == null) continue;

            ApplyPaintToSlot(slot, mat);
            Debug.Log($"[GarageSaveManager] Paint restored: {slot.name} -> {mat.name}");
        }
    }

    private void RestoreTires()
    {
        var slots = FindObjectsByType<GarageCarSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GarageCarSlot slot in slots)
            SyncCarWheels(slot);
    }

    /// <summary>
    /// Đồng bộ BÁNH của 1 xe với loadout: socket có data → spawn đúng bánh; không data → xóa sạch socket.
    /// Gọi lúc boot VÀ mỗi khi đổi xe (GarageCarManager.SetActiveCar) để bánh baked không hồi sinh.
    /// </summary>
    public void SyncCarWheels(GarageCarSlot slot)
    {
        if (slot == null) return;

        CarLoadoutSlot loadoutSlot = slot.GetComponent<CarLoadoutSlot>();
        List<CarPart> loadoutTires = GetLoadoutTires(loadoutSlot);

        WheelSocket[] sockets = slot.GetComponentsInChildren<WheelSocket>(true);
        for (int i = 0; i < sockets.Length; i++)
        {
            WheelSocket socket = sockets[i];
            CarPart part = null;

            if (i < loadoutTires.Count)
                part = loadoutTires[i];

            if (part == null)
            {
                // Không có data → xóa sạch socket để khớp "thiếu bánh"
                ClearWheelVisual(socket);
                continue;
            }

            RestoreWheelVisual(socket, part);
        }
    }

    private static List<CarPart> GetLoadoutTires(CarLoadoutSlot loadoutSlot)
    {
        if (loadoutSlot == null || loadoutSlot.loadout == null || loadoutSlot.loadout.wheels == null)
            return new List<CarPart>();
        return new List<CarPart>(loadoutSlot.loadout.wheels);
    }

    private static List<CarPart> GetLoadoutBrakes(CarLoadoutSlot loadoutSlot)
    {
        if (loadoutSlot == null || loadoutSlot.loadout == null || loadoutSlot.loadout.brakes == null)
            return new List<CarPart>();
        return new List<CarPart>(loadoutSlot.loadout.brakes);
    }

    private void RestoreBrakes()
    {
        var slots = FindObjectsByType<GarageCarSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GarageCarSlot slot in slots)
            SyncCarBrakes(slot);
    }

    /// <summary>
    /// Đồng bộ PHANH của 1 xe với loadout: socket có data → spawn đúng phanh; không data → xóa sạch socket.
    /// </summary>
    public void SyncCarBrakes(GarageCarSlot slot)
    {
        if (slot == null) return;

        CarLoadoutSlot loadoutSlot = slot.GetComponent<CarLoadoutSlot>();
        List<CarPart> loadoutBrakes = GetLoadoutBrakes(loadoutSlot);

        BrakeSocket[] sockets = slot.GetComponentsInChildren<BrakeSocket>(true);
        for (int i = 0; i < sockets.Length; i++)
        {
            BrakeSocket socket = sockets[i];
            CarPart part = null;

            if (i < loadoutBrakes.Count)
                part = loadoutBrakes[i];

            if (part == null)
            {
                // Không có data → xóa sạch socket để khớp "thiếu phanh"
                ClearBrakeVisual(socket);
                continue;
            }

            RestoreBrakeVisual(socket, part);
        }
    }

    /// <summary>
    /// Đồng bộ TOÀN BỘ bánh + phanh của 1 xe với loadout. Gọi khi đổi xe để khớp data↔scene.
    /// </summary>
    public void SyncCar(GarageCarSlot slot)
    {
        if (slot == null) return;
        SyncCarWheels(slot);
        SyncCarBrakes(slot);
    }

    public void RestoreBrakeVisual(BrakeSocket socket, CarPart part)
    {
        socket.ClearAllChildren();

        GameObject prefab = GetBrakePrefab(part, socket.Side);
        if (prefab == null) return;

        GameObject newBrakeGO = Instantiate(prefab);
        BrakeItem newBrakeItem = BrakeRuntimeBootstrap.EnsureItem(newBrakeGO, part, _defaultGhostMaterial, _defaultGhostPreviewDistance);
        if (newBrakeItem != null)
            socket.SetInitialBrake(newBrakeItem);
    }

    private GameObject GetBrakePrefab(CarPart part, BrakeSocket.BrakeSide side)
    {
        bool left = side == BrakeSocket.BrakeSide.Left;

        if (part != null)
        {
            GameObject preferred = left ? part.brakePrefabLeft : part.brakePrefabRight;
            if (preferred != null) return preferred;

            GameObject other = left ? part.brakePrefabRight : part.brakePrefabLeft;
            if (other != null) return other;
        }

        // Fallback default theo side
        return left ? defaultBrakePrefabLeft : defaultBrakePrefabRight;
    }

    /// <summary>Bỏ phanh khỏi socket: xóa SẠCH mọi child (sync khi loadout không có data cho socket này).</summary>
    private void ClearBrakeVisual(BrakeSocket socket)
    {
        socket.ClearAllChildren();
    }

    public void RestoreWheelVisual(WheelSocket socket, CarPart part)
    {
        socket.ClearAllChildren();

        GameObject prefab = GetWheelPrefab(part);
        if (prefab == null)
        {
            Debug.LogWarning($"[GarageSaveManager] Khong co wheelPrefab cho '{part?.partName}' va defaultWheelPrefab cung trong.");
            return;
        }

        GameObject newWheelGO = Instantiate(prefab);
        WheelItem newWheelItem = newWheelGO.GetComponent<WheelItem>();

        WheelStats newStats = newWheelGO.GetComponent<WheelStats>();
        if (newStats != null) newStats.partData = part;

        if (newWheelItem != null)
            socket.SetInitialWheel(newWheelItem);
    }

    /// <summary>Bo banh khoi socket: xoa sach moi child (sync khi loadout khong co data cho socket nay).</summary>
    private void ClearWheelVisual(WheelSocket socket)
    {
        socket.ClearAllChildren();
    }

    private GameObject GetWheelPrefab(CarPart part)
        => (part != null && part.wheelPrefab != null) ? part.wheelPrefab : defaultWheelPrefab;

    private void ApplyPaintToSlot(GarageCarSlot slot, Material mat)
    {
        var seen = new HashSet<Renderer>();

        foreach (CarPaintTarget target in slot.GetComponentsInChildren<CarPaintTarget>(true))
        {
            if (target.bodyRenderer == null) continue;
            seen.Add(target.bodyRenderer);
            SetRendererSlot(target.bodyRenderer, target.materialSlotIndex, mat);
        }

        foreach (Renderer r in slot.GetComponentsInChildren<Renderer>(true))
        {
            if (!seen.Contains(r) && r.CompareTag("PaintPart"))
                SetRendererSlot(r, r.sharedMaterials.Length - 1, mat);
        }
    }

    private static void SetRendererSlot(Renderer renderer, int slotIndex, Material mat)
    {
        Material[] mats = renderer.sharedMaterials;
        if (slotIndex < 0 || slotIndex >= mats.Length) return;

        mats[slotIndex] = mat;
        renderer.sharedMaterials = mats;
    }

    private static string GetLegacyTiresKey(string carName) => TIRES_PREFIX + carName;

    private static string GetTiresKey(string carName, string wheelPosition)
        => $"{TIRES_PREFIX}{carName}_{wheelPosition}";

    private static string GetBrakeKey(string carName, string brakePosition)
        => $"{BRAKE_PREFIX}{carName}_{brakePosition}";

#if UNITY_EDITOR
    /// <summary>
    /// Reset đầy đủ 1 xe:
    ///  1. Xóa PlayerPrefs (paint / tires / brakes)
    ///  2. Xóa wheels + brakes trong loadout asset → xe "thiếu part"
    ///  3. Tháo visual: detach + ẩn tất cả wheel/brake đang gắn trên xe trong scene
    /// Gọi được cả runtime lẫn Editor.
    /// </summary>
    public void ResetCarToDefault(CarLoadoutSlot loadoutSlot)
    {
        if (loadoutSlot == null) return;

        string carName = loadoutSlot.gameObject.name;
        GarageCarSlot carSlot = loadoutSlot.GetComponent<GarageCarSlot>();

        // 1. Xóa PlayerPrefs
        PlayerPrefs.DeleteKey(PAINT_PREFIX + carName);
        PlayerPrefs.DeleteKey(GetLegacyTiresKey(carName));

        if (carSlot != null)
        {
            foreach (WheelSocket ws in carSlot.GetComponentsInChildren<WheelSocket>(true))
                PlayerPrefs.DeleteKey(GetTiresKey(carName, ws.name));

            foreach (BrakeSocket bs in carSlot.GetComponentsInChildren<BrakeSocket>(true))
                PlayerPrefs.DeleteKey(GetBrakeKey(carName, bs.name));
        }

        PlayerPrefs.Save();

        // 2. Xóa loadout asset (wheels + brakes → xe thiếu part) + ghi xuống disk
        if (loadoutSlot.loadout != null)
        {
            loadoutSlot.loadout.wheels?.Clear();
            loadoutSlot.loadout.brakes?.Clear();
            loadoutSlot.loadout.paint = null;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(loadoutSlot.loadout);
            // QUAN TRỌNG: SetDirty chỉ đánh dấu, chưa ghi file.
            // Không có dòng này → Unity reload .asset cũ từ disk lần play tiếp → dữ liệu vẫn còn.
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        // 3. Xóa SẠCH child của mọi socket trong scene (đồng bộ với loadout rỗng)
        if (carSlot != null)
        {
            foreach (WheelSocket ws in carSlot.GetComponentsInChildren<WheelSocket>(true))
                ws.ClearAllChildren();

            foreach (BrakeSocket bs in carSlot.GetComponentsInChildren<BrakeSocket>(true))
                bs.ClearAllChildren();
        }

        // Log missing parts
        if (loadoutSlot.loadout != null)
        {
            var missing = loadoutSlot.loadout.GetMissingParts();
            Debug.Log($"[GarageSaveManager] Reset '{carName}' xong. Thiếu: {missing}");
        }
    }

    /// <summary>
    /// Kiểm tra xe thiếu part nào dựa trên loadout hiện tại.
    /// Trả về PlayerCarLoadout.MissingParts (flags).
    /// </summary>
    public PlayerCarLoadout.MissingParts GetMissingParts(CarLoadoutSlot loadoutSlot)
        => loadoutSlot?.loadout?.GetMissingParts() ?? PlayerCarLoadout.MissingParts.None;

    [ContextMenu("Clear All Saved State (Debug)")]
    private void DbgClear()
    {
        var loadoutSlots = FindObjectsByType<CarLoadoutSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (CarLoadoutSlot slot in loadoutSlots)
            ResetCarToDefault(slot);

        Debug.Log($"[GarageSaveManager] Reset {loadoutSlots.Length} xe — PlayerPrefs + loadout asset đã xóa.");

        foreach (CarLoadoutSlot slot in loadoutSlots)
        {
            if (slot?.loadout == null) continue;
            var missing = slot.loadout.GetMissingParts();
            Debug.Log($"  {slot.name}: {(missing == PlayerCarLoadout.MissingParts.None ? "OK" : "THIẾU " + missing)}");
        }
    }
#endif
}
