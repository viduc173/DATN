using UnityEngine;
using UnityEngine.Events;
using EVP;

/// <summary>
/// Nitro cho xe Player điều khiển bằng PC. Giữ phím <see cref="nitroKey"/> (mặc định LeftShift)
/// để phun nitro: nâng trần tốc độ + đẩy lực tiến (tái dùng <see cref="SpeedBoost"/> — cùng cơ chế
/// "tăng tốc thật" với bonus pickup), đồng thời bật VFX <see cref="nitroVfx"/> (PlayerController > Nitro).
///
/// Nhiên liệu (charge) có hạn — phun là tụt, nhả là hồi (sau <see cref="rechargeDelay"/>). Phun CẠN thì
/// khoá tới khi hồi đạt <see cref="refireChargeThreshold"/> (vd 50%) mới phun lại được.
///
/// ⭐ NITRO PHỤ THUỘC STAT XE (<see cref="CarStats"/> của loadout đang dùng) — xem <see cref="RecomputeFromStats"/>:
///   • <c>nitro</c> stat (0–100) → dung lượng bình (1.2s..3.2s) + tốc độ hồi (0.02..0.8). Linh kiện cộng
///     <c>statBonus.nitro</c> = "cộng thêm thời gian nitro".
///   • <c>acceleration</c> stat → lực đẩy nitro (nitroAccel): accel cao → đẩy mạnh → đạt tốc độ nitro cao hơn.
///   • Trần tốc độ nitro = <c>maxSpeedForward × nitroMultiplier</c>, mà maxSpeedForward đã scale theo
///     <c>maxSpeed</c> stat → xe nhanh sẵn thì nitro cũng nhanh hơn.
///
/// VFX: object <c>PlayerController > Nitro</c> được <see cref="LoadSceneController"/>/script này gắn vào
/// <c>CarType_* > NitroFx_Anchor</c> của xe đang active, rồi SetActive(true/false) theo trạng thái phun
/// (giống cách "Bonus" được active ra). Không cần đặt VFX sẵn trên xe.
///
/// Đặt component này trên GameObject <c>PlayerController</c> (luôn active). VFX có thể là con tên "Nitro"
/// của chính nó — script tự tìm và tự gắn sang anchor của xe khi vào race.
/// </summary>
[DefaultExecutionOrder(-70)] // sau LoadSceneController(-110)/LevelController(-100): xe player đã active
public class NitroController : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Phím phun nitro (PC). Mặc định Left Shift.")]
    public KeyCode nitroKey = KeyCode.LeftShift;

    // Yêu cầu phun từ script ngoài (vd VR: đẩy 2 tay lên giữ vài giây). Set mỗi frame qua SetExternalFire.
    private bool externalFire;

    /// <summary>
    /// Cho phép script ngoài (VR/AI) yêu cầu phun nitro mà không cần phím <see cref="nitroKey"/>.
    /// Gọi MỖI FRAME: <c>true</c> = muốn phun, <c>false</c> = thôi. Vẫn tuân thủ charge / khoá cạn /
    /// VFX / boost / guard kết thúc chặng giống hệt khi bấm phím.
    /// </summary>
    public void SetExternalFire(bool on) => externalFire = on;

    [Header("VFX (PlayerController > Nitro)")]
    [Tooltip("Object VFX nitro. Để trống → tự tìm con tên 'Nitro' của object này. Sẽ tự gắn vào " +
             "CarType_* > NitroFx_Anchor của xe đang active và bật/tắt theo trạng thái phun.")]
    public Transform nitroVfx;

    [Tooltip("Tên anchor trên xe để gắn VFX nitro.")]
    public string nitroAnchorName = "NitroFx_Anchor";

    [Header("Bình nitro — quy đổi từ stat 'nitro' (0-100)")]
    [Tooltip("Dung lượng bình (giây phun) khi stat nitro = 0 (xe stock).")]
    [Min(0.1f)] public float tankSecondsAtMinStat = 1.2f;

    [Tooltip("Dung lượng bình (giây phun) khi stat nitro = 100 (full linh kiện nitro).")]
    [Min(0.1f)] public float tankSecondsAtMaxStat = 3.2f;

    [Tooltip("Tốc độ hồi (charge/giây) khi stat nitro = 0.")]
    [Min(0f)] public float rechargeAtMinStat = 0.02f;

    [Tooltip("Tốc độ hồi (charge/giây) khi stat nitro = 100.")]
    [Min(0f)] public float rechargeAtMaxStat = 0.8f;

    [Tooltip("Độ trễ sau khi NGỪNG phun mới bắt đầu hồi (giây). Tạo nhịp 'hụt hơi' nhẹ.")]
    [Min(0f)] public float rechargeDelay = 0.5f;

    [Tooltip("Phun CẠN (charge=0) → khoá tới khi hồi lại đạt tỉ lệ này mới phun tiếp được. " +
             "0.5 = phải hồi 50% thanh. 1 = phải đầy. 0 = phun lại ngay khi >0.")]
    [Range(0f, 1f)] public float refireChargeThreshold = 0.5f;

    [Header("Lực đẩy nitro — chủ yếu từ ENGINE (stat acceleration)")]
    [Tooltip("Lực đẩy nitro khi stat acceleration = 0 (m/s^2).")]
    public float nitroAccelAtMinAccel = 4f;

    [Tooltip("Lực đẩy nitro khi stat acceleration = 50 (xe stock). Đây là mức 'base 8' yêu cầu.")]
    public float nitroAccelAtDefAccel = 8f;

    [Tooltip("Lực đẩy nitro khi stat acceleration = 100. Accel cao → đẩy mạnh → đạt tốc độ nitro cao hơn.")]
    public float nitroAccelAtMaxAccel = 16f;

    [Header("Tốc độ nitro — từ MAXSPEED (gián tiếp) + góp nhỏ từ TIRES (grip)")]
    [Tooltip("Hệ số nhân trần tốc độ cơ bản khi phun. 1.125 = +12.5% maxSpeedForward (đã scale theo maxSpeed stat).")]
    public float nitroMultiplier = 1.125f;

    [Tooltip("TIRES (grip) góp thêm vào lực đẩy nitro. = lực cộng thêm khi grip=100 (phần nhỏ so với engine).")]
    public float tirePushBonusAtMaxGrip = 2f;

    [Tooltip("TIRES (grip) góp thêm vào HỆ SỐ tốc độ nitro khi grip=100. 0.05 → x1.125 thành ~x1.175.")]
    public float tireSpeedBonusAtMaxGrip = 0.05f;

    [Header("Events (tuỳ chọn, cho UI thanh nitro)")]
    public UnityEvent onNitroStart;
    public UnityEvent onNitroStop;

    [Header("Debug")]
    public bool showDebugLog = false;

    // Khoảng thời gian boost gia hạn mỗi frame phun. Nhỏ → nhả phím là tự tắt nhanh (đuôi êm).
    private const float kBoostRefresh = 0.2f;

    // Giá trị thực thi — tính từ stat xe trong RecomputeFromStats().
    private float maxCharge = 1.2f;            // dung lượng bình (giây) — car base + ECU (stat 'nitro')
    private float rechargeRate = 0.02f;        // charge/giây — car base + ECU (stat 'nitro')
    private float nitroAccel = 8f;             // lực đẩy — engine (acceleration) + góp nhỏ tires (grip)
    private float effectiveMultiplier = 1.125f; // hệ số tốc độ — base + góp nhỏ tires (grip)

    private float currentCharge;
    private bool firing;
    private bool depletedLock;       // true sau khi phun cạn; mở khi hồi đạt refireChargeThreshold
    private float timeSinceFire;

    private Transform activeCar;       // xe Player đang active
    private Behaviour activeCarInput;   // PlayerDriverInputFromKeyboard của xe (PC) — guard khi kết thúc chặng
    private Behaviour activeCarInputVR; // PlayerInputFromVRController của xe (VR) — VR scene TẮT keyboard input
    private bool vfxAttached;

    /// <summary>Charge còn lại 0..1 (cho UI thanh nitro bind vào).</summary>
    public float ChargeNormalized => maxCharge > 0f ? Mathf.Clamp01(currentCharge / maxCharge) : 0f;
    public bool IsFiring => firing;
    /// <summary>Dung lượng bình hiện tại (giây) sau khi quy đổi từ stat.</summary>
    public float MaxCharge => maxCharge;

    void Start()
    {
        RecomputeFromStats();
        currentCharge = maxCharge;

        if (nitroVfx == null)
            nitroVfx = FindDirectChild(transform, "Nitro");

        if (nitroVfx != null)
            nitroVfx.gameObject.SetActive(false); // ẩn tới khi phun

        TryAttachVfxToCar();

        if (showDebugLog)
            Debug.Log($"[NitroController] start — vfx={(nitroVfx != null ? nitroVfx.name : "NULL")}, charge={currentCharge}");
    }

    void Update()
    {
        ResolveActiveCar();
        if (!vfxAttached) TryAttachVfxToCar();

        bool inputEnabled = IsAnyDriverInputEnabled(); // race end disable input → khoá nitro (xét cả PC lẫn VR)
        bool hasCharge = currentCharge > 0f && !depletedLock;
        bool wantFire = (Input.GetKey(nitroKey) || externalFire) && activeCar != null && inputEnabled && hasCharge;

        if (wantFire)
        {
            if (!firing) { firing = true; onNitroStart?.Invoke(); }

            currentCharge -= Time.deltaTime;
            timeSinceFire = 0f;

            if (currentCharge <= 0f)
            {
                currentCharge = 0f;
                depletedLock = true;   // phun cạn → khoá tới khi hồi đạt ngưỡng
                StopFiring();
            }
        }
        else
        {
            StopFiring();

            // Hồi nhiên liệu sau độ trễ.
            timeSinceFire += Time.deltaTime;
            if (timeSinceFire >= rechargeDelay && currentCharge < maxCharge)
                currentCharge = Mathf.Min(maxCharge, currentCharge + rechargeRate * Time.deltaTime);

            // Mở khoá khi hồi đạt tỉ lệ yêu cầu (vd 50% thanh).
            if (depletedLock && ChargeNormalized >= refireChargeThreshold)
                depletedLock = false;
        }

        if (nitroVfx != null && nitroVfx.gameObject.activeSelf != firing)
            nitroVfx.gameObject.SetActive(firing);

        // Tự nhả yêu cầu ngoài: caller (VR) phải set lại mỗi frame qua SetExternalFire → nếu controller bị
        // tắt/đổi xe giữa chừng thì nitro tự ngắt frame kế tiếp (không bị kẹt 'true').
        externalFire = false;
    }

    void FixedUpdate()
    {
        if (!firing) return;

        // Tái dùng SpeedBoost: nâng trần + đẩy lực (auto-restore sau kBoostRefresh khi ngừng gia hạn).
        var boost = SpeedBoost.GetForActivePlayer();
        boost?.ActivateBoost(effectiveMultiplier, kBoostRefresh, nitroAccel);
    }

    private void StopFiring()
    {
        if (!firing) return;
        firing = false;
        onNitroStop?.Invoke();
    }

    // ---------------------------------------------------------------- stats → nitro

    /// <summary>
    /// Quy đổi stat loadout đang dùng → thông số nitro (ánh xạ theo MÓN ĐỒ bán trong shop):
    ///   • <c>nitro</c> stat  (CAR base + ECU) → maxCharge (bình) + rechargeRate (tốc độ hồi)
    ///   • <c>acceleration</c> (ENGINE)        → nitroAccel (lực đẩy) — chủ lực
    ///   • <c>grip</c> (TIRES/wheels)          → góp NHỎ vào nitroAccel + hệ số tốc độ nitro
    ///   • <c>maxSpeed</c> (gián tiếp)         → trần tốc độ = maxSpeedForward × effectiveMultiplier
    /// Gọi ở Start (loadout cố định cả chặng). Có thể gọi lại nếu loadout đổi runtime.
    /// </summary>
    public void RecomputeFromStats()
    {
        CarStats s = ActiveLoadout.Current != null ? ActiveLoadout.Current.GetEffectiveStats() : null;
        float nitroStat = s != null ? s.nitro : 0f;          // car base + ECU → thời gian
        float accelStat = s != null ? s.acceleration : 50f;  // engine → lực đẩy (base 8 ở stock)
        float gripStat  = s != null ? s.grip : 50f;          // tires → góp nhỏ

        // Thời gian nitro: car base + ECU (stat 'nitro').
        float n = Mathf.Clamp01(nitroStat / 100f);
        maxCharge    = Mathf.Lerp(tankSecondsAtMinStat, tankSecondsAtMaxStat, n);
        rechargeRate = Mathf.Lerp(rechargeAtMinStat,    rechargeAtMaxStat,    n);

        // Lực đẩy: ENGINE (acceleration, piecewise quanh 50=base) + góp NHỎ từ TIRES (grip).
        float g = Mathf.Clamp01(gripStat / 100f);
        nitroAccel = MapStat(accelStat, nitroAccelAtMinAccel, nitroAccelAtDefAccel, nitroAccelAtMaxAccel)
                   + tirePushBonusAtMaxGrip * g;

        // Tốc độ nitro: base + góp NHỎ từ TIRES (grip). Trần thật = maxSpeedForward(maxSpeed stat) × cái này.
        effectiveMultiplier = nitroMultiplier + tireSpeedBonusAtMaxGrip * g;

        if (showDebugLog)
            Debug.Log($"[NitroController] stats→nitro: nitro={nitroStat:0} accel={accelStat:0} grip={gripStat:0} " +
                      $"⇒ bình={maxCharge:0.00}s hồi={rechargeRate:0.000}/s đẩy={nitroAccel:0.0} xMult={effectiveMultiplier:0.000}");
    }

    /// <summary>Piecewise lerp: 0..50 → min..def, 50..100 → def..max (50 = giá trị base).</summary>
    private static float MapStat(float v, float min, float def, float max)
    {
        v = Mathf.Clamp(v, 0f, 100f);
        return v <= 50f ? Mathf.Lerp(min, def, v / 50f)
                        : Mathf.Lerp(def, max, (v - 50f) / 50f);
    }

    // ---------------------------------------------------------------- car resolve

    private void ResolveActiveCar()
    {
        if (activeCar != null && activeCar.gameObject.activeInHierarchy)
            return;

        // Giống SpeedBoost: VehicleController active + tag Player = đúng xe đang dùng (AI là EasyCarController).
        var controllers = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        foreach (var c in controllers)
        {
            if (c == null || !c.gameObject.activeInHierarchy || !c.CompareTag("Player")) continue;
            activeCar = c.transform;
            activeCarInput = c.GetComponentInChildren<PlayerDriverInputFromKeyboard>(true);
            activeCarInputVR = c.GetComponentInChildren<PlayerInputFromVRController>(true);
            vfxAttached = false; // xe mới → gắn lại VFX
            return;
        }
        activeCar = null;
        activeCarInput = null;
        activeCarInputVR = null;
    }

    // Input lái còn bật không? VR scene TẮT keyboard input (lái bằng PlayerInputFromVRController), nên nếu chỉ
    // xét keyboard sẽ khoá nitro vĩnh viễn. Còn ÍT NHẤT 1 input (PC hoặc VR) đang bật → cho phép nitro.
    // Không tìm thấy input nào → vẫn cho phép (không chặn nhầm). Kết thúc chặng disable cả 2 → khoá lại.
    private bool IsAnyDriverInputEnabled()
    {
        bool hasAny = false;
        if (activeCarInput != null)   { hasAny = true; if (activeCarInput.enabled)   return true; }
        if (activeCarInputVR != null) { hasAny = true; if (activeCarInputVR.enabled) return true; }
        return !hasAny;
    }

    private void TryAttachVfxToCar()
    {
        if (nitroVfx == null || activeCar == null) return;

        Transform anchor = FindChildRecursive(activeCar, nitroAnchorName);
        if (anchor == null)
        {
            if (showDebugLog) Debug.LogWarning($"[NitroController] Không thấy '{nitroAnchorName}' trên xe '{activeCar.name}'.");
            return;
        }

        nitroVfx.SetParent(anchor, false);
        nitroVfx.localPosition = Vector3.zero;
        nitroVfx.localRotation = Quaternion.identity;
        nitroVfx.localScale = Vector3.one;
        nitroVfx.gameObject.SetActive(false);
        vfxAttached = true;

        if (showDebugLog)
            Debug.Log($"[NitroController] Gắn VFX nitro vào {activeCar.name}/{nitroAnchorName}.");
    }

    // ---------------------------------------------------------------- helpers

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == childName) return parent.GetChild(i);
        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null) return null;
        if (parent.name == childName) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var r = FindChildRecursive(parent.GetChild(i), childName);
            if (r != null) return r;
        }
        return null;
    }
}
