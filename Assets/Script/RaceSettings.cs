using UnityEngine;

/// <summary>
/// Cấu hình cho 1 race: số vòng đua, scene sẽ load khi kết thúc, delay.
/// Tạo asset: Right-click Project -> Create -> Race -> Race Settings.
/// Gán asset này vào field "raceSettings" của RacePositionTracker.
/// </summary>
[CreateAssetMenu(fileName = "RaceSettings", menuName = "Race/Race Settings", order = 0)]
public class RaceSettings : ScriptableObject
{
    [Header("Race Rules")]
    [Tooltip("Số vòng cần hoàn thành để về đích")]
    [Min(1)]
    public int totalLaps = 3;

    [Header("On Race Finish")]
    [Tooltip("Có tự load scene khi PLAYER về đích không")]
    public bool autoLoadSceneOnFinish = true;

    [Tooltip("Tên scene sẽ load khi player về đích (phải có trong Build Settings). Để trống nếu không tự load.")]
    public string endSceneName = "GarageLobby_pc";

    [Tooltip("Số giây chờ sau khi player về đích rồi mới load scene (cho phép xem kết quả)")]
    [Min(0f)]
    public float loadSceneDelay = 3f;

    [Header("Lap Detection")]
    [Tooltip("Ngưỡng (số checkpoint) để xác định 1 lần wrap quanh start/finish line là 1 vòng. Mặc định count/4.")]
    [Min(1)]
    public int lapWrapThreshold = 0; // 0 = tự tính count/4

    [Header("Lap Bonus (spawn bonus mỗi vòng)")]
    [Tooltip("Số bonus spawn ra cho MỖI vòng đua (gần các checkpoint). 0 = không spawn. " +
             "Dùng bởi LapBonusSpawner.")]
    [Min(0)]
    public int bonusesPerLap = 3;

    [Header("Rewards (gold theo hạng về đích)")]
    [Tooltip("Tiền thưởng (gold) theo hạng về đích. index 0 = hạng 1, index 1 = hạng 2... " +
             "Chỉ những hạng có trong mảng mới được thưởng (vd 3 phần tử = top 3).")]
    public int[] prizeByPosition = { 1000, 600, 300 };

    /// <summary>
    /// Tiền thưởng cho 1 vị trí về đích (1-based). Trả 0 nếu vị trí không hợp lệ
    /// hoặc nằm ngoài bảng thưởng (không thuộc top được thưởng).
    /// </summary>
    public int GetPrize(int position)
    {
        if (prizeByPosition == null) return 0;
        int index = position - 1;
        if (index < 0 || index >= prizeByPosition.Length) return 0;
        return Mathf.Max(0, prizeByPosition[index]);
    }
}
