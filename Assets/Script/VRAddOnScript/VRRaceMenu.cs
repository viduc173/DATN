using UnityEngine;
using UnityEngine.InputSystem;
using Michsky.UI.Heat;

/// <summary>
/// Quản UI menu/exit cho scene ĐUA trong VR. Khác với <see cref="VRMenuToggle"/> (bật/tắt cả canvas menu),
/// scene đua dùng <c>UI_Main Menu</c> như một CONTAINER chứa nhiều <see cref="ModalWindowManager"/>:
///   • <b>Achievements</b> — bảng kết quả, TỰ mở khi về đích (RaceResultsController.OpenWindow).
///   • <b>Exit/Pause</b> — modal thoát chặng (bản PC mở bằng phím Tab qua PCHotkeyManager — VR không có).
/// Vì các modal là CON của <c>UI_Main Menu</c> nên canvas này phải LUÔN ACTIVE (tắt nó là mất luôn bảng
/// kết quả + modal exit). Script này:
///   1. Bấm nút tay (mặc định A/X trái) → mở/đóng modal <see cref="exitModal"/> (OpenWindow/CloseWindow) — thay phím Tab.
///   2. Tự HIỆN tia khi CÓ modal đang mở (kết quả hoặc exit), ẩn tia khi không có modal nào (đỡ rối lúc lái).
///   3. Khi có modal mở → kéo canvas ra trước mặt (<see cref="VRWorldSpaceMenu.Recenter"/>).
///
/// Gắn lên 1 GameObject LUÔN ACTIVE (vd EventSystem). Nút Exit trong modal (SceneChanger.LoadScene /
/// RaceResultsController.ReturnToGarage) bấm được bằng tia vì modal nằm trong canvas world-space.
/// </summary>
public class VRRaceMenu : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Modal Exit/Pause (ModalWindowManager) — nút tay mở/đóng. = panel Tab mở ở bản PC. " +
             "Để trống → tự đoán: modal trong modalRoot KHÁC bảng kết quả của RaceResultsController.")]
    [SerializeField] private ModalWindowManager exitModal;

    [Tooltip("Canvas root world-space (UI_Main Menu) để kéo ra trước mặt khi mở modal.")]
    [SerializeField] private VRWorldSpaceMenu placer;

    [Tooltip("Gốc chứa các ModalWindowManager (= UI_Main Menu). Quét isOn để biết có modal nào đang mở.")]
    [SerializeField] private Transform modalRoot;

    [Tooltip("Các tia (Ray) — hiện khi có modal mở, ẩn khi không (mỗi xe/rig một tia).")]
    [SerializeField] private GameObject[] rayObjects;

    [Header("Cài đặt")]
    [Tooltip("Nút mở/đóng modal Exit. Mặc định nút A/primary tay trái.")]
    [SerializeField] private string bindingPath = "<XRController>{LeftHand}/primaryButton";

    [Tooltip("Bật log để kiểm tra: nút có nhận không, tìm thấy mấy modal, modal exit là cái nào.")]
    [SerializeField] private bool showDebugLog = false;

    private InputAction _toggle;
    private ModalWindowManager[] _modals;
    private bool _raysShown;

    private void Awake()
    {
        _toggle = new InputAction("VRRaceMenuToggle", InputActionType.Button, bindingPath);
        _toggle.performed += OnButton;

        if (modalRoot == null) modalRoot = transform;
        _modals = modalRoot.GetComponentsInChildren<ModalWindowManager>(true);

        if (exitModal == null) exitModal = GuessExitModal();

        if (showDebugLog)
            Debug.Log($"[VRRaceMenu] start — binding='{bindingPath}', modals={(_modals != null ? _modals.Length : 0)}, " +
                      $"exitModal={(exitModal != null ? exitModal.name : "NULL")}, placer={(placer != null ? "OK" : "NULL")}, " +
                      $"rays={(rayObjects != null ? rayObjects.Length : 0)}");
    }

    private void Start() => SetRaysActive(false); // vào game chưa có modal → ẩn tia

    private void OnEnable()  { _toggle?.Enable(); }
    private void OnDisable() { _toggle?.Disable(); }
    private void OnDestroy() { if (_toggle != null) _toggle.performed -= OnButton; }

    private void OnButton(InputAction.CallbackContext _)
    {
        if (exitModal == null)
        {
            if (showDebugLog) Debug.LogWarning("[VRRaceMenu] nút nhận nhưng exitModal = NULL (chưa wire / đoán sai).");
            return;
        }

        // Dùng activeInHierarchy làm "đang hiện" thay vì isOn: modal tắt sẵn (startBehaviour=Disable) có thể
        // chưa chạy Start → isOn lệch → OpenWindow() return sớm (ModalWindowManager.cs:159). activeInHierarchy
        // phản ánh đúng modal có đang bật hay không.
        bool showing = exitModal.gameObject.activeInHierarchy;
        if (showDebugLog)
            Debug.Log($"[VRRaceMenu] nút nhận — exitModal='{exitModal.name}', active={showing}, isOn={exitModal.isOn} → {(showing ? "CLOSE" : "OPEN")}");

        if (showing)
        {
            exitModal.CloseWindow();
        }
        else
        {
            if (placer != null) placer.Recenter();
            exitModal.isOn = false;     // chống isOn lệch khiến OpenWindow() bỏ qua (return sớm)
            exitModal.OpenWindow();
        }
    }

    private void Update()
    {
        bool anyOpen = AnyModalOpen();
        if (anyOpen == _raysShown) return;

        _raysShown = anyOpen;
        SetRaysActive(anyOpen);
        if (anyOpen && placer != null) placer.Recenter(); // modal (kể cả kết quả tự mở) → kéo ra trước mặt
    }

    private bool AnyModalOpen()
    {
        if (_modals == null) return false;
        foreach (var m in _modals)
            if (m != null && m.gameObject.activeInHierarchy && m.isOn) return true;
        return false;
    }

    private void SetRaysActive(bool active)
    {
        if (rayObjects == null) return;
        foreach (var r in rayObjects)
            if (r != null) r.SetActive(active);
    }

    // Đoán modal exit = modal đầu tiên trong modalRoot KHÁC bảng kết quả (achievementsWindow của RaceResultsController).
    private ModalWindowManager GuessExitModal()
    {
        if (_modals == null || _modals.Length == 0) return null;

        ModalWindowManager results = null;
        var rrc = FindFirstObjectByType<RaceResultsController>();
        if (rrc != null) results = rrc.AchievementsWindow;

        foreach (var m in _modals)
            if (m != null && m != results) return m;
        return _modals[0];
    }
}
