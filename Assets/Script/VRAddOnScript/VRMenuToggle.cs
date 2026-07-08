using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bật/tắt một UI (vd UI_Main Menu) bằng 1 nút trên tay VR — mặc định nút A/primary tay TRÁI.
/// Khi hiện, tự kéo menu ra trước mặt qua <see cref="VRWorldSpaceMenu.Recenter"/>.
///
/// Gắn lên 1 GameObject LUÔN ACTIVE (vd EventSystem hoặc XR Origin) — KHÔNG gắn lên chính menu,
/// vì menu sẽ bị tắt đi (component trên object tắt sẽ không nghe được nút bấm để mở lại).
///
/// Binding mặc định "&lt;XRController&gt;{LeftHand}/primaryButton" = nút A/X tay trái (Oculus/OpenXR).
/// Đổi sang "{RightHand}" hoặc "secondaryButton" nếu muốn nút khác.
/// </summary>
public class VRMenuToggle : MonoBehaviour
{
    [Header("Tham chiếu")]
    [Tooltip("GameObject UI cần bật/tắt (canvas root của UI_Main Menu).")]
    [SerializeField] private GameObject menu;
    [Tooltip("Tùy chọn: GameObject chứa tia (Ray). Bật/tắt theo menu để mở menu là luôn thấy tia.")]
    [SerializeField] private GameObject rayObject;
    [Tooltip("Tùy chọn: NHIỀU tia (vd mỗi xe/rig một tia — scene đua có 3 CarType). Bật/tắt CÙNG menu " +
             "với rayObject. Rig nào không active thì SetActive vô hại.")]
    [SerializeField] private GameObject[] rayObjects;
    [Tooltip("Tùy chọn: VRWorldSpaceMenu trên menu, để kéo ra trước mặt khi hiện.")]
    [SerializeField] private VRWorldSpaceMenu placer;

    [Header("Cài đặt")]
    [Tooltip("Ẩn menu khi bắt đầu game (để nút A bật lên).")]
    [SerializeField] private bool hideOnStart = true;
    [Tooltip("Đường dẫn binding nút bật/tắt. Mặc định nút A/primary tay trái.")]
    [SerializeField] private string bindingPath = "<XRController>{LeftHand}/primaryButton";

    private InputAction _toggle;

    private void Awake()
    {
        _toggle = new InputAction("ToggleVRMenu", InputActionType.Button, bindingPath);
        _toggle.performed += OnToggle;
    }

    private void Start()
    {
        if (hideOnStart)
        {
            if (menu != null) menu.SetActive(false);
            SetRaysActive(false);
        }
    }

    private void OnEnable()  { _toggle?.Enable(); }
    private void OnDisable() { _toggle?.Disable(); }
    private void OnDestroy() { if (_toggle != null) _toggle.performed -= OnToggle; }

    private void OnToggle(InputAction.CallbackContext _)
    {
        if (menu == null) return;
        bool show = !menu.activeSelf;
        menu.SetActive(show);
        SetRaysActive(show);                                 // mở menu là luôn hiện tia
        if (show && placer != null) placer.Recenter();
    }

    // Bật/tắt cả tia đơn (rayObject) lẫn danh sách tia (rayObjects, cho scene nhiều rig).
    private void SetRaysActive(bool active)
    {
        if (rayObject != null) rayObject.SetActive(active);
        if (rayObjects != null)
            foreach (var r in rayObjects)
                if (r != null) r.SetActive(active);
    }

    /// <summary>Cho UnityEvent/nút khác gọi để bật/tắt thủ công.</summary>
    public void Toggle() => OnToggle(default);
}
