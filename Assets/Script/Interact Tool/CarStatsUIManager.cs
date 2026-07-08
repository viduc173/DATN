using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Quản lý UI hiển thị thông số xe hiện tại.
/// UI đọc CarLoadoutSlot.GetEffectiveStats() và tự refresh khi linh kiện thay đổi.
/// </summary>
public class CarStatsUIManager : MonoBehaviour
{
    // ─── Panel chính ──────────────────────────────────────────────────────────

    [Header("Stats Panel")]
    [Tooltip("Panel hiện thông số — bật/tắt khi thay linh kiện")]
    [SerializeField] private GameObject statsPanel;

    [Tooltip("Tự động ẩn panel sau N giây. 0 = không tự ẩn.")]
    [SerializeField] private float autoDismissDelay = 3f;

    [Header("Live Car Stats")]
    [SerializeField] private bool autoTrackActiveCar = true;
    [SerializeField] private bool showPanelOnStatsChanged = true;
    [SerializeField] private bool showPanelOnPartRemoved = false;

    [Header("Player Facing Panel")]
    [SerializeField] private bool placeInFrontOfPlayer = true;
    [SerializeField] private Camera targetCamera;
    [SerializeField, Range(0.1f, 0.9f)] private float anchorBlendFromPlayer = 0.5f;
    [SerializeField] private float fallbackFrontDistance = 1.6f;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.25f, 0f);
    [SerializeField] private float showDelayAfterStatsChanged = 0.5f;
    [SerializeField] private bool suppressPanelDuringStartup = true;
    [SerializeField] private float startupPanelSuppressSeconds = 1f;

    // ─── UI Rows ──────────────────────────────────────────────────────────────

    [Header("UI Rows")]
    [SerializeField] private StatRow maxSpeedRow;
    [SerializeField] private StatRow accelerationRow;
    [SerializeField] private StatRow gripRow;
    [SerializeField] private StatRow brakingRow;
    [SerializeField] private StatRow handlingRow;
    [SerializeField] private StatRow nitroRow;

    // ─── Delta Colors ─────────────────────────────────────────────────────────

    [Header("Delta Colors")]
    [SerializeField] private Color colorIncrease = new Color(0.2f, 0.9f, 0.2f);
    [SerializeField] private Color colorDecrease = new Color(0.9f, 0.2f, 0.2f);

    // ─── Animation ────────────────────────────────────────────────────────────

    [Header("Animation")]
    [Tooltip("Thời gian slider + số đếm chạy từ cũ → mới (giây)")]
    [SerializeField] private float animDuration = 0.6f;

    // ─── StatRow Definition ───────────────────────────────────────────────────

    [System.Serializable]
    public class StatRow
    {
        public string          label;
        public Slider          slider;
        public TextMeshProUGUI valueText;
        public TextMeshProUGUI deltaText;
        public GameObject      deltaArrowUp;
        public GameObject      deltaArrowDown;

        [HideInInspector] public Coroutine animCoroutine; // track per-row
    }

    // ─── Private State ────────────────────────────────────────────────────────

    private CarStats displayedStats = new CarStats { maxSpeed=50, acceleration=50, grip=50, braking=50, handling=50 };
    private CarStats previousStats  = new CarStats { maxSpeed=50, acceleration=50, grip=50, braking=50, handling=50 };
    private Coroutine dismissCoroutine;
    private CarLoadoutSlot trackedLoadoutSlot;
    private Canvas panelCanvas;
    private GraphicRaycaster panelRaycaster;
    private CanvasGroup panelCanvasGroup;
    private bool statsPanelIsSelf;
    private Coroutine delayedShowCoroutine;
    private bool allowPanelFromPartChanges;
    private static Transform pendingPartChangeAnchor;
    private static bool pendingPartChangeIsAttach = true;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (statsPanel == null)
            statsPanel = gameObject;

        statsPanelIsSelf = statsPanel == gameObject;
        panelCanvas = statsPanel.GetComponent<Canvas>();
        panelRaycaster = statsPanel.GetComponent<GraphicRaycaster>();
        panelCanvasGroup = statsPanel.GetComponent<CanvasGroup>();

        if (targetCamera == null)
            targetCamera = ResolveCamera();

        HidePanel();
    }

    private void OnEnable()
    {
        if (GarageCarManager.Instance != null)
            GarageCarManager.Instance.onCarChanged.AddListener(HandleActiveCarChanged);

        if (autoTrackActiveCar)
            BindActiveCarLoadout();

        // Panel có thể bị tắt khi đứng ở tab khác (vd Inventory) — lúc đó part đổi
        // sẽ fire StatsChanged mà panel không nghe được. Khi panel bật lại (quay về
        // CarInfo) phải đọc lại stats hiện tại để không hiển thị giá trị cũ.
        RefreshCurrentStats(showPanel: false, animate: false);
    }

    private void OnDisable()
    {
        if (GarageCarManager.Instance != null)
            GarageCarManager.Instance.onCarChanged.RemoveListener(HandleActiveCarChanged);

        UnbindTrackedLoadout();
    }

    private void Start()
    {
        if (autoTrackActiveCar)
            BindActiveCarLoadout();

        RefreshCurrentStats(showPanel: false, animate: false);
        StartCoroutine(RefreshAfterInitialRestore());
        StartCoroutine(EnablePanelAfterStartup());
    }

    private IEnumerator RefreshAfterInitialRestore()
    {
        yield return null;

        if (autoTrackActiveCar)
            BindActiveCarLoadout();

        RefreshCurrentStats(showPanel: false, animate: false);
    }

    private IEnumerator EnablePanelAfterStartup()
    {
        allowPanelFromPartChanges = !suppressPanelDuringStartup;

        if (suppressPanelDuringStartup)
        {
            float delay = Mathf.Max(0f, startupPanelSuppressSeconds);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            allowPanelFromPartChanges = true;
            ClearPendingPartChange();
        }
    }

    // ─── PUBLIC: Gọi từ UnityEvent ────────────────────────────────────────────

    /// <summary>Kept for existing UnityEvent wiring. Now refreshes real current car stats, not random values.</summary>
    public void TriggerRandomStatBoost()
    {
        RefreshCurrentStats(showPanel: true, animate: true);
    }

    public void RefreshCurrentStats()
        => RefreshCurrentStats(showPanel: true, animate: true);

    public void RefreshCurrentStats(bool showPanel, bool animate)
    {
        if (trackedLoadoutSlot == null && autoTrackActiveCar)
            BindActiveCarLoadout();

        CarStats stats = trackedLoadoutSlot != null
            ? trackedLoadoutSlot.GetEffectiveStats()
            : displayedStats;

        DisplayStats(stats, animate, showPanel);
    }

    public static void ReportPartChangeAnchor(Transform anchor)
    {
        ReportPartChangeAnchor(anchor, isAttach: true);
    }

    public static void ReportPartChangeAnchor(Transform anchor, bool isAttach)
    {
        pendingPartChangeAnchor = anchor;
        pendingPartChangeIsAttach = isAttach;
    }

    private void HandleActiveCarChanged(int _)
    {
        if (!autoTrackActiveCar) return;

        // Suppress panel trong lúc switch xe — xe mới khi activate sẽ fire
        // StatsChanged từ WheelSocket/WheelItem init, không phải thao tác của user
        allowPanelFromPartChanges = false;

        BindActiveCarLoadout();
        RefreshCurrentStats(showPanel: false, animate: false);

        StartCoroutine(RestoreAllowPanelAfterSwitch());
    }

    private IEnumerator RestoreAllowPanelAfterSwitch()
    {
        // Đợi qua 2 frame để các event init của xe mới hoàn tất
        yield return null;
        yield return null;
        allowPanelFromPartChanges = true;
    }

    private void BindActiveCarLoadout()
    {
        CarLoadoutSlot nextSlot = GarageCarManager.Instance?.ActiveSlot != null
            ? GarageCarManager.Instance.ActiveSlot.GetComponent<CarLoadoutSlot>()
            : null;

        if (trackedLoadoutSlot == nextSlot)
            return;

        UnbindTrackedLoadout();
        trackedLoadoutSlot = nextSlot;

        if (trackedLoadoutSlot != null)
            trackedLoadoutSlot.StatsChanged += HandleStatsChanged;
    }

    private void UnbindTrackedLoadout()
    {
        if (trackedLoadoutSlot != null)
            trackedLoadoutSlot.StatsChanged -= HandleStatsChanged;

        trackedLoadoutSlot = null;
    }

    private void HandleStatsChanged(CarStats stats)
    {
        bool shouldShowPanel = allowPanelFromPartChanges &&
                               showPanelOnStatsChanged &&
                               (pendingPartChangeIsAttach || showPanelOnPartRemoved);
        DisplayStats(stats, animate: true, showPanel: false, delayPanel: shouldShowPanel);
    }

    private void DisplayStats(CarStats stats, bool animate, bool showPanel)
        => DisplayStats(stats, animate, showPanel, delayPanel: false);

    private void DisplayStats(CarStats stats, bool animate, bool showPanel, bool delayPanel)
    {
        if (stats == null) return;

        previousStats = CloneStats(displayedStats);
        displayedStats = CloneStats(stats);

        if (animate)
        {
            AnimateRow(maxSpeedRow, previousStats.maxSpeed, displayedStats.maxSpeed);
            AnimateRow(accelerationRow, previousStats.acceleration, displayedStats.acceleration);
            AnimateRow(gripRow, previousStats.grip, displayedStats.grip);
            AnimateRow(brakingRow, previousStats.braking, displayedStats.braking);
            AnimateRow(handlingRow, previousStats.handling, displayedStats.handling);
            AnimateRow(nitroRow, previousStats.nitro, displayedStats.nitro);
        }
        else
        {
            SetRowImmediate(maxSpeedRow, displayedStats.maxSpeed);
            SetRowImmediate(accelerationRow, displayedStats.acceleration);
            SetRowImmediate(gripRow, displayedStats.grip);
            SetRowImmediate(brakingRow, displayedStats.braking);
            SetRowImmediate(handlingRow, displayedStats.handling);
            SetRowImmediate(nitroRow, displayedStats.nitro);
        }

        if (delayPanel)
        {
            ShowPanelDelayed();
        }
        else if (showPanel)
        {
            ShowPanel();
        }
        else
        {
            ClearPendingPartChange();
        }

        Debug.Log($"[CarStatsUIManager] Current stats → " +
                  $"Speed:{displayedStats.maxSpeed:F0} | " +
                  $"Accel:{displayedStats.acceleration:F0} | " +
                  $"Grip:{displayedStats.grip:F0} | " +
                  $"Brake:{displayedStats.braking:F0} | " +
                  $"Handle:{displayedStats.handling:F0} | " +
                  $"Nitro:{displayedStats.nitro:F0}");
    }

    // ─── Animate 1 Row ────────────────────────────────────────────────────────

    private void AnimateRow(StatRow row, float from, float to)
    {
        if (row == null) return;

        float delta = to - from;

        // Setup slider range
        if (row.slider != null)
        {
            row.slider.minValue     = 0f;
            row.slider.maxValue     = 100f;
            row.slider.interactable = false;
        }

        // Delta text — hiện ngay, không cần đợi animate
        if (row.deltaText != null)
        {
            if (Mathf.Approximately(delta, 0f))
            {
                row.deltaText.gameObject.SetActive(false);
            }
            else
            {
                row.deltaText.gameObject.SetActive(true);
                row.deltaText.text  = delta > 0 ? $"+{Mathf.RoundToInt(delta)}" : $"{Mathf.RoundToInt(delta)}";
                row.deltaText.color = delta > 0 ? colorIncrease : colorDecrease;
            }
        }

        // Mũi tên
        if (row.deltaArrowUp   != null) row.deltaArrowUp.SetActive(delta > 0);
        if (row.deltaArrowDown != null) row.deltaArrowDown.SetActive(delta < 0);

        // Stop coroutine cũ nếu đang chạy, bắt đầu mới
        if (row.animCoroutine != null) StopCoroutine(row.animCoroutine);
        row.animCoroutine = StartCoroutine(AnimateStatRow(row, from, to));
    }

    // ─── Coroutine: Slider trượt + số đếm từ from → to ───────────────────────

    private IEnumerator AnimateStatRow(StatRow row, float from, float to)
    {
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;

            float t        = Mathf.Clamp01(elapsed / animDuration);
            float easedT   = EaseOutCubic(t);           // bắt đầu nhanh, cuối chậm
            float current  = Mathf.Lerp(from, to, easedT);

            // Slider fill chạy từ from -> to (tăng lên hoặc giảm xuống)
            if (row.slider != null)
                row.slider.value = current;

            if (row.valueText != null)
                row.valueText.text = Mathf.RoundToInt(current).ToString();

            yield return null;
        }

        // Snap chính xác về đích
        if (row.slider    != null) row.slider.value   = to;
        if (row.valueText != null) row.valueText.text = Mathf.RoundToInt(to).ToString();

        row.animCoroutine = null;
    }

    // ─── Easing ───────────────────────────────────────────────────────────────

    // f(t) = 1 - (1-t)^3 : nhanh đầu, chậm về cuối
    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    // ─── Set giá trị ngay, không animate ─────────────────────────────────────

    private void SetRowImmediate(StatRow row, float value)
    {
        if (row == null) return;
        if (row.slider != null)
        {
            row.slider.minValue     = 0f;
            row.slider.maxValue     = 100f;
            row.slider.interactable = false;
            row.slider.value        = value;
        }
        if (row.valueText  != null) row.valueText.text = Mathf.RoundToInt(value).ToString();
        if (row.deltaText  != null) row.deltaText.gameObject.SetActive(false);
        if (row.deltaArrowUp   != null) row.deltaArrowUp.SetActive(false);
        if (row.deltaArrowDown != null) row.deltaArrowDown.SetActive(false);
    }

    // ─── Panel Show / Hide ────────────────────────────────────────────────────

    public void ShowPanel()
    {
        if (statsPanel == null) return;

        if (delayedShowCoroutine != null)
        {
            StopCoroutine(delayedShowCoroutine);
            delayedShowCoroutine = null;
        }

        PlacePanelBetweenPlayerAndPart();
        SetPanelVisible(true);
        ClearPendingPartChange();

        if (autoDismissDelay > 0f)
        {
            if (dismissCoroutine != null) StopCoroutine(dismissCoroutine);
            dismissCoroutine = StartCoroutine(AutoDismiss());
        }
    }

    public void HidePanel()
    {
        if (delayedShowCoroutine != null)
        {
            StopCoroutine(delayedShowCoroutine);
            delayedShowCoroutine = null;
        }

        SetPanelVisible(false);
    }

    private void ShowPanelDelayed()
    {
        if (delayedShowCoroutine != null)
            StopCoroutine(delayedShowCoroutine);

        delayedShowCoroutine = StartCoroutine(ShowPanelAfterDelay());
    }

    private IEnumerator ShowPanelAfterDelay()
    {
        float delay = Mathf.Max(0f, showDelayAfterStatsChanged);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        delayedShowCoroutine = null;
        ShowPanel();
    }

    private IEnumerator AutoDismiss()
    {
        yield return new WaitForSeconds(autoDismissDelay);
        HidePanel();
        dismissCoroutine = null;
    }

    private void ClearPendingPartChange()
    {
        pendingPartChangeAnchor = null;
        pendingPartChangeIsAttach = true;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private CarStats CloneStats(CarStats s) => new CarStats
    {
        maxSpeed     = s.maxSpeed,
        acceleration = s.acceleration,
        grip         = s.grip,
        braking      = s.braking,
        handling     = s.handling,
        nitro        = s.nitro
    };

    private void SetPanelVisible(bool visible)
    {
        if (statsPanel == null) return;

        if (!statsPanelIsSelf)
        {
            statsPanel.SetActive(visible);
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (panelCanvas != null)
            panelCanvas.enabled = visible;

        if (panelRaycaster != null)
            panelRaycaster.enabled = visible;

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = visible ? 1f : 0f;
            panelCanvasGroup.interactable = visible;
            panelCanvasGroup.blocksRaycasts = visible;
        }
    }

    private void PlacePanelBetweenPlayerAndPart()
    {
        if (!placeInFrontOfPlayer || statsPanel == null)
            return;

        if (targetCamera == null)
            targetCamera = ResolveCamera();

        if (targetCamera == null)
            return;

        Transform cam = targetCamera.transform;
        RectTransform rect = statsPanel.GetComponent<RectTransform>();
        Transform panelTransform = rect != null ? rect : statsPanel.transform;

        Vector3 targetPosition;
        if (pendingPartChangeAnchor != null)
        {
            targetPosition = Vector3.Lerp(cam.position, pendingPartChangeAnchor.position, anchorBlendFromPlayer);
        }
        else
        {
            targetPosition = cam.position + cam.forward * fallbackFrontDistance;
        }

        panelTransform.position = targetPosition + worldOffset;

        Vector3 lookDirection = panelTransform.position - cam.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.0001f)
            panelTransform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
    }

    private Camera ResolveCamera()
    {
        if (Camera.main != null && Camera.main.isActiveAndEnabled)
            return Camera.main;

        Camera[] cameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in cameras)
        {
            if (cam != null && cam.isActiveAndEnabled)
                return cam;
        }

        return Camera.main;
    }

#if UNITY_EDITOR
    [ContextMenu("Test: Refresh Current Stats")]
    private void DbgRefreshCurrentStats() => RefreshCurrentStats();
#endif
}
