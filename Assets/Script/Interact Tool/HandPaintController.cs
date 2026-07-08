using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Gán lên XR Controller (cùng GameObject với HandInputValue).
/// Theo dõi vật đang được cầm — nếu là CarPaintCan thì bridge
/// các event từ HandInputValue sang CarPaintCan.
/// </summary>
[RequireComponent(typeof(HandInputValue))]
public class HandPaintController : MonoBehaviour
{
    // ─── Cấu hình ─────────────────────────────────────────────────────────────

    [Header("XR Controller")]
    [Tooltip("XRDirectInteractor hoặc XRRayInteractor trên tay này")]
    [SerializeField] private XRBaseControllerInteractor handInteractor;

    [Header("Hành động khi bấm nút (nếu cầm bình sơn)")]
    [Tooltip("Primary Button (thường là Button A/X) → ApplyPaint")]
    [SerializeField] private PaintAction primaryAction = PaintAction.ApplyPaint;

    [Tooltip("Secondary Button (thường là Button B/Y) → CancelPreview")]
    [SerializeField] private PaintAction secondaryAction = PaintAction.CancelPreview;

    [Tooltip("Khi nhặt bình sơn lên → tự động gọi PreviewPaint ngay")]
    [SerializeField] private bool autoPreviewOnGrab = true;

    public enum PaintAction { None, PreviewPaint, ApplyPaint, CancelPreview }

    // ─── Private State ────────────────────────────────────────────────────────

    private HandInputValue handInput;
    private CarPaintCan currentPaintCan; // Bình sơn đang cầm (null nếu không phải)

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        handInput = GetComponent<HandInputValue>();

        if (handInteractor == null)
        {
            // Tự tìm trong cùng GameObject hoặc children
            handInteractor = GetComponent<XRBaseControllerInteractor>();
            if (handInteractor == null)
                handInteractor = GetComponentInChildren<XRBaseControllerInteractor>();
        }

        if (handInteractor == null)
            Debug.LogWarning($"[HandPaintController: {name}] Không tìm thấy XRBaseControllerInteractor!");
    }

    private void OnEnable()
    {
        if (handInteractor != null)
        {
            handInteractor.selectEntered.AddListener(OnGrabObject);
            handInteractor.selectExited.AddListener(OnReleaseObject);
        }

        handInput.primaryPressEvent.AddListener(OnPrimaryPress);
        handInput.secondaryPressEvent.AddListener(OnSecondaryPress);
    }

    private void OnDisable()
    {
        if (handInteractor != null)
        {
            handInteractor.selectEntered.RemoveListener(OnGrabObject);
            handInteractor.selectExited.RemoveListener(OnReleaseObject);
        }

        handInput.primaryPressEvent.RemoveListener(OnPrimaryPress);
        handInput.secondaryPressEvent.RemoveListener(OnSecondaryPress);
    }

    // ─── Grab / Release ───────────────────────────────────────────────────────

    private void OnGrabObject(SelectEnterEventArgs args)
    {
        // Lấy CarPaintCan từ vật vừa cầm
        currentPaintCan = args.interactableObject.transform.GetComponent<CarPaintCan>();

        if (currentPaintCan != null)
        {
            Debug.Log($"[HandPaintController] Đang cầm bình sơn: {currentPaintCan.name}");

            if (autoPreviewOnGrab)
                currentPaintCan.PreviewPaint();
        }
        else
        {
            Debug.Log($"[HandPaintController] Cầm vật: {args.interactableObject.transform.name} (không phải bình sơn)");
        }
    }

    private void OnReleaseObject(SelectExitEventArgs args)
    {
        if (currentPaintCan != null)
        {
            // Khi thả bình sơn → tự động hủy preview nếu chưa apply
            currentPaintCan.CancelPreview();
            Debug.Log($"[HandPaintController] Thả bình sơn: {currentPaintCan.name}");
        }

        currentPaintCan = null;
    }

    // ─── Button Press ─────────────────────────────────────────────────────────

    private void OnPrimaryPress()
    {
        ExecuteAction(primaryAction);
    }

    private void OnSecondaryPress()
    {
        ExecuteAction(secondaryAction);
    }

    private void ExecuteAction(PaintAction action)
    {
        if (currentPaintCan == null) return; // Không cầm bình sơn → bỏ qua

        switch (action)
        {
            case PaintAction.PreviewPaint:   currentPaintCan.PreviewPaint();   break;
            case PaintAction.ApplyPaint:     currentPaintCan.ApplyPaint();     break;
            case PaintAction.CancelPreview:  currentPaintCan.CancelPreview();  break;
            case PaintAction.None: break;
        }
    }

    // ─── Public Helper ────────────────────────────────────────────────────────

    /// <summary>Trả về bình sơn đang cầm, null nếu không có.</summary>
    public CarPaintCan GetCurrentPaintCan() => currentPaintCan;

    /// <summary>Tay này có đang cầm bình sơn không?</summary>
    public bool IsHoldingPaintCan => currentPaintCan != null;
}