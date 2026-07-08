using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// "Bộ xe + linh kiện đang lắp" của 1 player. Mỗi loại linh kiện có slot RIÊNG (không còn
/// list phẳng): Paint, Engine, Suspension, ECU (1 cái/slot) và Wheels, Brakes (4 cái — mỗi
/// góc 1 part: FL, FR, RL, RR).
///
/// LevelController đọc PlayerCarLoadout, gọi GetEffectiveStats() để có CarStats (0-100
/// abstract), rồi MAP sang physics fields của VehicleController.
///
/// Tạo asset: Right-click Project -> Create -> Race -> Player Car Loadout
/// </summary>
[CreateAssetMenu(fileName = "PlayerCarLoadout", menuName = "Race/Player Car Loadout", order = 5)]
public class PlayerCarLoadout : ScriptableObject
{
    [Header("Identity")]
    public string loadoutName = "Default Loadout";

    [Tooltip("(Tùy chọn) Prefab xe — nếu set, LevelController có thể spawn xe này tại spawn point thay vì dùng xe có sẵn trong scene.")]
    public GameObject carPrefab;

    [Header("Shop (bán xe)")]
    [Tooltip("Icon hiển thị trong shop/inventory cho xe này.")]
    public Sprite icon;

    [Tooltip("Giá mua xe (gold). 0 = miễn phí / xe mặc định.")]
    [Min(0)] public int costGold = 0;

    [Tooltip("Mô tả xe hiển thị trong shop.")]
    [TextArea(2, 4)] public string description;

    [Header("Base Stats (xe gốc, chưa lắp linh kiện) — thang 0-100")]
    [Tooltip("Stat trung tính = 50. Tăng/giảm để thể hiện xe mạnh/yếu so với baseline.")]
    public CarStats baseStats = new CarStats
    {
        maxSpeed     = 50f,
        acceleration = 50f,
        grip         = 50f,
        braking      = 50f,
        handling     = 50f,
    };

    [Header("Paint")]
    [Tooltip("Material sơn xe đang chọn. Đồng bộ với hệ GP_ PlayerPrefs + CarPaintCan của garage.")]
    public Material paint;

    [Header("Stat-only parts (1 cái/slot, không có model 3D)")]
    [Tooltip("Slot Engine — nên là CarPart.slot = Engine")]
    public CarPart engine;

    [Tooltip("Slot Suspension — nên là CarPart.slot = Suspension")]
    public CarPart suspension;

    [Tooltip("Slot ECU — nên là CarPart.slot = ECU")]
    public CarPart ecu;

    [Header("Per-corner parts (tối đa 4: FL, FR, RL, RR)")]
    [Tooltip("Bánh xe theo từng góc — CarPart.slot = Wheels")]
    public List<CarPart> wheels = new List<CarPart>();

    [Tooltip("Phanh theo từng góc — CarPart.slot = Brakes")]
    public List<CarPart> brakes = new List<CarPart>();

    /// <summary>
    /// Liệt kê tất cả CarPart đang lắp (engine, suspension, ecu + từng wheel + từng brake).
    /// Bỏ qua null. Dùng cho GetEffectiveStats và build catalog.
    /// </summary>
    public IEnumerable<CarPart> AllParts()
    {
        if (engine != null) yield return engine;
        if (suspension != null) yield return suspension;
        if (ecu != null) yield return ecu;

        if (wheels != null)
            foreach (var w in wheels)
                if (w != null) yield return w;

        if (brakes != null)
            foreach (var b in brakes)
                if (b != null) yield return b;
    }

    /// <summary>
    /// Trả về CarStats sau khi cộng tất cả parts đang lắp.
    /// CarStats.operator+ tự clamp về [0, 100].
    /// </summary>
    public CarStats GetEffectiveStats()
    {
        var total = baseStats;
        foreach (var part in AllParts())
        {
            if (part.statBonus == null) continue;
            total = total + part.statBonus;
        }
        return total;
    }

    /// <summary>
    /// Lắp part vào đúng slot dựa theo part.slot. Wheels/Brakes thêm vào list (tối đa 4, không trùng).
    /// </summary>
    public bool EquipPart(CarPart part)
    {
        if (part == null) return false;

        switch (part.slot)
        {
            case CarPart.PartSlot.Engine:     engine = part; return true;
            case CarPart.PartSlot.Suspension: suspension = part; return true;
            case CarPart.PartSlot.ECU:        ecu = part; return true;

            case CarPart.PartSlot.Wheels:
                wheels ??= new List<CarPart>();
                if (wheels.Contains(part) || wheels.Count >= 4) return false;
                wheels.Add(part);
                return true;

            case CarPart.PartSlot.Brakes:
                brakes ??= new List<CarPart>();
                if (brakes.Contains(part) || brakes.Count >= 4) return false;
                brakes.Add(part);
                return true;

            default:
                return false; // Body/Aero/Other/Paint không có slot lưu trong loadout
        }
    }

    /// <summary>
    /// Tháo part khỏi slot tương ứng.
    /// </summary>
    public bool UnequipPart(CarPart part)
    {
        if (part == null) return false;

        switch (part.slot)
        {
            case CarPart.PartSlot.Engine:     if (engine == part)     { engine = null; return true; } return false;
            case CarPart.PartSlot.Suspension: if (suspension == part) { suspension = null; return true; } return false;
            case CarPart.PartSlot.ECU:        if (ecu == part)        { ecu = null; return true; } return false;
            case CarPart.PartSlot.Wheels:     return wheels != null && wheels.Remove(part);
            case CarPart.PartSlot.Brakes:     return brakes != null && brakes.Remove(part);
            default:                          return false;
        }
    }

    /// <summary>True nếu part đang được lắp ở bất kỳ slot nào.</summary>
    public bool HasPart(CarPart part)
    {
        if (part == null) return false;
        foreach (var p in AllParts())
            if (p == part) return true;
        return false;
    }

    // ─── Reset ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Xoá các part đang lắp (engine/suspension/ecu/wheels/brakes). GIỮ NGUYÊN
    /// loadoutName, carPrefab, icon, costGold, description, baseStats, paint.
    /// </summary>
    public void ResetParts()
    {
        engine = null;
        suspension = null;
        ecu = null;
        wheels ??= new List<CarPart>();
        brakes ??= new List<CarPart>();
        wheels.Clear();
        brakes.Clear();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[PlayerCarLoadout: {loadoutName}] Reset equipped parts (identity/stats kept).");
    }

    // ─── Missing Parts ────────────────────────────────────────────────────────

    [System.Flags]
    public enum MissingParts
    {
        None       = 0,
        Engine     = 1 << 0,
        Suspension = 1 << 1,
        Wheels     = 1 << 2,   // < 4 wheels hoặc có slot null
        Brakes     = 1 << 3,   // < 4 brakes hoặc có slot null
        Paint      = 1 << 4,   // paint chưa chọn
    }

    /// <summary>
    /// Kiểm tra xe đang thiếu part nào. Dùng sau reset hoặc để hiển thị cảnh báo UI.
    /// Wheels/Brakes yêu cầu đủ 4 cái (per-corner), không có null.
    /// </summary>
    public MissingParts GetMissingParts()
    {
        MissingParts missing = MissingParts.None;

        if (engine == null)     missing |= MissingParts.Engine;
        if (suspension == null) missing |= MissingParts.Suspension;
        if (paint == null)      missing |= MissingParts.Paint;

        bool wheelsOk = wheels != null && wheels.Count == 4 && !wheels.Exists(w => w == null);
        if (!wheelsOk) missing |= MissingParts.Wheels;

        bool brakesOk = brakes != null && brakes.Count == 4 && !brakes.Exists(b => b == null);
        if (!brakesOk) missing |= MissingParts.Brakes;

        return missing;
    }

    public bool HasAnyMissingPart() => GetMissingParts() != MissingParts.None;
}
