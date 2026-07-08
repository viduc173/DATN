using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawn bonus theo VÒNG ĐUA. Mỗi lap sinh ra <c>RaceSettings.bonusesPerLap</c> bonus,
/// đặt gần các checkpoint (con của object có tag "CheckPoint"), nhân bản từ 1 BonusObject
/// mẫu (đang inactive) đặt trong BonusPlace.
///
/// Luồng:
///   • Start         → spawn batch cho Lap 1 (lúc vào race).
///   • onLapCompleted(Player) → spawn batch cho lap kế tiếp (cho tới hết totalLaps).
///   • Mỗi batch (tuỳ chọn) xoá bonus chưa ăn của lap trước để track không bị rác.
///
/// BonusObject mẫu đã tự wire sẵn (PlayerTriggerEvent → bật Effect + ẩn mesh +
/// BonusEvent.TriggerBonusUnityEvent). Instantiate giữ nguyên wiring nội bộ và vẫn trỏ
/// BonusEvent singleton bên ngoài → bonus clone hoạt động đầy đủ, kể cả tăng tốc (SpeedBoost).
/// </summary>
[DefaultExecutionOrder(-80)] // sau LoadSceneController(-110), trước/cùng tracker là đủ
public class LapBonusSpawner : MonoBehaviour
{
    [Header("Prefab (ưu tiên)")]
    [Tooltip("Prefab bonus nhân bản (Assets/Prefabs/Bonus/Object.prefab). Đã wire bằng GUID trong scene. " +
             "Nếu để trống → fallback sang bonusTemplate trong scene.")]
    public GameObject bonusPrefab;

    [Header("Template (fallback nếu không có prefab)")]
    [Tooltip("BonusObject mẫu (inactive) trong BonusPlace. Để trống → tự lấy con đầu của BonusPlace / của object này.")]
    public Transform bonusTemplate;

    [Tooltip("Cha chứa bonus spawn ra. Để trống → spawn dưới chính spawner.")]
    public Transform spawnParent;

    [Header("Refs (để trống = tự tìm)")]
    public RacePositionTracker tracker;
    public RaceSettings raceSettings;

    [Tooltip("Dùng khi không có raceSettings (số bonus / lap dự phòng).")]
    [Min(0)] public int fallbackBonusesPerLap = 3;

    [Header("Đặt vị trí quanh checkpoint")]
    [Tooltip("Nâng bonus lên khỏi checkpoint (m).")]
    public float spawnHeight = 1.5f;

    [Tooltip("Lệch ngẫu nhiên trên mặt phẳng quanh checkpoint (m). 0 = ngay tại checkpoint.")]
    public float spawnRadius = 3f;

    [Tooltip("Xoá bonus chưa ăn của lap trước khi spawn lap mới.")]
    public bool clearPreviousLapBonuses = true;

    [Header("Debug")]
    public bool showDebugLog = true;

    private readonly List<Transform> checkpointAnchors = new List<Transform>();
    private readonly List<GameObject> spawned = new List<GameObject>();
    private bool subscribed;

    void Start()
    {
        ResolveRefs();
        DiscoverCheckpoints();
        SubscribeTracker();

        // Batch cho Lap 1 ngay khi vào race.
        SpawnBatch(1);
    }

    void OnDestroy() => UnsubscribeTracker();

    // ---------------------------------------------------------------- refs

    private void ResolveRefs()
    {
        if (tracker == null)
            tracker = FindObjectOfType<RacePositionTracker>();

        if (raceSettings == null && tracker != null)
            raceSettings = tracker.raceSettings;

        // Chỉ cần fallback template khi KHÔNG có prefab.
        if (bonusPrefab == null && bonusTemplate == null)
        {
            // Ưu tiên BonusPlace > con đầu; nếu không có thì con đầu của chính spawner.
            var bonusPlace = GameObject.Find("BonusPlace");
            if (bonusPlace != null && bonusPlace.transform.childCount > 0)
                bonusTemplate = bonusPlace.transform.GetChild(0);
            else if (transform.childCount > 0)
                bonusTemplate = transform.GetChild(0);
        }

        if (spawnParent == null)
            spawnParent = transform;

        if (showDebugLog)
            Debug.Log($"[LapBonusSpawner] source={(bonusPrefab != null ? "prefab:" + bonusPrefab.name : (bonusTemplate != null ? "template:" + bonusTemplate.name : "NULL"))}, " +
                      $"tracker={(tracker != null)}, raceSettings={(raceSettings != null)}");
    }

    private void DiscoverCheckpoints()
    {
        checkpointAnchors.Clear();

        // 1) Dùng list checkpoint tracker đã nạp (nếu sẵn).
        if (tracker != null && tracker.checkpoints != null && tracker.checkpoints.Count > 0)
        {
            foreach (var cp in tracker.checkpoints)
                if (cp != null) checkpointAnchors.Add(cp);
        }

        // 2) Fallback: object tag "CheckPoint" → con trực tiếp (giống RacePositionTracker).
        if (checkpointAnchors.Count == 0)
        {
            GameObject cpParent = null;
            try { cpParent = GameObject.FindGameObjectWithTag("CheckPoint"); }
            catch (UnityException) { /* tag chưa khai báo */ }

            if (cpParent == null) cpParent = GameObject.Find("Checkpoint_Manager");

            if (cpParent != null)
                foreach (Transform child in cpParent.transform)
                    checkpointAnchors.Add(child);
        }

        if (showDebugLog)
            Debug.Log($"[LapBonusSpawner] Tìm thấy {checkpointAnchors.Count} checkpoint làm điểm spawn.");
    }

    private void SubscribeTracker()
    {
        if (subscribed || tracker == null) return;
        tracker.onLapCompleted.AddListener(HandleLapCompleted);
        subscribed = true;
    }

    private void UnsubscribeTracker()
    {
        if (!subscribed || tracker == null) return;
        tracker.onLapCompleted.RemoveListener(HandleLapCompleted);
        subscribed = false;
    }

    // ---------------------------------------------------------------- lap event

    private void HandleLapCompleted(string racerName, int lapCount)
    {
        // Chỉ player mới sinh bonus (AI hoàn thành lap không spam).
        if (racerName != "Player") return;

        int totalLaps = raceSettings != null ? Mathf.Max(1, raceSettings.totalLaps)
                                              : (tracker != null ? tracker.totalLaps : 1);

        int nextLap = lapCount + 1;
        // Hoàn thành lap cuối = về đích → không cần bonus cho "lap" không tồn tại.
        if (nextLap > totalLaps) return;

        SpawnBatch(nextLap);
    }

    // ---------------------------------------------------------------- spawn

    private int GetBonusesPerLap()
    {
        return raceSettings != null ? Mathf.Max(0, raceSettings.bonusesPerLap)
                                    : Mathf.Max(0, fallbackBonusesPerLap);
    }

    /// <summary>Nguồn để nhân bản: ưu tiên prefab, fallback template trong scene.</summary>
    private GameObject SpawnSource => bonusPrefab != null ? bonusPrefab
                                    : (bonusTemplate != null ? bonusTemplate.gameObject : null);

    /// <summary>Spawn 1 batch bonus cho lap chỉ định.</summary>
    public void SpawnBatch(int lapNumber)
    {
        if (SpawnSource == null)
        {
            if (showDebugLog) Debug.LogWarning("[LapBonusSpawner] Chưa có bonusPrefab/bonusTemplate — bỏ qua spawn.");
            return;
        }

        if (checkpointAnchors.Count == 0)
        {
            DiscoverCheckpoints();
            if (checkpointAnchors.Count == 0)
            {
                if (showDebugLog) Debug.LogWarning("[LapBonusSpawner] Không có checkpoint nào để spawn bonus.");
                return;
            }
        }

        int count = GetBonusesPerLap();
        if (count <= 0) return;

        if (clearPreviousLapBonuses)
            ClearSpawned();

        var indices = PickCheckpointIndices(count, checkpointAnchors.Count);
        int created = 0;
        var source = SpawnSource;

        foreach (int idx in indices)
        {
            var cp = checkpointAnchors[idx];
            if (cp == null) continue;

            Vector3 offset = Vector3.up * spawnHeight;
            if (spawnRadius > 0f)
            {
                Vector2 r = Random.insideUnitCircle * spawnRadius;
                offset += new Vector3(r.x, 0f, r.y);
            }

            // Hướng Z = nhìn về checkpoint kế tiếp theo thứ tự vòng đua
            // (vd spawn ở Checkpoint_4 → Z hướng tới Checkpoint_5). Vòng lại đầu ở checkpoint cuối.
            Quaternion rot = GetForwardToNextCheckpoint(idx);

            GameObject clone = Instantiate(source, cp.position + offset, rot, spawnParent);
            clone.name = $"{source.name}_Lap{lapNumber}_{created}";
            WireBonusEvent(clone); // nối lại BonusEvent (prefab không giữ được ref singleton scene)
            clone.SetActive(true); // phòng trường hợp nguồn là template inactive
            spawned.Add(clone);
            created++;
        }

        if (showDebugLog)
            Debug.Log($"[LapBonusSpawner] Lap {lapNumber}: spawn {created} bonus gần {checkpointAnchors.Count} checkpoint.");
    }

    /// <summary>
    /// Nối lại sự kiện tăng tốc cho clone. Trong PREFAB, call thứ 3 của
    /// <c>PlayerTriggerEvent.onPlayerEnter</c> (<c>BonusEvent.TriggerBonusUnityEvent</c>) có
    /// <c>m_Target = null</c> vì prefab asset không thể tham chiếu singleton BonusEvent nằm trong scene.
    /// → tự AddListener runtime trỏ tới <c>BonusEvent.Instance</c> để ăn bonus vẫn tăng tốc.
    /// (Hai call còn lại — Effect.SetActive / item.SetActive — trỏ object NỘI BỘ clone nên Instantiate
    /// tự remap, không cần đụng tới.)
    /// </summary>
    private void WireBonusEvent(GameObject clone)
    {
        var trigger = clone.GetComponentInChildren<PlayerTriggerEvent>(true);
        if (trigger == null)
        {
            if (showDebugLog) Debug.LogWarning($"[LapBonusSpawner] Clone '{clone.name}' không có PlayerTriggerEvent.");
            return;
        }

        if (trigger.onPlayerEnter == null)
            trigger.onPlayerEnter = new UnityEngine.Events.UnityEvent();

        var bonusEvent = BonusEvent.Instance;
        trigger.onPlayerEnter.AddListener(bonusEvent.TriggerBonusUnityEvent);
    }

    /// <summary>Xoá mọi bonus đã spawn còn lại (chưa bị ăn).</summary>
    public void ClearSpawned()
    {
        foreach (var go in spawned)
            if (go != null) Destroy(go);
        spawned.Clear();
    }

    /// <summary>
    /// Quaternion để bonus tại checkpoint <paramref name="idx"/> hướng trục Z (forward) về
    /// checkpoint kế tiếp theo thứ tự vòng đua (idx+1, vòng lại 0 ở checkpoint cuối).
    /// Nếu 2 checkpoint trùng vị trí (hướng ~0) → giữ rotation của checkpoint hiện tại.
    /// </summary>
    private Quaternion GetForwardToNextCheckpoint(int idx)
    {
        var cur = checkpointAnchors[idx];
        int nextIdx = (idx + 1) % checkpointAnchors.Count;
        var next = checkpointAnchors[nextIdx];

        if (cur != null && next != null && next != cur)
        {
            Vector3 dir = next.position - cur.position;
            dir.y = 0f; // giữ bonus đứng thẳng, chỉ xoay quanh trục Y
            if (dir.sqrMagnitude > 0.0001f)
                return Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        return cur != null ? cur.rotation : Quaternion.identity;
    }

    /// <summary>
    /// Chọn <paramref name="count"/> index checkpoint. Không trùng nếu đủ checkpoint;
    /// nếu cần nhiều hơn số checkpoint thì lặp lại (vẫn rải đều).
    /// </summary>
    private List<int> PickCheckpointIndices(int count, int total)
    {
        var result = new List<int>(count);
        if (total <= 0) return result;

        // Tạo danh sách index rồi xáo trộn (Fisher–Yates).
        var pool = new List<int>(total);
        for (int i = 0; i < total; i++) pool.Add(i);

        while (result.Count < count)
        {
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            for (int i = 0; i < pool.Count && result.Count < count; i++)
                result.Add(pool[i]);
        }

        return result;
    }
}
