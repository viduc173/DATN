using UnityEngine;
using EVP;

/// <summary>
/// Master controller cho 1 màn chơi. Đọc LevelSettings (ScriptableObject) và apply
/// xuống các hệ thống đã có trong scene: RacePositionTracker, MatchWaitTime, Player VehicleController.
///
/// Stat overrides: PlayerCarLoadout chứa CarStats abstract 0-100 (maxSpeed, acceleration,
/// grip, braking, handling). LevelController.ApplyStatsTo() MAP abstract → physics field
/// trên VehicleController bằng MapStat() (50 abstract = giá trị default của VehicleController).
///
/// Awake() chạy TRƯỚC Start() của RacePositionTracker / MatchWaitTime để các override
/// có hiệu lực ngay khi 2 component đó khởi tạo.
/// </summary>
[DefaultExecutionOrder(-100)]
public class LevelController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Asset LevelSettings — chứa toàn bộ rule + player loadout của màn này")]
    public LevelSettings settings;

    [Header("Scene References")]
    [Tooltip("Component RacePositionTracker trong scene. Nếu null sẽ tự FindObjectOfType.")]
    public RacePositionTracker raceTracker;

    [Tooltip("Component MatchWaitTime trong scene. Nếu null sẽ tự FindObjectOfType.")]
    public MatchWaitTime matchWaitTime;

    [Tooltip("VehicleController của player trong scene. Nếu null sẽ tìm theo tag 'Player'.")]
    public VehicleController playerVehicle;

    [Tooltip("(Tùy chọn) Spawn point để spawn xe player từ prefab — chỉ dùng khi settings.spawnPlayerCarFromPrefab = true.")]
    public Transform playerSpawnPoint;

    [Header("Debug")]
    public bool showDebugInfo = true;

    // ─── Mapping ranges: abstract 0-100 → physics value ─────────────────────────
    // Tại abstract = 50, output = default value của VehicleController gốc.
    // Có thể tune từng range nếu cần (qua [SerializeField] nếu muốn expose Inspector).

    private const float SPEED_FORWARD_MIN = 13.89f,  SPEED_FORWARD_DEF = 27.78f, SPEED_FORWARD_MAX = 55.56f; // m/s
    private const float DRIVE_FORCE_MIN   = 500f,    DRIVE_FORCE_DEF   = 2000f,  DRIVE_FORCE_MAX   = 5000f;  // N
    private const float TIRE_FRICTION_MIN = 0.5f,    TIRE_FRICTION_DEF = 1.0f,   TIRE_FRICTION_MAX = 1.5f;
    private const float BRAKE_FORCE_MIN   = 1000f,   BRAKE_FORCE_DEF   = 3000f,  BRAKE_FORCE_MAX   = 6000f;  // N
    private const float STEER_ANGLE_MIN   = 20f,     STEER_ANGLE_DEF   = 35f,    STEER_ANGLE_MAX   = 55f;    // deg

    void Awake()
    {
        if (settings == null)
        {
            Debug.LogError("[LevelController] LevelSettings chưa gán — bỏ qua apply.");
            return;
        }

        ResolveSceneReferences();
        SpawnOrAdoptPlayerCar();
        ApplyToRaceTracker();
        ApplyToMatchWaitTime();
        ApplyPlayerLoadoutStats();
    }

    private void ResolveSceneReferences()
    {
        if (raceTracker == null)
            raceTracker = FindObjectOfType<RacePositionTracker>();

        if (matchWaitTime == null)
            matchWaitTime = FindObjectOfType<MatchWaitTime>();

        if (playerVehicle == null)
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
                playerVehicle = playerGo.GetComponent<VehicleController>();
        }

        if (showDebugInfo)
            Debug.Log($"[LevelController] References — tracker:{raceTracker != null} wait:{matchWaitTime != null} player:{playerVehicle != null}");
    }

    private void SpawnOrAdoptPlayerCar()
    {
        if (!settings.spawnPlayerCarFromPrefab) return;
        if (settings.playerLoadout == null || settings.playerLoadout.carPrefab == null)
        {
            Debug.LogWarning("[LevelController] spawnPlayerCarFromPrefab = true nhưng PlayerCarLoadout.carPrefab null — bỏ qua spawn.");
            return;
        }
        if (playerSpawnPoint == null)
        {
            Debug.LogWarning("[LevelController] spawnPlayerCarFromPrefab = true nhưng playerSpawnPoint null — bỏ qua spawn.");
            return;
        }

        if (playerVehicle != null)
        {
            playerVehicle.gameObject.SetActive(false);
            if (showDebugInfo)
                Debug.Log($"[LevelController] Disabled xe cũ '{playerVehicle.name}'");
        }

        var newCar = Instantiate(settings.playerLoadout.carPrefab,
                                 playerSpawnPoint.position,
                                 playerSpawnPoint.rotation);
        newCar.tag = "Player";
        playerVehicle = newCar.GetComponent<VehicleController>();

        if (showDebugInfo)
            Debug.Log($"[LevelController] Spawned player car từ prefab '{settings.playerLoadout.carPrefab.name}'");
    }

    private void ApplyToRaceTracker()
    {
        if (raceTracker == null) return;

        raceTracker.totalLaps = Mathf.Max(1, settings.totalLaps);
        if (showDebugInfo)
            Debug.Log($"[LevelController] RacePositionTracker.totalLaps = {raceTracker.totalLaps}");
    }

    private void ApplyToMatchWaitTime()
    {
        if (matchWaitTime == null) return;

        matchWaitTime.SetWaitTime(settings.countdownTime);
        if (showDebugInfo)
            Debug.Log($"[LevelController] MatchWaitTime.waitTime = {settings.countdownTime}s");
    }

    private void ApplyPlayerLoadoutStats()
    {
        if (!settings.applyPlayerStatOverrides) return;
        if (playerVehicle == null)
        {
            Debug.LogWarning("[LevelController] Không tìm thấy player VehicleController để apply stats.");
            return;
        }

        // Ưu tiên loadout từ xe đang chọn trong garage; fallback về LevelSettings.playerLoadout
        var loadout = ActiveLoadout.Current ?? settings.playerLoadout;
        if (loadout == null) return;

        var stats = loadout.GetEffectiveStats();
        ApplyStatsTo(playerVehicle, stats);

        if (showDebugInfo)
            Debug.Log($"[LevelController] Applied loadout '{loadout.loadoutName}'" +
                      $"{(ActiveLoadout.Current != null ? " (from ActiveLoadout)" : " (from LevelSettings)")} " +
                      $"| spd={stats.maxSpeed:0} acc={stats.acceleration:0} grip={stats.grip:0} brake={stats.braking:0} hand={stats.handling:0}" +
                      $"→ speedFwd={playerVehicle.maxSpeedForward:0.#} drive={playerVehicle.maxDriveForce:0} steer={playerVehicle.maxSteerAngle:0.#}°");
    }

    /// <summary>
    /// Map CarStats abstract (0-100) → physics field của VehicleController.
    /// Tại 50: output = default. 0..50 lerp từ MIN→DEF, 50..100 lerp từ DEF→MAX.
    /// </summary>
    private void ApplyStatsTo(VehicleController vc, CarStats stats)
    {
        vc.maxSpeedForward = MapStat(stats.maxSpeed,     SPEED_FORWARD_MIN, SPEED_FORWARD_DEF, SPEED_FORWARD_MAX);
        vc.maxDriveForce   = MapStat(stats.acceleration, DRIVE_FORCE_MIN,   DRIVE_FORCE_DEF,   DRIVE_FORCE_MAX);
        vc.tireFriction    = MapStat(stats.grip,         TIRE_FRICTION_MIN, TIRE_FRICTION_DEF, TIRE_FRICTION_MAX);
        vc.maxBrakeForce   = MapStat(stats.braking,      BRAKE_FORCE_MIN,   BRAKE_FORCE_DEF,   BRAKE_FORCE_MAX);
        vc.maxSteerAngle   = MapStat(stats.handling,     STEER_ANGLE_MIN,   STEER_ANGLE_DEF,   STEER_ANGLE_MAX);
    }

    /// <summary>
    /// Piecewise lerp: abstract 0..50 → min..def, abstract 50..100 → def..max.
    /// Đảm bảo abstract=50 luôn map về def (giá trị default gốc của VehicleController).
    /// </summary>
    private static float MapStat(float abstractValue, float min, float def, float max)
    {
        abstractValue = Mathf.Clamp(abstractValue, 0f, 100f);
        if (abstractValue <= 50f)
            return Mathf.Lerp(min, def, abstractValue / 50f);
        else
            return Mathf.Lerp(def, max, (abstractValue - 50f) / 50f);
    }
}
