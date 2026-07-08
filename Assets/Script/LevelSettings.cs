using UnityEngine;

/// <summary>
/// MASTER cấu hình cho 1 màn chơi đua xe. 1 asset = 1 màn.
/// Bao gồm: rule race (số vòng, scene end), countdown, và reference tới
/// PlayerCarLoadout (xe + linh kiện player).
///
/// LevelController trong scene đọc asset này lúc Awake() và apply lên:
///  - RacePositionTracker (totalLaps, lapWrapThreshold)
///  - MatchWaitTime (waitTime)
///  - Player VehicleController (stats từ PlayerCarLoadout.GetEffectiveStats())
///
/// Tạo asset: Right-click Project -> Create -> Race -> Level Settings
/// </summary>
[CreateAssetMenu(fileName = "LevelSettings", menuName = "Race/Level Settings", order = 0)]
public class LevelSettings : ScriptableObject
{
    [Header("Race Rules")]
    [Tooltip("Số vòng cần hoàn thành để về đích")]
    [Min(1)] public int totalLaps = 3;

    [Tooltip("Ngưỡng wrap để detect 1 vòng. 0 = tự tính checkpoints.Count / 4")]
    [Min(0)] public int lapWrapThreshold = 0;

    [Header("Match Setup")]
    [Tooltip("Thời gian countdown trước khi bắt đầu (giây)")]
    [Min(0f)] public float countdownTime = 5f;

    [Header("Stat Requirements (gate vào màn)")]
    [Tooltip("Xe player phải có CarStats >= từng giá trị ở đây mới được chạy màn này. Stat = 0 nghĩa là KHÔNG yêu cầu stat đó. Vd: maxSpeed=60 + grip=50, các stat khác=0 → chỉ check 2 stat đó.")]
    public CarStats statRequirements = new CarStats { maxSpeed = 0f, acceleration = 0f, grip = 0f, braking = 0f, handling = 0f };

    [Tooltip("Mô tả ngắn hiển thị UI level select. Vd: 'Yêu cầu: Top Speed 60+, Grip 50+'.")]
    [TextArea(1, 3)] public string requirementsDescription;

    [Header("Player Car & Stats")]
    [Tooltip("Loadout của player (xe + linh kiện). Null = không override stats, dùng giá trị có sẵn trong scene.")]
    public PlayerCarLoadout playerLoadout;

    [Tooltip("Apply stat overrides lên VehicleController khi scene start")]
    public bool applyPlayerStatOverrides = true;

    [Tooltip("Spawn lại xe player từ prefab (PlayerCarLoadout.carPrefab) thay vì dùng xe có sẵn. Cần playerSpawnPoint trên LevelController.")]
    public bool spawnPlayerCarFromPrefab = false;

    [Header("On Race Finish")]
    public bool autoLoadSceneOnFinish = true;

    [Tooltip("Tên scene sẽ load khi player về đích. Phải có trong Build Settings.")]
    public string endSceneName = "GarageLobby_pc";

    [Tooltip("Số giây chờ sau khi player về đích rồi mới load scene")]
    [Min(0f)] public float loadSceneDelay = 3f;
}
