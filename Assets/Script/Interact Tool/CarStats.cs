using UnityEngine;

/// <summary>
/// Dữ liệu 5 thông số xe. Dùng làm base stats và delta stats của linh kiện.
/// </summary>
[System.Serializable]
public class CarStats
{
    [Range(0f, 100f)] public float maxSpeed     = 50f;  // Tốc độ tối đa
    [Range(0f, 100f)] public float acceleration = 50f;  // Gia tốc
    [Range(0f, 100f)] public float grip         = 50f;  // Bám đường
    [Range(0f, 100f)] public float braking      = 50f;  // Phanh
    [Range(0f, 100f)] public float handling     = 50f;  // Khả năng lái

    // Stat thứ 6 — chỉ ảnh hưởng NITRO (bình + tốc độ hồi), KHÔNG map sang physics ở LevelController.
    // Default 0 = bình nitro tối thiểu (1.2s) + hồi chậm nhất; linh kiện cộng vào để tăng "thời gian nitro".
    // Xem NitroController.RecomputeFromStats() và Loadout_Stats_Balancing.md §6.
    [Range(0f, 100f)] public float nitro        = 0f;   // Dung lượng + tốc độ hồi nitro

    /// <summary>Cộng delta linh kiện vào stats</summary>
    public static CarStats operator +(CarStats a, CarStats b) => new CarStats
    {
        maxSpeed     = Mathf.Clamp(a.maxSpeed     + b.maxSpeed,     0f, 100f),
        acceleration = Mathf.Clamp(a.acceleration + b.acceleration, 0f, 100f),
        grip         = Mathf.Clamp(a.grip         + b.grip,         0f, 100f),
        braking      = Mathf.Clamp(a.braking      + b.braking,      0f, 100f),
        handling     = Mathf.Clamp(a.handling     + b.handling,     0f, 100f),
        nitro        = Mathf.Clamp(a.nitro        + b.nitro,        0f, 100f),
    };

    /// <summary>Trừ delta khi tháo linh kiện</summary>
    public static CarStats operator -(CarStats a, CarStats b) => new CarStats
    {
        maxSpeed     = Mathf.Clamp(a.maxSpeed     - b.maxSpeed,     0f, 100f),
        acceleration = Mathf.Clamp(a.acceleration - b.acceleration, 0f, 100f),
        grip         = Mathf.Clamp(a.grip         - b.grip,         0f, 100f),
        braking      = Mathf.Clamp(a.braking      - b.braking,      0f, 100f),
        handling     = Mathf.Clamp(a.handling     - b.handling,     0f, 100f),
        nitro        = Mathf.Clamp(a.nitro        - b.nitro,        0f, 100f),
    };
}