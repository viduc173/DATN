using UnityEngine;

/// <summary>
/// Gắn lên UI menu (vd UI_Main Menu).
/// Menu bật (OnEnable) → hiện chuột + tắt xoay camera.
/// Menu tắt (OnDisable) → khoá chuột + bật xoay camera.
/// Yêu cầu menu được ẩn/hiện bằng SetActive — OnEnable/OnDisable không chạy nếu dùng alpha/CanvasGroup.
/// </summary>
public class MenuCursorBinder : MonoBehaviour
{
    [Tooltip("Bỏ trống = tự tìm PCCameraController trong scene.")]
    [SerializeField] private PCCameraController cameraController;

    private void OnEnable()  => Apply(true);
    private void OnDisable() => Apply(false);

    private void Apply(bool menuOpen)
    {
        if (cameraController == null)
            cameraController = FindFirstObjectByType<PCCameraController>(FindObjectsInactive.Include);

        if (cameraController != null)
            cameraController.SetMenuMode(menuOpen);
    }
}
