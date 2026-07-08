using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Component đặt trên xe để nhận và xử lý bonus
/// </summary>
public class BonusReceiver : MonoBehaviour
{
    [System.Serializable]
    public class BonusReceivedEvent : UnityEvent<BonusEvent.BonusData> { }

    [Header("Check Settings")]
    [Tooltip("Tần suất check bonus (giây). Càng nhỏ càng nhanh nhưng tốn hiệu năng hơn.")]
    public float checkInterval = 0.1f;

    [Header("Events")]
    [Tooltip("Event được gọi khi nhận bonus. Có thể dùng để hiển thị UI, sound effect, v.v.")]
    public BonusReceivedEvent onBonusReceived = new BonusReceivedEvent();

    [Header("Debug")]
    public bool showDebugLog = true;
    [SerializeField]
    private int totalBonusReceived = 0;
    [SerializeField]
    private bool isCoroutineRunning = false;

    // Active bonuses (bonus có duration)
    private List<ActiveBonus> activeBonuses = new List<ActiveBonus>();

    private class ActiveBonus
    {
        public BonusEvent.BonusData data;
        public float timeRemaining;
        
        public ActiveBonus(BonusEvent.BonusData bonusData)
        {
            data = bonusData;
            timeRemaining = bonusData.duration;
        }
    }

    void Start()
    {
        // Debug check
        if (showDebugLog)
        {
            Debug.Log($"[{gameObject.name}] BonusReceiver Started");
            
            if (BonusEvent.Instance == null)
            {
                Debug.LogError($"[{gameObject.name}] BonusEvent.Instance is NULL! Make sure BonusEvent exists in scene.");
            }
            else
            {
                Debug.Log($"[{gameObject.name}] BonusEvent.Instance found successfully");
            }
        }
        isCoroutineRunning = true;
    }

    void OnDisable()
    {
        isCoroutineRunning = false;
        if (showDebugLog)
            Debug.Log($"[{gameObject.name}] BonusReceiver Disabled");
    }

    void OnEnable()
    {
        if (showDebugLog)
            Debug.Log($"[{gameObject.name}] BonusReceiver Enabled");
        
        StartCoroutine(CheckForBonusRoutine());
    }

    void Update()
    {
        // Update active bonuses với duration
        for (int i = activeBonuses.Count - 1; i >= 0; i--)
        {
            activeBonuses[i].timeRemaining -= Time.deltaTime;
            
            if (activeBonuses[i].timeRemaining <= 0)
            {
                OnBonusExpired(activeBonuses[i].data);
                activeBonuses.RemoveAt(i);
            }
        }
    }

    IEnumerator CheckForBonusRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            // Safety check - nếu Instance bị null thì dừng coroutine
            if (BonusEvent.Instance == null)
            {
                if (showDebugLog)
                    Debug.LogError($"[{gameObject.name}] BonusEvent.Instance is NULL during check! Stopping coroutine.");
                yield break;
            }

            // Check xem có bonus nào đang chờ không
            if (BonusEvent.Instance.HasPendingBonus())
            {
                BonusEvent.BonusData bonus = BonusEvent.Instance.GetNextBonus();
                
                if (bonus != null)
                {
                    ProcessBonus(bonus);
                }
            }
        }
    }

    /// <summary>
    /// Xử lý bonus nhận được
    /// </summary>
    void ProcessBonus(BonusEvent.BonusData bonus)
    {
        totalBonusReceived++;

        if (showDebugLog)
            Debug.Log($"[{gameObject.name}] Received Bonus: Type={bonus.bonusType}, Value={bonus.bonusValue}, Duration={bonus.duration}s");

        // Nếu bonus có duration, thêm vào list active
        if (bonus.duration > 0)
        {
            activeBonuses.Add(new ActiveBonus(bonus));
        }

        // Gọi event để các component khác xử lý
        onBonusReceived?.Invoke(bonus);

        // Có thể xử lý các loại bonus cụ thể ở đây
        HandleBonusType(bonus);
    }

    /// <summary>
    /// Xử lý các loại bonus cụ thể
    /// </summary>
    void HandleBonusType(BonusEvent.BonusData bonus)
    {
        switch (bonus.bonusType.ToLower())
        {
            case "speed":
                HandleSpeedBonus(bonus);
                break;
            case "coin":
                HandleCoinBonus(bonus);
                break;
            case "shield":
                HandleShieldBonus(bonus);
                break;
            default:
                if (showDebugLog)
                    Debug.Log($"Unhandled bonus type: {bonus.bonusType}");
                break;
        }
    }

    void HandleSpeedBonus(BonusEvent.BonusData bonus)
    {
        // bonusValue = hệ số nhân trần tốc độ (vd 1.6 = +60%), duration = thời gian boost.
        float multiplier = bonus.bonusValue;
        float dur = bonus.duration > 0f ? bonus.duration : 3f;

        // Tìm (hoặc tự gắn) SpeedBoost trên xe Player đang active rồi kích hoạt.
        SpeedBoost boost = SpeedBoost.GetForActivePlayer();
        if (boost != null)
        {
            boost.ActivateBoost(multiplier, dur);
            if (showDebugLog)
                Debug.Log($"Speed boost x{multiplier} áp lên '{boost.name}' trong {dur}s");
        }
        else if (showDebugLog)
        {
            Debug.LogWarning("[BonusReceiver] Không tìm thấy xe Player active để boost.");
        }
    }

    void HandleCoinBonus(BonusEvent.BonusData bonus)
    {
        // Example: thêm coin
        if (showDebugLog)
            Debug.Log($"Coins collected: +{bonus.bonusValue}");
        
        // TODO: Implement thêm coin vào player
    }

    void HandleShieldBonus(BonusEvent.BonusData bonus)
    {
        // Example: kích hoạt shield
        if (showDebugLog)
            Debug.Log($"Shield activated for {bonus.duration}s");
        
        // TODO: Implement shield
    }

    /// <summary>
    /// Được gọi khi bonus hết hạn
    /// </summary>
    void OnBonusExpired(BonusEvent.BonusData bonus)
    {
        if (showDebugLog)
            Debug.Log($"Bonus expired: Type={bonus.bonusType}");

        // TODO: Xử lý khi bonus hết hạn (remove buff, v.v.)
    }

    /// <summary>
    /// Kiểm tra xem có bonus loại nào đang active không
    /// </summary>
    public bool HasActiveBonus(string bonusType)
    {
        foreach (var bonus in activeBonuses)
        {
            if (bonus.data.bonusType.Equals(bonusType, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Lấy danh sách tất cả bonus đang active
    /// </summary>
    public List<BonusEvent.BonusData> GetActiveBonuses()
    {
        List<BonusEvent.BonusData> result = new List<BonusEvent.BonusData>();
        foreach (var bonus in activeBonuses)
        {
            result.Add(bonus.data);
        }
        return result;
    }
}
