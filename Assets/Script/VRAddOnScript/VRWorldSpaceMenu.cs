using UnityEngine;

/// <summary>
/// Đặt một World-Space Canvas vào đúng tầm mắt người chơi VR và (tùy chọn) BÁM THEO khi quay đầu.
///   - Cách mặt người chơi một khoảng cố định <see cref="distance"/> (m), ngang tầm mắt.
///   - ORBIT: khi người chơi nhìn đi hướng khác, panel xoay vòng quanh đầu (giữ nguyên bán kính)
///     để luôn nằm trước mặt — mượt, có vùng chết (deadzone) chống rung/say VR.
///   - Luôn quay mặt về phía người chơi (billboard).
///   - Tự scale theo khoảng cách để UI luôn chiếm cùng một góc nhìn ("vừa tầm mắt").
///
/// Tái sử dụng: gắn lên GameObject GỐC của World-Space Canvas. Xem README_GarageLobby_VR.md.
/// </summary>
[DisallowMultipleComponent]
public class VRWorldSpaceMenu : MonoBehaviour
{
    public enum FollowMode
    {
        /// <summary>Đặt 1 lần khi bật lên rồi neo cố định. Hợp với panel tĩnh.</summary>
        PlaceOnEnable,
        /// <summary>Bám theo hướng nhìn kiểu xoay vòng quanh đầu (orbit). Hợp với menu chính.</summary>
        OrbitFollow
    }

    [Header("Camera")]
    [Tooltip("Camera đầu VR. Để trống = tự lấy Camera.main.")]
    [SerializeField] private Camera targetCamera;

    [Header("Vị trí đặt")]
    [Tooltip("Khoảng cách (m) panel cách mặt người chơi. 1.2–2.0 là thoải mái.")]
    [SerializeField] private float distance = 1.5f;
    [Tooltip("Lệch dọc so với tầm mắt (m). Âm = thấp hơn đường nhìn cho đỡ mỏi cổ.")]
    [SerializeField] private float verticalOffset = -0.1f;
    [Tooltip("PlaceOnEnable: đặt 1 lần. OrbitFollow: xoay vòng bám theo hướng nhìn.")]
    [SerializeField] private FollowMode followMode = FollowMode.OrbitFollow;

    [Header("Orbit Follow (mượt + vùng chết)")]
    [Tooltip("Người chơi quay lệch khỏi panel quá ngưỡng này (độ) thì panel mới bắt đầu xoay theo.")]
    [SerializeField] private float deadzoneAngle = 8f;
    [Tooltip("Panel xoay theo cho tới khi lệch nhỏ hơn ngưỡng này (độ) thì dừng — chống rung (hysteresis).")]
    [SerializeField] private float settleAngle = 2f;
    [Tooltip("Tốc độ xoay vòng (orbit) bám theo. Cao = bám nhanh.")]
    [SerializeField] private float orbitSmoothing = 4f;
    [Tooltip("Tốc độ bám vị trí (khi người chơi đi lại / cúi ngẩng).")]
    [SerializeField] private float positionSmoothing = 6f;

    [Header("Hướng quay (Billboard)")]
    [Tooltip("Giữ panel thẳng đứng (chỉ xoay trục Y). Tắt = panel ngửa/cúi theo đầu.")]
    [SerializeField] private bool keepUpright = true;

    [Header("Scale theo khoảng cách (giữ kích thước thị giác không đổi)")]
    [Tooltip("Bật: canvas tự scale để luôn trông cùng một cỡ dù gần hay xa.")]
    [SerializeField] private bool constantAngularSize = true;
    [Tooltip("localScale 'vừa mắt' tại referenceDistance. World-canvas thường ~0.0005–0.002. Chỉnh số này nếu panel to/nhỏ.")]
    [SerializeField] private float baseScale = 0.0006f;
    [Tooltip("Khoảng cách (m) mà baseScale được canh đẹp. Thường = distance.")]
    [SerializeField] private float referenceDistance = 1.5f;

    private Transform _cam;
    private bool _following;

    private void OnEnable()
    {
        ResolveCamera();
        _following = false;
        SnapInFront();   // bật lên là hiện ngay trước mặt
    }

    private void LateUpdate()
    {
        ResolveCamera();   // mỗi frame: bám camera ĐANG active (scene đua có nhiều Main Camera theo từng xe;
                           // xe đổi / load xong → camera active đổi → phải đổi theo, đừng giữ camera xe đã tắt)
        if (_cam == null) return;
        if (followMode == FollowMode.OrbitFollow) OrbitFollow();
    }

    /// <summary>Kéo menu về đúng trước mặt người chơi (gọi khi mở menu / từ nút bấm).</summary>
    public void Recenter() { ResolveCamera(); _following = false; SnapInFront(); }

    private void ResolveCamera()
    {
        // Giữ camera hiện tại NẾU còn active+enabled; nếu không (vd xe của nó đã bị tắt khi đổi xe / load xong)
        // → chọn lại camera ĐANG active. Quan trọng: phải so theo isActiveAndEnabled, KHÔNG phải == null —
        // Transform của camera xe đã tắt vẫn != null (chỉ inactive) nên nếu chỉ check null sẽ kẹt camera sai.
        if (targetCamera == null || !targetCamera.isActiveAndEnabled)
            targetCamera = PickActiveCamera();
        _cam = targetCamera != null ? targetCamera.transform : null;
    }

    /// <summary>
    /// Camera ĐANG ACTIVE của người chơi. Scene đua có nhiều Main Camera (mỗi rig "XR Inside Car &amp; Input"
    /// của từng CarType một cái) nhưng <c>LoadSceneController</c> chỉ bật xe đang dùng → chỉ 1 camera active.
    /// <c>Camera.main</c> đôi khi trả nhầm khi nhiều object cùng tag MainCamera, nên lọc lại theo active+enabled.
    /// </summary>
    private static Camera PickActiveCamera()
    {
        Camera main = Camera.main;                  // đã lọc: enabled + GO active + tag MainCamera
        if (main != null && main.isActiveAndEnabled) return main;
        foreach (var c in Camera.allCameras)        // fallback: bất kỳ camera enabled + GO active
            if (c != null && c.isActiveAndEnabled) return c;
        return null;
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude < 1e-5f ? Vector3.forward : v.normalized;
    }

    private void SnapInFront()
    {
        if (_cam == null) return;
        Vector3 dir = Flat(_cam.forward);
        transform.position = _cam.position + dir * distance + Vector3.up * verticalOffset;
        FacePlayer();
        ApplyScale();
    }

    private void OrbitFollow()
    {
        Vector3 head = _cam.position;
        Vector3 gaze = Flat(_cam.forward);
        Vector3 curDir = Flat(transform.position - head);
        float ang = Vector3.Angle(curDir, gaze);

        // Hysteresis: bắt đầu xoay khi lệch > deadzone, xoay tới khi < settle thì dừng.
        if (!_following && ang > deadzoneAngle) _following = true;
        else if (_following && ang < settleAngle) _following = false;

        Vector3 dir = _following
            ? Vector3.Slerp(curDir, gaze, orbitSmoothing * Time.deltaTime)
            : curDir;

        Vector3 target = head + dir * distance + Vector3.up * verticalOffset;
        transform.position = Vector3.Lerp(transform.position, target, positionSmoothing * Time.deltaTime);

        FacePlayer();
        ApplyScale();
    }

    private void FacePlayer()
    {
        Vector3 dir = transform.position - _cam.position;
        if (keepUpright) dir.y = 0f;
        if (dir.sqrMagnitude < 1e-5f) return;
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    private void ApplyScale()
    {
        if (!constantAngularSize) return;
        float d = Vector3.Distance(transform.position, _cam.position);
        float s = baseScale * (d / Mathf.Max(0.01f, referenceDistance));
        transform.localScale = new Vector3(s, s, s);
    }
}
