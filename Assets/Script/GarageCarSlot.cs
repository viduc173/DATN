using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Đại diện cho 1 xe trong CarPlace. Auto-add bởi GarageCarManager.
///
/// Vấn đề gốc rễ:
///   CarType_X có Rigidbody (mass=1200, isKinematic=0, gravity=1) + WheelCollider trong children.
///   SetActive(false→true) phá physics vì:
///     1. WheelCollider mất toàn bộ trạng thái suspension khi deactivate
///     2. Frame đầu sau SetActive(true): Rigidbody chịu gravity + WheelCollider tính
///        suspension sai (chưa có contact point) → bắn lực lớn → xe văng hoặc chìm đất
///
/// Giải pháp:
///   Garage là màn trưng bày — xe không cần physics động.
///   → isKinematic=true TRƯỚC khi deactivate (WheelCollider không còn lực để bắn)
///   → Giữ isKinematic=true sau activate (xe đứng yên trên sàn)
///   → WheelCollider vẫn chạy nhưng không gây lực vì Rigidbody kinematic
/// </summary>
public class GarageCarSlot : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent onActivated;
    public UnityEvent onDeactivated;

    private int _slotIndex;
    private GarageCarManager _manager;
    private PCInteractorObject[] _interactors;

    // Chỉ lấy Rigidbody trên root — KHÔNG GetComponentsInChildren
    // vì WheelItem cũng có Rigidbody riêng do nó tự quản lý
    private Rigidbody _rootRb;
    private WheelCollider[] _wheelColliders;

    public bool IsActive  => gameObject.activeSelf;
    public int  SlotIndex => _slotIndex;

    public void Init(int index, GarageCarManager manager)
    {
        _slotIndex = index;
        _manager   = manager;

        _rootRb        = GetComponent<Rigidbody>();
        _wheelColliders = GetComponentsInChildren<WheelCollider>(includeInactive: true);
        _interactors   = GetComponentsInChildren<PCInteractorObject>(includeInactive: true);
    }

    // ── Core ─────────────────────────────────────────────────────────────────────

    /// <summary>Gọi bởi GarageCarManager — hoạt động kể cả khi GO đang inactive.</summary>
    public void SetActive(bool active)
    {
        if (active) Activate();
        else        Deactivate();
    }

    private void Deactivate()
    {
        // Bước 1: Freeze Rigidbody TRƯỚC khi deactivate
        // Khi isKinematic=true, WheelCollider không còn lực nào để bắn khi reactivate
        if (_rootRb != null)
        {
            _rootRb.isKinematic    = true;
            _rootRb.linearVelocity  = Vector3.zero;
            _rootRb.angularVelocity = Vector3.zero;
        }

        gameObject.SetActive(false);
        onDeactivated?.Invoke();
    }

    private void Activate()
    {
        gameObject.SetActive(true);

        // Bước 2: Giữ kinematic ngay sau activate — không để gravity/WheelCollider chạy
        if (_rootRb != null)
        {
            _rootRb.isKinematic    = true;
            _rootRb.linearVelocity  = Vector3.zero;
            _rootRb.angularVelocity = Vector3.zero;
        }

        // Bước 3: Reset WheelCollider inputs sau 1 physics frame để chúng khởi tạo xong
        StartCoroutine(ResetWheelCollidersAfterInit());

        // Enable interaction components
        if (_interactors != null)
            foreach (var io in _interactors)
                if (io != null) io.enabled = true;

        onActivated?.Invoke();
    }

    private IEnumerator ResetWheelCollidersAfterInit()
    {
        yield return new WaitForFixedUpdate();

        if (_wheelColliders == null) yield break;
        foreach (var wc in _wheelColliders)
        {
            if (wc == null) continue;
            wc.motorTorque = 0f;
            wc.brakeTorque = 0f;
            wc.steerAngle  = 0f;
        }
    }

    // ── Chọn xe bằng cách nhìn vào thân xe và bấm F ─────────────────────────────

    public void SelectThisCar() => _manager?.SelectSlot(this);
}
