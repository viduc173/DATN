using UnityEngine;
using Michsky.UI.Heat;

/// <summary>
/// Hiện + mở khóa con trỏ chuột khi bấm phím mở menu (mặc định Escape / Tab) trong scene đua.
///
/// Trong màn đua, <see cref="PlayerDriverInputFromKeyboard"/> khóa con trỏ ở Awake
/// (CursorLockMode.Locked + visible=false). Khi mở modal Quit/Pause bằng Escape/Tab, con trỏ vẫn
/// đang bị khóa nên không click được nút. Script này gỡ khóa con trỏ khi bấm các phím đó.
///
/// Gắn lên 1 GameObject luôn active trong scene (vd MatchController hoặc 1 object riêng).
///
/// KHUYẾN NGHỊ: bật <see cref="driveByModalState"/> — con trỏ sẽ tự bám theo trạng thái của
/// các <see cref="ModalWindowManager"/> trong scene: mở bất kỳ panel nào (Tab/Escape/nút) → hiện con trỏ;
/// đóng hết panel (bằng phím HAY nút) → tự khóa con trỏ lại. Không cần wire UnityEvent tay,
/// không lệ thuộc cách đóng panel.
/// </summary>
public class MenuCursorController : MonoBehaviour
{
    [Header("Phím mở menu → hiện con trỏ")]
    [Tooltip("Bấm bất kỳ phím nào trong list sẽ hiện + mở khóa con trỏ. Bỏ qua nếu bật driveByModalState.")]
    [SerializeField] private KeyCode[] showKeys = { KeyCode.Escape, KeyCode.Tab };

    [Header("Hành vi")]
    [Tooltip("Bật: con trỏ tự bám theo ModalWindowManager.isOn — mở panel thì hiện, đóng hết panel thì khóa lại. " +
             "Cách robust nhất, không cần phím/Event. Khi bật, phần xử lý phím bên dưới bị bỏ qua.")]
    [SerializeField] private bool driveByModalState = false;

    [Tooltip("Bật: bấm lại phím sẽ khóa con trỏ lại (toggle hiện/ẩn). Tắt: bấm chỉ luôn hiện con trỏ.")]
    [SerializeField] private bool toggle = false;

    [Tooltip("Khóa + ẩn con trỏ ngay khi Start (đảm bảo vào màn là chế độ lái).")]
    [SerializeField] private bool lockOnStart = false;

    private bool _cursorShown;
    private ModalWindowManager[] _modals;

    private void Start()
    {
        if (driveByModalState)
            _modals = FindObjectsOfType<ModalWindowManager>(true);

        if (lockOnStart)
            LockCursor();
    }

    private void Update()
    {
        if (driveByModalState)
        {
            UpdateByModalState();
            return;
        }

        if (showKeys == null) return;

        for (int i = 0; i < showKeys.Length; i++)
        {
            if (showKeys[i] == KeyCode.None || !Input.GetKeyDown(showKeys[i]))
                continue;

            if (toggle && _cursorShown)
                LockCursor();
            else
                ShowCursor();

            break;
        }
    }

    /// <summary>
    /// Hiện con trỏ khi có panel modal đang mở; khóa lại khi đã đóng hết.
    /// </summary>
    private void UpdateByModalState()
    {
        bool anyOpen = false;
        if (_modals != null)
        {
            for (int i = 0; i < _modals.Length; i++)
            {
                if (_modals[i] != null && _modals[i].isOn)
                {
                    anyOpen = true;
                    break;
                }
            }
        }

        if (anyOpen && !_cursorShown)
            ShowCursor();
        else if (!anyOpen && _cursorShown)
            LockCursor();
    }

    /// <summary>Hiện + mở khóa con trỏ. Gọi được từ UnityEvent (vd ModalWindowManager.onOpen).</summary>
    public void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _cursorShown = true;
    }

    /// <summary>Ẩn + khóa con trỏ về giữa màn hình. Gọi khi Resume/đóng menu.</summary>
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _cursorShown = false;
    }

    /// <summary>Đảo trạng thái con trỏ (tiện wire vào 1 nút duy nhất).</summary>
    public void ToggleCursor()
    {
        if (_cursorShown) LockCursor();
        else ShowCursor();
    }
}
