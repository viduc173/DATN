using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;

public class RacePositionTracker : MonoBehaviour
{
    [System.Serializable]
    public class RacerData
    {
        public Transform racerTransform;
        public string racerName;
        [HideInInspector] public int currentCheckpointIndex = 0;
        [HideInInspector] public int lastCheckpointIndex = -1; // -1 = chưa đo lần nào
        [HideInInspector] public int maxCheckpointInLap = 0;   // CP cao nhất đã chạm trong lap hiện tại — chống false-positive khi spawn gần finish line
        [HideInInspector] public bool spawnedBehindLine = false; // spawn ở vùng cuối track (phía sau vạch xuất phát)
        [HideInInspector] public bool passedStartLine = false;   // đã băng qua vạch xuất phát lần đầu (= bắt đầu Lap 1) chưa
        [HideInInspector] public int lapCount = 0;
        [HideInInspector] public float distanceToNextCheckpoint;
        [HideInInspector] public int currentPosition;
        [HideInInspector] public float totalProgress;
        [HideInInspector] public bool hasFinished = false;
        [HideInInspector] public int finishPosition = 0; // Vị trí khi về đích (1, 2, 3...)
    }

    [Header("Race Setup")]
    [Tooltip("Transform của Player")]
    public Transform playerTransform;

    [Tooltip("List các AI racers")]
    public List<Transform> aiRacers = new List<Transform>();

    [Tooltip("List các checkpoint theo thứ tự")]
    public List<Transform> checkpoints = new List<Transform>();

    [Tooltip("Số vòng đua tối đa (bị OVERRIDE bởi raceSettings nếu raceSettings được gán)")]
    public int totalLaps = 3;

    [Header("Race Settings (file cấu hình)")]
    [Tooltip("ScriptableObject cấu hình race. Nếu gán, sẽ override totalLaps và xử lý load scene khi kết thúc race.")]
    public RaceSettings raceSettings;

    [Header("Settings")]
    [Tooltip("Tự động tìm checkpoint từ children")]
    public bool autoDetectCheckpoints = true;
    
    [Tooltip("Cập nhật vị trí mỗi X giây (0 = mỗi frame)")]
    public float updateInterval = 0.1f;

    [Header("UI TextMeshPro")]
    [Tooltip("Text hiển thị vị trí (ví dụ: '1st')")]
    public TextMeshProUGUI positionText;
    
    [Tooltip("Text hiển thị vị trí đầy đủ (ví dụ: '2nd / 3')")]
    public TextMeshProUGUI positionFullText;
    
    [Tooltip("Text hiển thị checkpoint (ví dụ: 'Checkpoint 3/8')")]
    public TextMeshProUGUI checkpointText;
    
    [Tooltip("Text hiển thị số vòng (ví dụ: 'Lap 2/3')")]
    public TextMeshProUGUI lapText;
    
    [Tooltip("Text hiển thị bảng xếp hạng đầy đủ")]
    public TextMeshProUGUI leaderboardText;

    [Header("Top 6 Leaderboard UI")]
    [Tooltip("Text hiển thị vị trí 1")]
    public TextMeshProUGUI position1Text;
    
    [Tooltip("Text hiển thị vị trí 2")]
    public TextMeshProUGUI position2Text;
    
    [Tooltip("Text hiển thị vị trí 3")]
    public TextMeshProUGUI position3Text;
    
    [Tooltip("Text hiển thị vị trí 4")]
    public TextMeshProUGUI position4Text;
    
    [Tooltip("Text hiển thị vị trí 5")]
    public TextMeshProUGUI position5Text;
    
    [Tooltip("Text hiển thị vị trí 6")]
    public TextMeshProUGUI position6Text;

    [Tooltip("Text hiển thị vị trí của player")]
    public TextMeshProUGUI playerPositionText;

    [Header("Events")]
    [Tooltip("Event khi vị trí player thay đổi (int: vị trí mới)")]
    public UnityEvent<int> onPlayerPositionChanged;

    [Tooltip("Event cập nhật vị trí (int: vị trí, int: tổng số xe)")]
    public UnityEvent<int, int> onPositionUpdate;

    [Tooltip("Event khi 1 racer hoàn thành 1 vòng (string: tên racer, int: lapCount mới)")]
    public UnityEvent<string, int> onLapCompleted;

    [Tooltip("Event khi PLAYER về đích")]
    public UnityEvent onPlayerRaceFinished;

    [Header("Debug")]
    [Tooltip("Hiển thị debug info")]
    public bool showDebugInfo = true;

    private List<RacerData> allRacers = new List<RacerData>();
    private int lastPlayerPosition = -1;
    private float updateTimer = 0f;
    private int nextFinishPosition = 1; // Vị trí tiếp theo cho xe về đích
    private bool playerSceneLoadScheduled = false;

    void Start()
    {
        AutoDiscoverReferences();
        ApplyRaceSettings();
        InitializeRacers();

        if (autoDetectCheckpoints && checkpoints.Count == 0)
        {
            DetectCheckpoints();
        }
    }

    /// <summary>
    /// Tự động gán playerTransform (qua tag "Player") và tất cả UI text (qua RaceUILabel marker).
    /// Chỉ gán khi field đang null — Inspector assignment luôn được ưu tiên.
    /// </summary>
    private void AutoDiscoverReferences()
    {
        if (playerTransform == null)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                playerTransform = playerGO.transform;
                if (showDebugInfo)
                    Debug.Log($"[RacePositionTracker] Auto-found playerTransform: {playerGO.name}");
            }
            else
            {
                Debug.LogWarning("[RacePositionTracker] Không tìm thấy GameObject với tag 'Player'.");
            }
        }

        foreach (var label in FindObjectsOfType<RaceUILabel>())
        {
            switch (label.role)
            {
                case RaceUIRole.Position1:     if (position1Text     == null) position1Text     = label.TMP; break;
                case RaceUIRole.Position2:     if (position2Text     == null) position2Text     = label.TMP; break;
                case RaceUIRole.Position3:     if (position3Text     == null) position3Text     = label.TMP; break;
                case RaceUIRole.Position4:     if (position4Text     == null) position4Text     = label.TMP; break;
                case RaceUIRole.Position5:     if (position5Text     == null) position5Text     = label.TMP; break;
                case RaceUIRole.Position6:     if (position6Text     == null) position6Text     = label.TMP; break;
                case RaceUIRole.PlayerPosition: if (playerPositionText == null) playerPositionText = label.TMP; break;
            }
        }

        if (showDebugInfo)
            Debug.Log($"[RacePositionTracker] AutoDiscover done — playerTransform={(playerTransform != null ? playerTransform.name : "NULL")}, " +
                      $"p1={position1Text != null}, p2={position2Text != null}, p3={position3Text != null}, you={playerPositionText != null}");
    }

    /// <summary>
    /// Áp dụng cấu hình từ RaceSettings ScriptableObject (nếu có).
    /// </summary>
    private void ApplyRaceSettings()
    {
        if (raceSettings == null) return;

        totalLaps = Mathf.Max(1, raceSettings.totalLaps);

        if (showDebugInfo)
            Debug.Log($"[RacePositionTracker] Đã áp dụng RaceSettings: totalLaps={totalLaps}, endScene='{raceSettings.endSceneName}'");
    }

    /// <summary>
    /// Tính ngưỡng wrap để detect 1 vòng từ vị trí "gần checkpoint cuối" sang "gần checkpoint đầu".
    /// </summary>
    private int GetLapWrapThreshold()
    {
        if (raceSettings != null && raceSettings.lapWrapThreshold > 0)
            return raceSettings.lapWrapThreshold;
        return Mathf.Max(1, checkpoints.Count / 4);
    }

    void Update()
    {
        // Cập nhật theo interval
        updateTimer += Time.deltaTime;
        if (updateInterval > 0 && updateTimer < updateInterval)
            return;
        
        updateTimer = 0f;

        UpdateAllPositions();
        CalculateRankings();
        UpdateUI();
    }

    /// <summary>
    /// Tự động cập nhật tất cả UI text
    /// </summary>
    private void UpdateUI()
    {
        if (positionText != null)
        {
            positionText.text = GetPlayerPositionText();
        }

        if (positionFullText != null)
        {
            positionFullText.text = GetPlayerPositionFullText();
        }

        if (checkpointText != null)
        {
            checkpointText.text = GetPlayerCheckpointText();
        }

        if (lapText != null)
        {
            lapText.text = GetPlayerLapText();
        }

        if (leaderboardText != null)
        {
            leaderboardText.text = GetFullLeaderboardText();
        }

        // Cập nhật top 6 positions
        UpdateTop6PositionsUI();

        if (playerPositionText != null)
        {
            var playerData = allRacers.FirstOrDefault(r => r.racerName == "Player");
            if (playerData != null)
            {
                string displayText = $"{playerData.currentPosition}. {playerData.racerName} | CP: {playerData.currentCheckpointIndex + 1}/{checkpoints.Count} | Lap: {GetDisplayLap(playerData)}/{totalLaps}";
                playerPositionText.text = displayText;
                playerPositionText.color = Color.yellow;
            }
            else
            {
                playerPositionText.text = "--";
                playerPositionText.color = Color.white;
            }
        }
    }

    /// <summary>
    /// Cập nhật UI hiển thị top 6 vị trí
    /// </summary>
    private void UpdateTop6PositionsUI()
    {
        var ranked = GetRankedRacers();
        var top6TextFields = new TextMeshProUGUI[] 
        { 
            position1Text, position2Text, position3Text, 
            position4Text, position5Text, position6Text 
        };

        for (int i = 0; i < top6TextFields.Length; i++)
        {
            if (top6TextFields[i] != null)
            {
                if (i < ranked.Count && ranked[i].racerTransform != null)
                {
                    var racer = ranked[i];
                    string finishStatus = racer.hasFinished ? " ✓" : "";
                    string displayText = $"{racer.currentPosition}. {racer.racerName}{finishStatus} | CP: {racer.currentCheckpointIndex + 1}/{checkpoints.Count} | Lap: {GetDisplayLap(racer)}/{totalLaps}";
                    
                    top6TextFields[i].text = displayText;
                    
                    // Xe đã về đích: màu xanh lá
                    // Player: màu vàng
                    // Xe khác: màu trắng
                    if (racer.hasFinished)
                    {
                        top6TextFields[i].color = racer.racerName == "Player" ? Color.green : new Color(0.5f, 1f, 0.5f);
                    }
                    else if (racer.racerName == "Player")
                    {
                        top6TextFields[i].color = Color.yellow;
                    }
                    else
                    {
                        top6TextFields[i].color = Color.white;
                    }
                }
                else
                {
                    // Nếu không đủ xe thì hiển thị trống
                    top6TextFields[i].text = $"{FormatPosition(i + 1)} ---";
                    top6TextFields[i].color = Color.gray;
                }
            }
        }
    }

    /// <summary>
    /// Lấy text cho top 6 racers (có thể gọi từ bên ngoài)
    /// </summary>
    public string[] GetTop6RacersText()
    {
        var ranked = GetRankedRacers();
        string[] results = new string[6];
        
        for (int i = 0; i < 6; i++)
        {
            if (i < ranked.Count && ranked[i].racerTransform != null)
            {
                var racer = ranked[i];
                results[i] = $"{FormatPosition(racer.currentPosition)} {racer.racerName}";
            }
            else
            {
                results[i] = $"{FormatPosition(i + 1)} ---";
            }
        }
        
        return results;
    }

    /// <summary>
    /// Gán player transform theo xe mà LoadSceneController đã kích hoạt.
    /// An toàn gọi trước hoặc sau Start(): nếu allRacers đã khởi tạo thì cập nhật entry "Player",
    /// nếu chưa thì chỉ set field để InitializeRacers() dùng. Reset lại tiến độ đua.
    /// </summary>
    public void SetPlayer(Transform player)
    {
        playerTransform = player;

        var existing = allRacers.FirstOrDefault(r => r.racerName == "Player");
        if (existing != null)
            existing.racerTransform = player;
        else if (player != null)
            allRacers.Add(new RacerData { racerTransform = player, racerName = "Player" });

        ResetRace();

        if (showDebugInfo)
            Debug.Log($"[RacePositionTracker] SetPlayer → {(player != null ? player.name : "NULL")}");
    }

    /// <summary>
    /// Đăng ký AI racer động — gọi từ Race_Manager sau khi spawn tại runtime.
    /// An toàn để gọi trước hoặc sau Start().
    /// </summary>
    public void RegisterAIRacer(Transform racerTransform, string racerName = null)
    {
        if (racerTransform == null) return;
        if (allRacers.Any(r => r.racerTransform == racerTransform)) return;

        string name = string.IsNullOrEmpty(racerName) ? $"AI {aiRacers.Count + 1}" : racerName;
        allRacers.Add(new RacerData { racerTransform = racerTransform, racerName = name });

        if (!aiRacers.Contains(racerTransform))
            aiRacers.Add(racerTransform);

        if (showDebugInfo)
            Debug.Log($"[RacePositionTracker] Đã đăng ký: {name}");
    }

    /// <summary>
    /// Khởi tạo danh sách racers
    /// </summary>
    private void InitializeRacers()
    {
        allRacers.Clear();

        // Thêm player
        if (playerTransform != null)
        {
            allRacers.Add(new RacerData
            {
                racerTransform = playerTransform,
                racerName = "Player"
            });
        }

        // Thêm AI (bỏ qua nếu đã đăng ký qua RegisterAIRacer trước Start)
        for (int i = 0; i < aiRacers.Count; i++)
        {
            if (aiRacers[i] != null && !allRacers.Any(r => r.racerTransform == aiRacers[i]))
            {
                allRacers.Add(new RacerData
                {
                    racerTransform = aiRacers[i],
                    racerName = $"AI {i + 1}"
                });
            }
        }

        if (showDebugInfo)
            Debug.Log($"Initialized {allRacers.Count} racers");
    }

    /// <summary>
    /// Tự động tìm checkpoints từ children
    /// </summary>
    private void DetectCheckpoints()
    {
        checkpoints.Clear();

        // Ưu tiên: GameObject có tag "CheckPoint" làm cha → nạp TẤT CẢ con trực tiếp theo đúng thứ tự hierarchy
        var cpParent = FindByTagSafe("CheckPoint");
        if (cpParent != null)
        {
            foreach (Transform child in cpParent.transform)
                checkpoints.Add(child);
            if (showDebugInfo)
                Debug.Log($"[RacePositionTracker] Nạp {checkpoints.Count} checkpoint từ object tag 'CheckPoint' ('{cpParent.name}')");
            return;
        }

        // Fallback 1: tìm theo tên "Checkpoint_Manager"
        var cpManager = GameObject.Find("Checkpoint_Manager");
        if (cpManager != null)
        {
            foreach (Transform child in cpManager.transform)
                checkpoints.Add(child);
            if (showDebugInfo)
                Debug.Log($"[RacePositionTracker] Nạp {checkpoints.Count} checkpoint từ Checkpoint_Manager (fallback theo tên)");
            return;
        }

        // Fallback 2: con của chính tracker
        foreach (Transform child in transform)
            checkpoints.Add(child);
        if (showDebugInfo)
            Debug.Log($"[RacePositionTracker] Nạp {checkpoints.Count} checkpoint (fallback: con của tracker)");
    }

    /// <summary>
    /// FindGameObjectWithTag an toàn — không ném exception nếu tag chưa khai báo trong TagManager.
    /// </summary>
    private GameObject FindByTagSafe(string tag)
    {
        try
        {
            return GameObject.FindGameObjectWithTag(tag);
        }
        catch (UnityException)
        {
            if (showDebugInfo)
                Debug.LogWarning($"[RacePositionTracker] Tag '{tag}' chưa khai báo trong TagManager — bỏ qua bước tìm theo tag.");
            return null;
        }
    }

    /// <summary>
    /// Cập nhật vị trí của tất cả racers + detect lap completion + detect finish.
    /// Lap được tăng khi closestCheckpointIndex "wrap" từ vùng cuối track về vùng đầu.
    /// </summary>
    private void UpdateAllPositions()
    {
        if (checkpoints.Count == 0) return;

        int n = checkpoints.Count;
        int wrapThreshold = GetLapWrapThreshold();

        foreach (var racer in allRacers)
        {
            if (racer.racerTransform == null) continue;

            // Nếu xe đã về đích, không cần cập nhật nữa
            if (racer.hasFinished) continue;

            // Tìm checkpoint gần nhất
            int closestCheckpointIndex = FindClosestCheckpoint(racer.racerTransform.position);

            // --- LAP DETECTION ---
            // Track CP cao nhất đã chạm trong lap hiện tại
            racer.maxCheckpointInLap = Mathf.Max(racer.maxCheckpointInLap, closestCheckpointIndex);

            // Yêu cầu phải chạm tới ít nhất CP giữa track (n/2) trước khi count 1 lap
            int midwayRequirement = n / 2;

            // Lần đo đầu tiên: chỉ set last, không count lap (tránh false positive khi spawn gần CP0)
            if (racer.lastCheckpointIndex < 0)
            {
                racer.lastCheckpointIndex = closestCheckpointIndex;
                // Spawn ở vùng cuối track (sau vạch xuất phát) → lần băng vạch ĐẦU TIÊN là xuất phát, không phải hoàn thành vòng
                racer.spawnedBehindLine = closestCheckpointIndex >= n - wrapThreshold;
            }
            else if (closestCheckpointIndex != racer.lastCheckpointIndex)
            {
                bool forwardWrap = racer.lastCheckpointIndex >= n - wrapThreshold
                                   && closestCheckpointIndex < wrapThreshold;
                bool backwardWrap = racer.lastCheckpointIndex < wrapThreshold
                                    && closestCheckpointIndex >= n - wrapThreshold;

                if (forwardWrap && racer.maxCheckpointInLap >= midwayRequirement)
                {
                    // Lần băng vạch đầu tiên của xe spawn-sau-vạch = thời điểm XUẤT PHÁT (bắt đầu Lap 1), KHÔNG count lap
                    if (racer.spawnedBehindLine && !racer.passedStartLine)
                    {
                        racer.passedStartLine = true;
                        racer.maxCheckpointInLap = 0; // bắt đầu đếm tiến độ cho Lap 1 thực sự

                        if (showDebugInfo)
                            Debug.Log($"[Race] {racer.racerName} băng qua vạch xuất phát — bắt đầu Lap 1");
                    }
                    else
                    {
                        racer.lapCount++;
                        racer.maxCheckpointInLap = 0; // reset cho lap mới
                        onLapCompleted?.Invoke(racer.racerName, racer.lapCount);

                        if (showDebugInfo)
                            Debug.Log($"[Race] {racer.racerName} hoàn thành lap {racer.lapCount}/{totalLaps}");

                        // FINISH: đủ số vòng
                        if (racer.lapCount >= totalLaps)
                        {
                            MarkRacerFinished(racer);
                            continue; // skip phần progress update vì xe đã finished
                        }
                    }
                }
                else if (forwardWrap && showDebugInfo)
                {
                    Debug.Log($"[Race] {racer.racerName} qua finish line nhưng chưa chạm midway (max={racer.maxCheckpointInLap}/{midwayRequirement}) — không count lap");
                }
                else if (backwardWrap)
                {
                    // Chạy ngược qua start/finish line → giảm lap (chống ăn gian)
                    if (racer.lapCount > 0)
                        racer.lapCount = racer.lapCount - 1;
                    if (showDebugInfo)
                        Debug.Log($"[Race] {racer.racerName} đi ngược qua finish line, lap giảm còn {racer.lapCount}");
                }

                racer.lastCheckpointIndex = closestCheckpointIndex;
            }

            // Cập nhật checkpoint hiện tại
            racer.currentCheckpointIndex = closestCheckpointIndex;

            // Tính khoảng cách đến checkpoint tiếp theo
            int nextCheckpointIndex = (closestCheckpointIndex + 1) % n;
            racer.distanceToNextCheckpoint = Vector3.Distance(
                racer.racerTransform.position,
                checkpoints[nextCheckpointIndex].position
            );

            // Chuẩn hóa theo độ dài segment thực tế (thay vì hằng số 100f)
            float segmentLength = Vector3.Distance(
                checkpoints[closestCheckpointIndex].position,
                checkpoints[nextCheckpointIndex].position
            );
            float segmentProgress = segmentLength > 0.01f
                ? Mathf.Clamp01(1f - (racer.distanceToNextCheckpoint / segmentLength))
                : 0f;

            // totalProgress dùng cho ranking
            racer.totalProgress = (racer.lapCount * n) + racer.currentCheckpointIndex + segmentProgress;
        }
    }

    /// <summary>
    /// Đánh dấu 1 racer đã về đích + xử lý load scene nếu là Player.
    /// </summary>
    private void MarkRacerFinished(RacerData racer)
    {
        racer.hasFinished = true;
        racer.finishPosition = nextFinishPosition;
        nextFinishPosition++;

        if (showDebugInfo)
            Debug.Log($"[Race] {racer.racerName} đã VỀ ĐÍCH! Vị trí cuối: {racer.finishPosition}");

        // Nếu là player → trigger event + load scene
        if (racer.racerName == "Player")
        {
            onPlayerRaceFinished?.Invoke();

            if (raceSettings != null
                && raceSettings.autoLoadSceneOnFinish
                && !string.IsNullOrEmpty(raceSettings.endSceneName)
                && !playerSceneLoadScheduled)
            {
                playerSceneLoadScheduled = true;
                StartCoroutine(LoadEndSceneAfterDelay(raceSettings.endSceneName, raceSettings.loadSceneDelay));
            }
        }
    }

    private IEnumerator LoadEndSceneAfterDelay(string sceneName, float delay)
    {
        if (showDebugInfo)
            Debug.Log($"[Race] Sẽ load scene '{sceneName}' sau {delay}s");

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Tìm checkpoint gần nhất với vị trí cho trước
    /// </summary>
    private int FindClosestCheckpoint(Vector3 position)
    {
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < checkpoints.Count; i++)
        {
            float distance = Vector3.Distance(position, checkpoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    /// <summary>
    /// Tính toán và xếp hạng các racers
    /// </summary>
    private void CalculateRankings()
    {
        // Tách xe đã về đích và chưa về đích
        var finishedRacers = allRacers.Where(r => r.hasFinished)
                                       .OrderBy(r => r.finishPosition)
                                       .ToList();
        
        var activeRacers = allRacers.Where(r => !r.hasFinished)
                                     .OrderByDescending(r => r.totalProgress)
                                     .ToList();

        // Gán vị trí cho xe đã về đích (giữ nguyên finishPosition)
        foreach (var racer in finishedRacers)
        {
            racer.currentPosition = racer.finishPosition;
        }

        // Gán vị trí cho xe chưa về đích (bắt đầu từ sau xe cuối cùng về đích)
        int nextPosition = finishedRacers.Count + 1;
        foreach (var racer in activeRacers)
        {
            racer.currentPosition = nextPosition;
            nextPosition++;
        }

        // Kiểm tra vị trí của player
        var playerData = allRacers.FirstOrDefault(r => r.racerName == "Player");
        if (playerData != null)
        {
            if (lastPlayerPosition != playerData.currentPosition)
            {
                lastPlayerPosition = playerData.currentPosition;
                onPlayerPositionChanged?.Invoke(playerData.currentPosition);
                
                if (showDebugInfo)
                    Debug.Log($"Player position: {playerData.currentPosition}/{allRacers.Count}");
            }

            // Gọi event cập nhật
            onPositionUpdate?.Invoke(playerData.currentPosition, allRacers.Count);
        }
    }

    /// <summary>
    /// Gọi khi xe chạm 1 checkpoint trigger (opt-in, dùng kèm CheckpointTrigger.cs).
    /// Phát hiện lap qua trigger thay vì qua distance — chính xác hơn nhưng cần collider trên mỗi checkpoint.
    /// </summary>
    public void OnCheckpointPassed(Transform racerTransform, int checkpointIndex)
    {
        var racer = allRacers.FirstOrDefault(r => r.racerTransform == racerTransform);
        if (racer == null || racer.hasFinished) return;
        if (checkpoints.Count == 0) return;

        int previousIndex = racer.currentCheckpointIndex;
        racer.currentCheckpointIndex = checkpointIndex;

        int n = checkpoints.Count;

        racer.maxCheckpointInLap = Mathf.Max(racer.maxCheckpointInLap, checkpointIndex);

        // Phát hiện hoàn thành vòng: vừa đi qua CP cuối rồi qua CP đầu (theo đúng thứ tự)
        if (previousIndex == n - 1 && checkpointIndex == 0 && racer.maxCheckpointInLap >= n / 2)
        {
            // Lần băng vạch đầu tiên = XUẤT PHÁT (bắt đầu Lap 1), không count
            if (!racer.passedStartLine)
            {
                racer.passedStartLine = true;
                racer.maxCheckpointInLap = 0;

                if (showDebugInfo)
                    Debug.Log($"[Race/Trigger] {racer.racerName} băng qua vạch xuất phát — bắt đầu Lap 1");
            }
            else
            {
                racer.lapCount++;
                racer.maxCheckpointInLap = 0;
                onLapCompleted?.Invoke(racer.racerName, racer.lapCount);

                if (showDebugInfo)
                    Debug.Log($"[Race/Trigger] {racer.racerName} hoàn thành lap {racer.lapCount}/{totalLaps}");

                if (racer.lapCount >= totalLaps)
                {
                    MarkRacerFinished(racer);
                }
            }
        }

        // Sync với hệ distance-based để không bị conflict
        racer.lastCheckpointIndex = checkpointIndex;
    }

    /// <summary>
    /// Lấy vị trí hiện tại của player
    /// </summary>
    public int GetPlayerPosition()
    {
        var playerData = allRacers.FirstOrDefault(r => r.racerName == "Player");
        return playerData?.currentPosition ?? -1;
    }

    /// <summary>
    /// Lấy text vị trí player (ví dụ: "1st", "2nd", "3rd")
    /// </summary>
    public string GetPlayerPositionText()
    {
        int position = GetPlayerPosition();
        if (position <= 0) return "--";
        return FormatPosition(position);
    }

    /// <summary>
    /// Lấy text vị trí đầy đủ (ví dụ: "2nd / 3")
    /// </summary>
    public string GetPlayerPositionFullText()
    {
        int position = GetPlayerPosition();
        if (position <= 0) return "-- / --";
        return $"{FormatPosition(position)} / {allRacers.Count}";
    }

    /// <summary>
    /// Lấy text checkpoint hiện tại của player (ví dụ: "Checkpoint 3/8")
    /// </summary>
    public string GetPlayerCheckpointText()
    {
        var playerData = allRacers.FirstOrDefault(r => r.racerName == "Player");
        if (playerData == null || checkpoints.Count == 0) return "--";
        return $"Checkpoint {playerData.currentCheckpointIndex + 1}/{checkpoints.Count}";
    }

    /// <summary>
    /// Lấy text số vòng của player (ví dụ: "Lap 2/3")
    /// </summary>
    public string GetPlayerLapText()
    {
        var playerData = allRacers.FirstOrDefault(r => r.racerName == "Player");
        if (playerData == null) return "--";
        return $"Lap {GetDisplayLap(playerData)}/{totalLaps}";
    }

    /// <summary>
    /// Lấy text bảng xếp hạng đầy đủ (nhiều dòng)
    /// </summary>
    public string GetFullLeaderboardText()
    {
        var ranked = GetRankedRacers();
        string text = "";
        
        foreach (var racer in ranked)
        {
            if (racer.racerTransform == null) continue;
            
            text += $"{racer.currentPosition}. {racer.racerName}";
            text += $" | CP: {racer.currentCheckpointIndex + 1}/{checkpoints.Count}";
            text += $" | Lap: {GetDisplayLap(racer)}/{totalLaps}\n";
        }
        
        return text;
    }

    /// <summary>
    /// Lấy text bảng xếp hạng chi tiết (nhiều dòng với checkpoint và lap)
    /// </summary>
    public string GetDetailedLeaderboardText()
    {
        var ranked = GetRankedRacers();
        string text = "=== STANDINGS ===\n";
        
        foreach (var racer in ranked)
        {
            if (racer.racerTransform == null) continue;
            text += $"{FormatPosition(racer.currentPosition)} {racer.racerName} ";
            text += $"(CP {racer.currentCheckpointIndex + 1}, Lap {GetDisplayLap(racer)})\n";
        }
        
        return text;
    }

    /// <summary>
    /// Format vị trí với suffix (1st, 2nd, 3rd, 4th...)
    /// </summary>
    private string FormatPosition(int position)
    {
        if (position <= 0) return "--";
        
        string suffix;
        int lastDigit = position % 10;
        int lastTwoDigits = position % 100;
        
        // Xử lý 11th, 12th, 13th đặc biệt
        if (lastTwoDigits >= 11 && lastTwoDigits <= 13)
        {
            suffix = "th";
        }
        else
        {
            switch (lastDigit)
            {
                case 1: suffix = "st"; break;
                case 2: suffix = "nd"; break;
                case 3: suffix = "rd"; break;
                default: suffix = "th"; break;
            }
        }
        
        return $"{position}{suffix}";
    }

    /// <summary>
    /// Lấy thông tin vị trí của một racer cụ thể
    /// </summary>
    public RacerData GetRacerData(Transform racerTransform)
    {
        return allRacers.FirstOrDefault(r => r.racerTransform == racerTransform);
    }

    /// <summary>
    /// Lấy tất cả racers đã xếp hạng
    /// </summary>
    public List<RacerData> GetRankedRacers()
    {
        return allRacers.OrderBy(r => r.currentPosition).ToList();
    }

    /// <summary>
    /// Reset race
    /// </summary>
    public void ResetRace()
    {
        foreach (var racer in allRacers)
        {
            racer.currentCheckpointIndex = 0;
            racer.lastCheckpointIndex = -1;
            racer.maxCheckpointInLap = 0;
            racer.spawnedBehindLine = false;
            racer.passedStartLine = false;
            racer.lapCount = 0;
            racer.currentPosition = 0;
            racer.totalProgress = 0;
            racer.hasFinished = false;
            racer.finishPosition = 0;
        }

        lastPlayerPosition = -1;
        nextFinishPosition = 1;
        playerSceneLoadScheduled = false;

        if (showDebugInfo)
            Debug.Log("Race reset");
    }

    /// <summary>
    /// Số vòng hiển thị (1-based, clamp ở totalLaps khi đã finish).
    /// </summary>
    private int GetDisplayLap(RacerData racer)
    {
        if (racer == null) return 1;
        if (racer.hasFinished) return totalLaps;
        return Mathf.Clamp(racer.lapCount + 1, 1, totalLaps);
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (!showDebugInfo || checkpoints.Count == 0) return;

        // Vẽ checkpoint path
        Gizmos.color = Color.yellow;
        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i] == null) continue;
            
            int nextIndex = (i + 1) % checkpoints.Count;
            if (checkpoints[nextIndex] != null)
            {
                Gizmos.DrawLine(checkpoints[i].position, checkpoints[nextIndex].position);
            }
            
            Gizmos.DrawWireSphere(checkpoints[i].position, 2f);
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Label("=== RACE POSITIONS ===");
        
        var ranked = GetRankedRacers();
        foreach (var racer in ranked)
        {
            if (racer.racerTransform == null) continue;
            
            string posText = $"{racer.currentPosition}. {racer.racerName}";
            posText += $" | CP: {racer.currentCheckpointIndex + 1}/{checkpoints.Count}";
            posText += $" | Lap: {GetDisplayLap(racer)}/{totalLaps}";
            
            GUILayout.Label(posText);
        }
        
        GUILayout.EndArea();
    }
}
