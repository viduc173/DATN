using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Trình quản lý các phím chức năng (Hotkey) tổng hợp dành cho PC.
/// Cung cấp list cấu hình các phím bấm (như Tab, M, I...) để kích hoạt các sự kiện UnityEvent.
/// Phù hợp cho việc Open/Close Menu Panel, tương tác UI, bật/tắt tính năng đặc biệt.
/// </summary>
public class PCHotkeyManager : MonoBehaviour
{
    public enum KeyActionType
    {
        PressOnly,   // Chỉ gọi sự kiện 1 lần khi nhấn phím (VD: Bấm R để nạp đạn).
        Hold,        // Gọi sự kiện On khi bấm giữ, sự kiện Off khi nhả phím (VD: Giữ Tab để xem bảng điểm).
        Toggle       // Bấm lần 1 gọi Sự kiện Bật, Bấm lần 2 gọi Sự kiện Tắt (VD: Bấm M để mở map, bấm lúc nữa để đóng map).
    }

    [System.Serializable]
    public class HotkeyBinding
    {
        [Tooltip("Tên để dễ nhận diện trong Inspector")]
        public string actionName = "Mở Menu";
        
        [Tooltip("Phím cần bấm")]
        public KeyCode key = KeyCode.Tab;
        
        [Tooltip("Cách thức hoạt động của phím này")]
        public KeyActionType actionType = KeyActionType.Toggle;

        [Space(10)]
        [Header("Events")]
        [Tooltip("Gọi khi bấm phím (Press/Hold Start/Toggle On)")]
        public UnityEvent onActionStart; 
        
        [Tooltip("Gọi khi nhả phím (Hold End) hoặc bấm lại lần 2 (Toggle Off)")]
        public UnityEvent onActionEnd;

        // Lưu trữ trạng thái bật/tắt nội bộ cho chế độ Toggle
        [HideInInspector]
        public bool isToggledOn = false;
    }

    [Header("Cấu hình Phím Tắt")]
    [Tooltip("Thêm các phím bạn muốn quản lý vào đây.")]
    [SerializeField] private List<HotkeyBinding> bindings = new List<HotkeyBinding>();

    [Header("Cài đặt chung")]
    [Tooltip("Bật/Tắt toàn bộ hệ thống phím này")]
    public bool enableInput = true;

    private void Update()
    {
        if (!enableInput || bindings == null) return;

        foreach (var bind in bindings)
        {
            if (bind.key == KeyCode.None) continue;

            switch (bind.actionType)
            {
                case KeyActionType.PressOnly:
                    if (Input.GetKeyDown(bind.key))
                    {
                        bind.onActionStart?.Invoke();
                    }
                    break;

                case KeyActionType.Hold:
                    if (Input.GetKeyDown(bind.key))
                    {
                        bind.onActionStart?.Invoke();
                    }
                    else if (Input.GetKeyUp(bind.key))
                    {
                        bind.onActionEnd?.Invoke();
                    }
                    break;

                case KeyActionType.Toggle:
                    if (Input.GetKeyDown(bind.key))
                    {
                        bind.isToggledOn = !bind.isToggledOn;
                        if (bind.isToggledOn)
                        {
                            bind.onActionStart?.Invoke();
                        }
                        else
                        {
                            bind.onActionEnd?.Invoke();
                        }
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Cho phép bạn dùng nút [X] bằng chuột trên UI gọi hàm này để ép Reset cờ trạng thái "Toggle về false"
    /// Nếu UI bị đóng theo cách không bấm phím (như click nút Đóng bằng chuột).
    /// </summary>
    /// <param name="actionName">Đúng với Action Name trong Inspector để script tìm ra phím.</param>
    public void ForceResetToggleState(string actionName)
    {
        foreach (var bind in bindings)
        {
            if (bind.actionName == actionName && bind.actionType == KeyActionType.Toggle)
            {
                bind.isToggledOn = false;
            }
        }
    }
}
