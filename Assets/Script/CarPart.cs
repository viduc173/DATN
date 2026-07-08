using UnityEngine;

/// <summary>
/// 1 linh kiện có thể lắp lên xe — đại diện cho engine, lốp, phanh, body kit...
/// Mỗi part chứa CarStats delta (có thể âm/dương, kiểu game-design 0-100).
/// PlayerCarLoadout.GetEffectiveStats() cộng dồn parts vào baseStats (CarStats.operator+
/// tự clamp 0-100).
///
/// Tạo asset: Right-click Project -> Create -> Race -> Car Part
/// Vd: "Turbo V2" thuộc slot Engine, statBonus.acceleration = +10.
/// </summary>
[CreateAssetMenu(fileName = "CarPart", menuName = "Race/Car Part", order = 10)]
public class CarPart : ScriptableObject
{
    public enum PartSlot
    {
        Engine,       // 0
        Wheels,       // 1 (trước đây tên 'Tires' — giữ nguyên int = 1 nên asset cũ không vỡ)
        Brakes,       // 2
        Suspension,   // 3
        Body,         // 4
        Aero,         // 5
        Other,        // 6
        ECU,          // 7 — linh kiện stat-only (không model), giống Engine
        Paint,        // 8 — dành cho part sơn có stat (paint thường lưu ở PlayerCarLoadout.paint dạng Material)
    }

    [Header("Identity")]
    public string partName = "Untitled Part";

    [Tooltip("Slot mà part này lắp vào — dùng cho UI lọc/sort. Logic không enforce 1 slot = 1 part.")]
    public PartSlot slot = PartSlot.Other;

    [TextArea(2, 4)]
    public string description;

    public Sprite icon;

    [Header("Stat Bonus (delta cộng vào base, kiểu 0-100)")]
    [Tooltip("Vd part 'Turbo V2': acceleration = +10. Trade-off có thể negative: 'Heavy Armor' tăng braking nhưng giảm acceleration.")]
    public CarStats statBonus = new CarStats
    {
        maxSpeed     = 0f,
        acceleration = 0f,
        grip         = 0f,
        braking      = 0f,
        handling     = 0f,
    };

    [Header("Visual (Garage)")]
    [Tooltip("WheelItem prefab spawned at each WheelSocket when restoring this tire from save.")]
    public GameObject wheelPrefab;

    [Tooltip("BrakeItem prefab spawned on left brake sockets (FL/RL).")]
    public GameObject brakePrefabLeft;

    [Tooltip("BrakeItem prefab spawned on right brake sockets (FR/RR).")]
    public GameObject brakePrefabRight;

    [Tooltip("Spray can prefab spawned at CartPartPlace > Spray (slot = Paint).")]
    public GameObject sprayPrefab;

    [Header("Meta")]
    [Min(0)] public int tier = 1;
    [Min(0)] public int costGold = 0;
}
