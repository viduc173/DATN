using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Michsky.UI.Heat;
using ALIyerEdon; // EasyCarController

/// <summary>
/// Khi PLAYER chạm vạch đích lap cuối (RacePositionTracker.onPlayerRaceFinished):
///   1. Cộng tiền thưởng cho player theo hạng về đích (lấy từ RaceSettings.prizeByPosition — chỉ top được thưởng).
///   2. Điền bảng kết quả: ghi tên các tay đua theo thứ tự về đích vào title của từng row Rank_* (icon số giữ nguyên).
///   3. Mở panel Achievements bằng animation của ModalWindowManager.OpenWindow().
///
/// Refs được Editor script "Tools/Stadium/Setup Results Board" gán sẵn, nhưng đều có fallback auto-resolve.
/// </summary>
[DefaultExecutionOrder(-20)]
public class RaceResultsController : MonoBehaviour
{
    [Header("Refs (auto-resolve nếu để trống)")]
    [Tooltip("Bảng xếp hạng. Để trống = FindObjectOfType.")]
    [SerializeField] private RacePositionTracker tracker;

    [Tooltip("Panel Achievements (ModalWindowManager) sẽ mở khi về đích.")]
    [SerializeField] private ModalWindowManager achievementsWindow;

    [Tooltip("Ví người chơi (PlayerInventory asset) để cộng thưởng.")]
    [SerializeField] private PlayerInventory inventory;

    [Tooltip("Container chứa các row Rank_* (Layout Group). Mỗi con có AchievementItem; thứ tự sibling = hạng 1..N.")]
    [SerializeField] private Transform rankBoardContainer;

    [Tooltip("RaceSettings dùng để tra tiền thưởng. Để trống = lấy từ tracker.raceSettings.")]
    [SerializeField] private RaceSettings raceSettings;

    [Header("Hiển thị")]
    [Tooltip("Tự mở panel Achievements khi player về đích.")]
    [SerializeField] private bool autoOpenWindow = true;

    [Tooltip("Khi kết thúc chặng: phanh cứng + dừng hẳn TẤT CẢ xe (player + AI).")]
    [SerializeField] private bool stopVehiclesOnFinish = true;

    [Tooltip("Màu title của dòng Player trên bảng kết quả.")]
    [SerializeField] private Color playerRowColor = Color.yellow;

    [Tooltip("Màu title của các dòng còn lại.")]
    [SerializeField] private Color otherRowColor = Color.white;

    [Tooltip("Ẩn hẳn (SetActive false) các dòng Rank không có tay đua tương ứng. " +
             "Tắt = giữ dòng và hiện emptyRowText.")]
    [SerializeField] private bool hideEmptyRows = true;

    [Tooltip("Text cho row không có tay đua tương ứng (chỉ dùng khi hideEmptyRows = false).")]
    [SerializeField] private string emptyRowText = "---";

    [Tooltip("Định dạng tiền thưởng hiển thị ở 'Rank_X > Gold > Title'. {0} = số gold. Vd '+{0:N0} gold'.")]
    [SerializeField] private string goldFormat = "+{0:N0} gold";

    [Tooltip("Tên GameObject con chứa tiền thưởng trong mỗi Rank (Rank_X > Gold > Title).")]
    [SerializeField] private string goldChildName = "Gold";

    private bool _handled;

    /// <summary>Bảng kết quả (Achievements) — cho VR (VRRaceMenu) phân biệt với modal exit.</summary>
    public ModalWindowManager AchievementsWindow => achievementsWindow;

    private void Awake()
    {
        if (tracker == null)
            tracker = FindObjectOfType<RacePositionTracker>();
    }

    private void OnEnable()
    {
        if (tracker != null)
            tracker.onPlayerRaceFinished.AddListener(HandlePlayerFinished);
    }

    private void OnDisable()
    {
        if (tracker != null)
            tracker.onPlayerRaceFinished.RemoveListener(HandlePlayerFinished);
    }

    /// <summary>Gọi khi PLAYER về đích (qua RacePositionTracker.onPlayerRaceFinished).</summary>
    public void HandlePlayerFinished()
    {
        if (_handled) return;
        _handled = true;

        RaceSettings settings = ResolveSettings();

        // Snapshot xếp hạng tại thời điểm player về đích, sắp xếp ngay bằng các field đã chính xác
        // (finishPosition / totalProgress) thay vì currentPosition — vì currentPosition chỉ được
        // CalculateRankings() cập nhật ở CUỐI frame, sau khi event này đã bắn.
        List<RacePositionTracker.RacerData> ranked = SortedStandings();

        if (stopVehiclesOnFinish)
            StopAllVehicles(ranked);

        GrantReward(settings, ranked);
        PopulateBoard(ranked, settings);

        if (autoOpenWindow && achievementsWindow != null)
            achievementsWindow.OpenWindow();
        else if (autoOpenWindow)
            Debug.LogWarning("[RaceResultsController] autoOpenWindow bật nhưng chưa gán achievementsWindow.");
    }

    /// <summary>
    /// Sắp xếp racers: xe đã về đích trước (theo finishPosition tăng dần), rồi xe chưa về đích
    /// (theo totalProgress giảm dần). Khớp với logic của RacePositionTracker.CalculateRankings.
    /// </summary>
    private List<RacePositionTracker.RacerData> SortedStandings()
    {
        return tracker.GetRankedRacers()
            .Where(r => r != null && r.racerTransform != null)
            .OrderBy(r => r.hasFinished ? 0 : 1)
            .ThenBy(r => r.hasFinished ? r.finishPosition : 0)
            .ThenByDescending(r => r.hasFinished ? 0 : r.totalProgress)
            .ToList();
    }

    /// <summary>
    /// Kết thúc chặng → phanh cứng + dừng hẳn mọi xe trong bảng (player + AI).
    /// </summary>
    private void StopAllVehicles(List<RacePositionTracker.RacerData> ranked)
    {
        foreach (var r in ranked)
        {
            if (r != null && r.racerTransform != null)
                StopVehicle(r.racerTransform);
        }
    }

    private void StopVehicle(Transform car)
    {
        // 1) Triệt tiêu quán tính của mọi Rigidbody trong xe
        foreach (var rb in car.GetComponentsInChildren<Rigidbody>())
        {
            if (rb.isKinematic) continue;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 2) Xe player (EVP VehicleController qua PlayerDriveInputManager): phanh cứng + ngắt input lái
        foreach (var pin in car.GetComponentsInChildren<PlayerDriverInputFromKeyboard>())
            pin.enabled = false; // ngừng đọc WASD/chuột để không ghi đè lệnh phanh

        foreach (var vrin in car.GetComponentsInChildren<PlayerInputFromVRController>())
            vrin.enabled = false; // VR: ngừng đọc cần gạt (cũng khoá nitro qua NitroController guard)

        foreach (var pim in car.GetComponentsInChildren<PlayerDriveInputManager>())
            pim.SetValue(0f, 0f, 1f, 1f, 0); // steer0, move0, brake1, handbrake1, gear0

        // 3) Xe AI (EasyCarController + CarAIController): ngắt AI + phanh tay
        foreach (var ai in car.GetComponentsInChildren<CarAIController>())
            ai.enabled = false;

        foreach (var ec in car.GetComponentsInChildren<EasyCarController>())
        {
            ec.throttleInput = 0f;
            ec.handBrake = true;
            ec.Clutch = true;
        }
    }

    private void GrantReward(RaceSettings settings, List<RacePositionTracker.RacerData> ranked)
    {
        if (settings == null || inventory == null) return;

        RacePositionTracker.RacerData player = ranked.FirstOrDefault(r => r.racerName == "Player");
        int position = player != null && player.hasFinished
            ? player.finishPosition
            : (tracker != null ? tracker.GetPlayerPosition() : 0);

        int prize = settings.GetPrize(position);
        if (prize <= 0)
        {
            Debug.Log($"[RaceResultsController] Player về hạng {position} — không có thưởng.");
            return;
        }

        inventory.AddGold(prize);
        inventory.SaveToPlayerPrefs();
        Debug.Log($"[RaceResultsController] Player về hạng {position} → +{prize}g. Tổng: {inventory.gold}g");
    }

    private void PopulateBoard(List<RacePositionTracker.RacerData> ranked, RaceSettings settings)
    {
        if (rankBoardContainer == null) return;

        int i = 0;
        foreach (Transform child in rankBoardContainer)
        {
            AchievementItem row = child.GetComponent<AchievementItem>();
            if (row == null || row.titleObj == null) continue;

            int position = i + 1; // thứ tự sibling = hạng (1..N), khớp icon số trên row
            SetGoldPrize(child, settings, position);

            bool hasRacer = i < ranked.Count && ranked[i] != null && ranked[i].racerTransform != null;

            if (hasRacer)
            {
                if (!child.gameObject.activeSelf) child.gameObject.SetActive(true);
                bool isPlayer = ranked[i].racerName == "Player";
                row.titleObj.text = ranked[i].racerName;
                row.titleObj.color = isPlayer ? playerRowColor : otherRowColor;
            }
            else if (hideEmptyRows)
            {
                // Ít xe hơn số dòng → ẩn hẳn dòng thừa
                child.gameObject.SetActive(false);
            }
            else
            {
                if (!child.gameObject.activeSelf) child.gameObject.SetActive(true);
                row.titleObj.text = emptyRowText;
                row.titleObj.color = otherRowColor;
            }

            i++;
        }
    }

    /// <summary>
    /// Ghi tiền thưởng của hạng (lấy từ RaceSettings.prizeByPosition) vào 'Rank_X > Gold > Title'.
    /// </summary>
    private void SetGoldPrize(Transform row, RaceSettings settings, int position)
    {
        if (settings == null) return;

        Transform gold = row.Find(goldChildName);
        if (gold == null) return;

        // Title nằm dưới Gold; lấy TMP đầu tiên (Gold không có TMP riêng, chỉ con "Title" có).
        TextMeshProUGUI tmp = gold.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp == null) return;

        int prize = settings.GetPrize(position);
        if (prize > 0)
            tmp.text = string.Format(goldFormat, prize);
    }

    private RaceSettings ResolveSettings()
    {
        if (raceSettings != null) return raceSettings;
        if (tracker != null) raceSettings = tracker.raceSettings;
        return raceSettings;
    }

    /// <summary>Wire vào nút Confirm/Back của panel để quay lại Garage.</summary>
    public void ReturnToGarage()
    {
        RaceSettings settings = ResolveSettings();
        string scene = settings != null && !string.IsNullOrEmpty(settings.endSceneName)
            ? settings.endSceneName
            : "GarageLobby_pc";
        SceneManager.LoadScene(scene);
    }

#if UNITY_EDITOR
    [ContextMenu("DEBUG: Trigger Finish")]
    private void DebugTriggerFinish() => HandlePlayerFinished();
#endif
}
