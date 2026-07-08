using UnityEngine;
using System.Collections;

/// <summary>
/// Gán vào bất kỳ đâu.
/// ShowAt(Transform) → di chuyển panel đến vị trí Transform rồi hiện lên,
/// sau đó tự ẩn sau displayDuration giây.
/// </summary>
public class TimedPanelController : MonoBehaviour
{
    // ─── Panel ────────────────────────────────────────────────────────────────

    [Header("Panel")]
    [Tooltip("GameObject cần hiện/ẩn")]
    [SerializeField] private GameObject panel;

    [Tooltip("Thời gian panel hiện trên màn hình (giây)")]
    [SerializeField] private float displayDuration = 3f;

    // ─── Vị trí mặc định ─────────────────────────────────────────────────────

    [Header("Vị trí mặc định")]
    [Tooltip("Nếu không truyền Transform vào, panel sẽ hiện ở vị trí này")]
    [SerializeField] private Transform defaultAnchor;

    // ─── Offset ───────────────────────────────────────────────────────────────

    [Header("Offset (World Space)")]
    [Tooltip("Dịch chuyển thêm so với vị trí anchor")]
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, 0.3f, 0f);

    // ─── Canvas Mode ─────────────────────────────────────────────────────────

    [Header("Canvas Mode")]
    [Tooltip("true = World Space Canvas. false = Screen Space Overlay/Camera")]
    [SerializeField] private bool isWorldSpaceCanvas = false;

    [Tooltip("Camera dùng để convert World → Screen (chỉ cần khi isWorldSpaceCanvas = false)")]
    [SerializeField] private Camera targetCamera;

    // ─── Private ─────────────────────────────────────────────────────────────

    private Coroutine hideCoroutine;
    private RectTransform panelRect;
    private Transform currentAnchor;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (panel != null)
        {
            panelRect = panel.GetComponent<RectTransform>();
            panel.SetActive(false);
        }

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Update()
    {
        // Liên tục bám theo anchor mỗi frame (Position & Rotation)
        if (panel != null && panel.activeInHierarchy && currentAnchor != null)
        {
            TrackAnchor(currentAnchor);
        }
    }

    // ─── PUBLIC API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Hiện panel tại vị trí của Transform truyền vào.
    /// </summary>
    public void ShowAt(Transform anchor)
    {
        if (panel == null) return;

        currentAnchor = anchor != null ? anchor : defaultAnchor;

        // ✅ FIX: SetActive(true) TRƯỚC — để RectTransform active trong hierarchy
        // rồi mới set position, tránh bị Canvas layout reset lại
        panel.SetActive(true);

        TryTriggerRandomStats();

        TrackAnchor(currentAnchor);
        Canvas.ForceUpdateCanvases();

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(AutoHide());
    }

    /// <summary>
    /// Hiện panel tại vị trí mặc định (defaultAnchor).
    /// Gán trực tiếp vào UnityEvent trong Inspector.
    /// </summary>
    public void ShowAtDefault()
    {
        ShowAt(defaultAnchor);
    }

    /// <summary>
    /// Hiện panel tại chỗ, không di chuyển.
    /// </summary>
    public void Show()
    {
        if (panel == null) return;

        currentAnchor = defaultAnchor;
        panel.SetActive(true);

        TryTriggerRandomStats();

        if (currentAnchor != null)
        {
            TrackAnchor(currentAnchor);
            Canvas.ForceUpdateCanvases();
        }

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(AutoHide());
    }

    private void TryTriggerRandomStats()
    {
        if (panel == null) return;

        CarStatsUIManager carStats = panel.GetComponent<CarStatsUIManager>();
        if (carStats != null)
        {
            carStats.TriggerRandomStatBoost();
        }
    }

    /// <summary>
    /// Ẩn panel ngay lập tức.
    /// </summary>
    public void Hide()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (panel != null) panel.SetActive(false);
        currentAnchor = null;
    }

    // ─── Di chuyển panel đến anchor ──────────────────────────────────────────

    private void TrackAnchor(Transform anchor)
    {
        if (anchor == null || panelRect == null) return;

        if (isWorldSpaceCanvas)
        {
            // ✅ Bám sát Tọa độ với offset quay theo hướng của anchor (Trục tọa độ local)
            panelRect.position = anchor.position + anchor.TransformDirection(positionOffset);

            // ✅ Tuân thủ Góc xoay (Rotation)
            panelRect.rotation = anchor.rotation;
        }
        else
        {
            // Màn hình Screen Space Overlay
            Vector3 targetWorldPos = anchor.position + anchor.TransformDirection(positionOffset);
            if (targetCamera != null)
            {
                panelRect.position = targetCamera.WorldToScreenPoint(targetWorldPos);
            }
        }
    }

    // ─── Auto Hide ────────────────────────────────────────────────────────────

    private IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(displayDuration);
        Hide();
    }

#if UNITY_EDITOR
    [ContextMenu("Test: Show At Default")]  void DbgShow() => ShowAtDefault();
    [ContextMenu("Test: Hide Immediately")] void DbgHide() => Hide();
#endif
}