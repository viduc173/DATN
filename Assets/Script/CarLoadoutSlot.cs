using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Links a physical car in the garage to its PlayerCarLoadout asset.
/// Wheel tires are tracked per socket so each of the four wheels can use a different CarPart.
/// </summary>
public class CarLoadoutSlot : MonoBehaviour
{
    [Tooltip("PlayerCarLoadout asset for this car.")]
    [SerializeField] public PlayerCarLoadout loadout;

    [Header("Events")]
    public UnityEvent<CarPart> onPartEquipped;
    public UnityEvent<CarPart> onPartUnequipped;
    public event UnityAction<CarStats> StatsChanged;

    private readonly Dictionary<string, CarPart> _tiresByWheelPosition = new Dictionary<string, CarPart>();
    private readonly Dictionary<string, CarPart> _brakesByPosition = new Dictionary<string, CarPart>();

    /// <summary>
    /// Backward-compatible tire equip: applies one tire part to the whole car.
    /// New wheel code should call EquipTires(part, wheelPosition).
    /// </summary>
    public void EquipTires(CarPart part)
    {
        if (loadout == null || part == null) return;

        _tiresByWheelPosition.Clear();
        _tiresByWheelPosition["All"] = part;
        RebuildTiresInLoadout();

        onPartEquipped?.Invoke(part);
        NotifyStatsChanged();

        MarkDirty();
        Debug.Log($"[CarLoadoutSlot: {name}] Tires -> {part.partName} (loadout: {loadout.loadoutName})");
    }

    public void EquipTires(CarPart part, string wheelPosition)
    {
        if (loadout == null || part == null || string.IsNullOrEmpty(wheelPosition)) return;

        EnsureTirePositionCacheFromLoadout();
        _tiresByWheelPosition[wheelPosition] = part;
        RebuildTiresInLoadout();

        onPartEquipped?.Invoke(part);
        NotifyStatsChanged();

        MarkDirty();
        Debug.Log($"[CarLoadoutSlot: {name}] Tires[{wheelPosition}] -> {part.partName} (loadout: {loadout.loadoutName})");
    }

    /// <summary>
    /// Restore one tire for the whole car. Kept for old saves that only have GW_{carName}.
    /// </summary>
    internal void RestoreTires(CarPart part)
    {
        if (loadout == null || part == null) return;

        _tiresByWheelPosition.Clear();
        _tiresByWheelPosition["All"] = part;
        RebuildTiresInLoadout();
        MarkDirty();
    }

    internal void RestoreTires(CarPart part, string wheelPosition)
    {
        if (loadout == null || part == null || string.IsNullOrEmpty(wheelPosition)) return;

        _tiresByWheelPosition[wheelPosition] = part;
        RebuildTiresInLoadout();
        MarkDirty();
    }

    public void UnequipTires(CarPart part)
    {
        if (loadout == null || part == null) return;

        EnsureTirePositionCacheFromLoadout();
        bool changed = false;
        var positionsToRemove = new List<string>();
        foreach (var kvp in _tiresByWheelPosition)
        {
            if (kvp.Value == part)
                positionsToRemove.Add(kvp.Key);
        }

        foreach (string position in positionsToRemove)
        {
            _tiresByWheelPosition.Remove(position);
            changed = true;
        }

        if (!changed) return;

        RebuildTiresInLoadout();
        onPartUnequipped?.Invoke(part);
        NotifyStatsChanged();
        MarkDirty();
        Debug.Log($"[CarLoadoutSlot: {name}] Removed tires: {part.partName}");
    }

    public void UnequipTires(CarPart part, string wheelPosition)
    {
        if (loadout == null || part == null || string.IsNullOrEmpty(wheelPosition)) return;

        EnsureTirePositionCacheFromLoadout();
        if (!_tiresByWheelPosition.TryGetValue(wheelPosition, out CarPart current) || current != part)
            return;

        _tiresByWheelPosition.Remove(wheelPosition);
        RebuildTiresInLoadout();

        onPartUnequipped?.Invoke(part);
        NotifyStatsChanged();
        MarkDirty();
        Debug.Log($"[CarLoadoutSlot: {name}] Removed tires[{wheelPosition}]: {part.partName}");
    }

    public void EquipBrake(CarPart part, string brakePosition)
    {
        if (loadout == null || part == null || string.IsNullOrEmpty(brakePosition)) return;

        EnsureBrakePositionCacheFromLoadout();
        _brakesByPosition[brakePosition] = part;
        RebuildBrakesInLoadout();

        onPartEquipped?.Invoke(part);
        NotifyStatsChanged();

        MarkDirty();
        Debug.Log($"[CarLoadoutSlot: {name}] Brake[{brakePosition}] -> {part.partName} (loadout: {loadout.loadoutName})");
    }

    internal void RestoreBrake(CarPart part, string brakePosition)
    {
        if (loadout == null || part == null || string.IsNullOrEmpty(brakePosition)) return;

        _brakesByPosition[brakePosition] = part;
        RebuildBrakesInLoadout();
        MarkDirty();
    }

    public void UnequipBrake(CarPart part, string brakePosition)
    {
        if (loadout == null || part == null || string.IsNullOrEmpty(brakePosition)) return;

        EnsureBrakePositionCacheFromLoadout();
        if (!_brakesByPosition.TryGetValue(brakePosition, out CarPart current) || current != part)
            return;

        _brakesByPosition.Remove(brakePosition);
        RebuildBrakesInLoadout();

        onPartUnequipped?.Invoke(part);
        NotifyStatsChanged();
        MarkDirty();
        Debug.Log($"[CarLoadoutSlot: {name}] Removed brake[{brakePosition}]: {part.partName}");
    }

    public CarPart GetTireAt(string wheelPosition)
    {
        if (loadout == null || string.IsNullOrEmpty(wheelPosition)) return null;

        EnsureTirePositionCacheFromLoadout();
        return _tiresByWheelPosition.TryGetValue(wheelPosition, out CarPart part) ? part : null;
    }

    public CarPart GetBrakeAt(string brakePosition)
    {
        if (loadout == null || string.IsNullOrEmpty(brakePosition)) return null;

        EnsureBrakePositionCacheFromLoadout();
        return _brakesByPosition.TryGetValue(brakePosition, out CarPart part) ? part : null;
    }

    public void EquipPart(CarPart part)
    {
        if (loadout == null || part == null) return;

        loadout.EquipPart(part);
        onPartEquipped?.Invoke(part);
        NotifyStatsChanged();
        MarkDirty();
    }

    public void UnequipPart(CarPart part)
    {
        if (loadout == null || part == null) return;

        if (loadout.UnequipPart(part))
        {
            onPartUnequipped?.Invoke(part);
            NotifyStatsChanged();
            MarkDirty();
        }
    }

    /// <summary>
    /// Đồng bộ TẤT CẢ socket bánh/phanh của xe này theo loadout HIỆN TẠI:
    ///   - Socket có part trong loadout  → hiện đúng bánh/phanh đó.
    ///   - Socket KHÔNG có part (thiếu)  → xóa sạch child của socket (xe để trống).
    /// Socket KHÔNG tự init bánh baked nữa — loadout là nguồn sự thật duy nhất.
    /// Ưu tiên engine của GarageSaveManager (spawn prefab theo loadout); nếu không có
    /// GarageSaveManager thì fallback: clear socket thiếu data theo index loadout.
    /// </summary>
    public void SyncSocketsFromLoadout()
    {
        if (loadout == null) return;

        // Mirroring the loadout spawns/attaches wheels programmatically; those attaches/detaches
        // must NOT touch PlayerInventory (only genuine player mount/unmount does).
        PartInventoryBridge.Suppressed = true;
        try
        {
            var carSlot = GetComponent<GarageCarSlot>();
            if (GarageSaveManager.Instance != null && carSlot != null)
            {
                GarageSaveManager.Instance.SyncCar(carSlot);
                return;
            }

            // Fallback (không có GarageSaveManager): clear theo index loadout.
            var wheelSockets = GetComponentsInChildren<WheelSocket>(true);
            for (int i = 0; i < wheelSockets.Length; i++)
            {
                bool hasData = loadout.wheels != null && i < loadout.wheels.Count && loadout.wheels[i] != null;
                if (!hasData) wheelSockets[i].ClearAllChildren();
            }

            var brakeSockets = GetComponentsInChildren<BrakeSocket>(true);
            for (int i = 0; i < brakeSockets.Length; i++)
            {
                bool hasData = loadout.brakes != null && i < loadout.brakes.Count && loadout.brakes[i] != null;
                if (!hasData) brakeSockets[i].ClearAllChildren();
            }
        }
        finally
        {
            PartInventoryBridge.Suppressed = false;
        }
    }

    /// <summary>
    /// Loadout hiện tại có bánh cho socket này không? (map theo index socket trong cây xe).
    /// Socket tự gọi ở Start để biết có nên xóa child hay không. Loadout là nguồn sự thật.
    /// </summary>
    public bool LoadoutHasWheelFor(WheelSocket socket)
    {
        if (loadout == null || loadout.wheels == null || socket == null) return false;
        var sockets = GetComponentsInChildren<WheelSocket>(true);
        int idx = System.Array.IndexOf(sockets, socket);
        return idx >= 0 && idx < loadout.wheels.Count && loadout.wheels[idx] != null;
    }

    /// <summary>Loadout hiện tại có phanh cho socket này không? (map theo index socket trong cây xe).</summary>
    public bool LoadoutHasBrakeFor(BrakeSocket socket)
    {
        if (loadout == null || loadout.brakes == null || socket == null) return false;
        var sockets = GetComponentsInChildren<BrakeSocket>(true);
        int idx = System.Array.IndexOf(sockets, socket);
        return idx >= 0 && idx < loadout.brakes.Count && loadout.brakes[idx] != null;
    }

    public CarStats GetEffectiveStats()
        => loadout != null ? loadout.GetEffectiveStats() : default;

    public bool HasPart(CarPart part)
        => loadout != null && loadout.HasPart(part);

    private void RebuildTiresInLoadout()
    {
        if (loadout == null) return;

        loadout.wheels ??= new List<CarPart>();
        loadout.wheels.Clear();

        if (_tiresByWheelPosition.TryGetValue("All", out CarPart allTire))
        {
            var allSockets = GetComponentsInChildren<WheelSocket>(true);
            int count = allSockets.Length > 0 ? allSockets.Length : 4;
            for (int i = 0; i < count; i++)
                loadout.wheels.Add(allTire);
            return;
        }

        var sockets = GetComponentsInChildren<WheelSocket>(true);
        foreach (WheelSocket socket in sockets)
        {
            _tiresByWheelPosition.TryGetValue(socket.name, out CarPart tire);
            loadout.wheels.Add(tire);
        }
    }

    private void RebuildBrakesInLoadout()
    {
        if (loadout == null) return;

        loadout.brakes ??= new List<CarPart>();
        loadout.brakes.Clear();

        var sockets = GetComponentsInChildren<BrakeSocket>(true);
        foreach (BrakeSocket socket in sockets)
        {
            _brakesByPosition.TryGetValue(socket.name, out CarPart brake);
            loadout.brakes.Add(brake);
        }
    }

    private void EnsureTirePositionCacheFromLoadout()
    {
        if (_tiresByWheelPosition.Count > 0 || loadout == null || loadout.wheels == null)
            return;

        var sockets = GetComponentsInChildren<WheelSocket>(true);
        for (int i = 0; i < sockets.Length && i < loadout.wheels.Count; i++)
        {
            CarPart tire = loadout.wheels[i];
            if (tire != null)
                _tiresByWheelPosition[sockets[i].name] = tire;
        }
    }

    private void EnsureBrakePositionCacheFromLoadout()
    {
        if (_brakesByPosition.Count > 0 || loadout == null || loadout.brakes == null)
            return;

        var sockets = GetComponentsInChildren<BrakeSocket>(true);
        for (int i = 0; i < sockets.Length && i < loadout.brakes.Count; i++)
        {
            CarPart brake = loadout.brakes[i];
            if (brake != null)
                _brakesByPosition[sockets[i].name] = brake;
        }
    }

    private void MarkDirty()
    {
#if UNITY_EDITOR
        if (loadout != null)
            UnityEditor.EditorUtility.SetDirty(loadout);
#endif
    }

    private void NotifyStatsChanged()
    {
        if (loadout != null)
            StatsChanged?.Invoke(loadout.GetEffectiveStats());
    }

#if UNITY_EDITOR
    private const string LoadoutFolder = "Assets/Data/Loadouts";

    private void Reset()
    {
        ResolveLoadoutFromAsset();
    }

    private void OnValidate()
    {
        ResolveLoadoutFromAsset();
    }

    [ContextMenu("Resolve Loadout From Asset")]
    private void ResolveLoadoutFromAsset()
    {
        string assetPath = $"{LoadoutFolder}/Loadout_{gameObject.name}.asset";
        PlayerCarLoadout resolved = UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerCarLoadout>(assetPath);

        if (resolved == null || loadout == resolved)
            return;

        loadout = resolved;
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[CarLoadoutSlot: {name}] Resolved loadout from {assetPath}", this);
    }

    [ContextMenu("Log Current Stats")]
    private void DbgLogStats()
    {
        if (loadout == null) { Debug.Log($"[CarLoadoutSlot: {name}] No loadout assigned."); return; }
        CarStats s = loadout.GetEffectiveStats();
        Debug.Log($"[CarLoadoutSlot: {name}] {loadout.loadoutName} -> Speed:{s.maxSpeed:F1} Accel:{s.acceleration:F1} Grip:{s.grip:F1} Brake:{s.braking:F1} Handling:{s.handling:F1}");

        var parts = new List<CarPart>(loadout.AllParts());
        Debug.Log($"  Parts ({parts.Count}):");
        foreach (var p in parts)
            Debug.Log($"    [{p?.slot}] {p?.partName} +({p?.statBonus.maxSpeed:F0},{p?.statBonus.acceleration:F0},{p?.statBonus.grip:F0})");
    }
#endif
}
