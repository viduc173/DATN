using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Cầu nối VR cho bình sơn: khi đang CẦM bình (XR grab) và bóp cò (Activate),
/// gọi <see cref="CarPaintCan.ApplyPaint"/> — đúng việc mà PCInteractorObject làm khi bấm F trên PC.
///
/// VR flow:
///   Tay VR chạm/cầm bình sơn  → XRGrabInteractable.selectEntered (cầm lên)
///   Bóp cò (Activate action)  → XRGrabInteractable.activated → ApplyPaint() lên xe đang active
///
/// Tái sử dụng: gắn cùng GameObject với CarPaintCan + XRGrabInteractable (mọi sprayCan_*).
/// Không thay thế PCInteractorObject — hai hệ chạy song song (F cho PC, cò cho VR).
/// </summary>
[RequireComponent(typeof(CarPaintCan))]
[RequireComponent(typeof(XRGrabInteractable))]
public class XRPaintCanActivator : MonoBehaviour
{
    private CarPaintCan _paintCan;
    private XRGrabInteractable _grab;

    private void Awake()
    {
        _paintCan = GetComponent<CarPaintCan>();
        _grab = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if (_grab != null) _grab.activated.AddListener(OnActivated);
    }

    private void OnDisable()
    {
        if (_grab != null) _grab.activated.RemoveListener(OnActivated);
    }

    private void OnActivated(ActivateEventArgs args)
    {
        if (_paintCan != null)
            _paintCan.ApplyPaint();
    }
}
