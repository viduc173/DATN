using UnityEngine;
using EVP;

/// <summary>
/// Tăng tốc tạm thời cho xe Player (hệ EVP <see cref="VehicleController"/>).
///
/// Cơ chế "tốc độ tăng thật" gồm 2 phần, vừa nâng trần tốc độ vừa đẩy xe đạt trần nhanh:
///   1) Nâng <c>maxSpeedForward</c> = base * <see cref="speedMultiplier"/> trong thời gian boost.
///   2) Trong FixedUpdate, cộng lực tiến <see cref="extraAcceleration"/> (ForceMode.Acceleration)
///      cho tới khi vận tốc đạt trần mới → cú "đẩy" có cảm giác nitro, không chỉ nâng trần suông.
/// Hết giờ → khôi phục nguyên trạng <c>maxSpeedForward</c>.
///
/// Component này KHÔNG cần đặt sẵn trong scene: <see cref="GetForActivePlayer"/> tự
/// AddComponent vào xe Player đang active khi nhận bonus đầu tiên. Chỉ có 1 trong 3
/// CarType_* active tại 1 thời điểm nên luôn trỏ đúng xe.
/// </summary>
[RequireComponent(typeof(VehicleController))]
public class SpeedBoost : MonoBehaviour
{
    [Header("Tham số mặc định (có thể override mỗi lần kích hoạt)")]
    [Tooltip("Hệ số nhân trần tốc độ. 1.6 = +60% maxSpeedForward khi boost. <1 sẽ bị kẹp về 1.")]
    public float speedMultiplier = 1.6f;

    [Tooltip("Thời gian boost (giây).")]
    public float duration = 3f;

    [Tooltip("Lực tiến cộng thêm mỗi FixedUpdate khi boost (m/s^2). Tạo cú 'đẩy' tới trần mới.")]
    public float extraAcceleration = 12f;

    [Tooltip("VFX bật/tắt theo trạng thái boost (tuỳ chọn, có thể để trống).")]
    public GameObject boostVfx;

    [Header("Debug")]
    public bool showDebugLog = false;

    // Xe Player đang active duy nhất. Set trong OnEnable, clear trong OnDisable.
    private static SpeedBoost activeInstance;
    public static SpeedBoost ActiveInstance => activeInstance;

    private VehicleController vc;
    private Rigidbody rb;

    private float baseMaxSpeedForward;   // trần tốc độ gốc (cache 1 lần khi bắt đầu boost)
    private float boostedMaxSpeed;       // trần tốc độ khi boost
    private float currentAccel;          // lực đẩy của lần boost hiện tại
    private float boostTimer;            // thời gian còn lại
    private bool boosting;

    private void Awake()
    {
        vc = GetComponent<VehicleController>();
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        // Chỉ xe Player mới là instance active (BonusController cũng tag Player nhưng không có VehicleController).
        if (CompareTag("Player"))
            activeInstance = this;
    }

    private void OnDisable()
    {
        // Xe bị tắt (đổi xe / cuối chặng) → khôi phục trần tốc độ rồi nhả instance.
        if (activeInstance == this)
        {
            RestoreNow();
            activeInstance = null;
        }
    }

    /// <summary>
    /// Trả về SpeedBoost của xe Player đang active; tự AddComponent nếu xe chưa có.
    /// An toàn gọi bất cứ lúc nào (BonusReceiver gọi khi nhận bonus).
    /// </summary>
    public static SpeedBoost GetForActivePlayer()
    {
        if (activeInstance != null && activeInstance.isActiveAndEnabled)
            return activeInstance;

        // FindObjectsByType (Exclude inactive) chỉ trả VehicleController trên GameObject đang active
        // → đúng 1 xe Player đang dùng (AI dùng EasyCarController, không phải EVP nên không lọt vào đây).
        var controllers = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        foreach (var c in controllers)
        {
            if (c == null) continue;
            if (!c.gameObject.activeInHierarchy) continue;
            if (!c.CompareTag("Player")) continue;

            var sb = c.GetComponent<SpeedBoost>();
            if (sb == null)
                sb = c.gameObject.AddComponent<SpeedBoost>(); // OnEnable set activeInstance ngay
            return sb;
        }
        return null;
    }

    /// <summary>Kích hoạt boost với tham số mặc định trên Inspector.</summary>
    public void ActivateBoost() => ActivateBoost(speedMultiplier, duration, extraAcceleration);

    /// <summary>Kích hoạt boost với hệ số + thời gian tuỳ ý (dùng lực đẩy mặc định).</summary>
    public void ActivateBoost(float multiplier, float dur) => ActivateBoost(multiplier, dur, extraAcceleration);

    /// <summary>Kích hoạt boost đầy đủ tham số. Gọi lại khi đang boost = gia hạn (lấy thời gian dài hơn).</summary>
    public void ActivateBoost(float multiplier, float dur, float accel)
    {
        if (vc == null) vc = GetComponent<VehicleController>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (vc == null) return;

        if (multiplier < 1f) multiplier = 1f;   // không bao giờ làm chậm xe
        if (dur <= 0f) dur = duration;

        // Cache trần gốc CHỈ khi chưa boost → tránh nhân chồng hệ số vào base.
        if (!boosting)
            baseMaxSpeedForward = vc.maxSpeedForward;

        currentAccel = accel;
        boostedMaxSpeed = baseMaxSpeedForward * multiplier;
        vc.maxSpeedForward = boostedMaxSpeed;
        boostTimer = Mathf.Max(boostTimer, dur);  // gia hạn: giữ thời gian dài hơn
        boosting = true;

        if (boostVfx != null) boostVfx.SetActive(true);

        if (showDebugLog)
            Debug.Log($"[SpeedBoost] {name}: x{multiplier} maxSpeed {baseMaxSpeedForward:0.0}->{boostedMaxSpeed:0.0} trong {boostTimer:0.0}s");
    }

    private void Update()
    {
        if (!boosting) return;

        boostTimer -= Time.deltaTime;
        if (boostTimer <= 0f)
            RestoreNow();
    }

    private void FixedUpdate()
    {
        if (!boosting || rb == null) return;

        // Đẩy tiến cho tới khi đạt trần mới (không vượt trần → tránh bay lung tung).
        if (rb.linearVelocity.magnitude < boostedMaxSpeed)
            rb.AddRelativeForce(Vector3.forward * currentAccel, ForceMode.Acceleration);
    }

    private void RestoreNow()
    {
        if (!boosting) return;
        boosting = false;
        boostTimer = 0f;

        if (vc != null) vc.maxSpeedForward = baseMaxSpeedForward;
        if (boostVfx != null) boostVfx.SetActive(false);

        if (showDebugLog)
            Debug.Log($"[SpeedBoost] {name}: hết boost, maxSpeed về {baseMaxSpeedForward:0.0}");
    }

    /// <summary>Dừng boost ngay (vd khi kết thúc chặng). Khôi phục trần tốc độ.</summary>
    public void StopBoost() => RestoreNow();
}
