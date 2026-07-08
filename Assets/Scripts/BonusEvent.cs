using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Quản lý hệ thống bonus event toàn cục
/// </summary>
public class BonusEvent : MonoBehaviour
{
    private static BonusEvent instance;
    public static BonusEvent Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("BonusEvent");
                instance = go.AddComponent<BonusEvent>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    /// <summary>
    /// Hàm static để các script khác gọi trực tiếp thêm bonus event
    /// </summary>
    public static void AddBonusEvent(string bonusType, float bonusValue, float duration = 0f, Vector3 position = default, GameObject triggerObject = null)
    {
        Instance.TriggerBonus(bonusType, bonusValue, duration, position, triggerObject);
    }

    [Header("Bonus mặc định (dùng cho TriggerBonusUnityEvent wire trong Inspector)")]
    [Tooltip("Loại bonus. 'speed' → BonusReceiver áp tăng tốc lên xe Player active.")]
    public string defaultBonusType = "speed";
    [Tooltip("Giá trị bonus. Với 'speed' = hệ số nhân trần tốc độ (1.6 = +60%).")]
    public float defaultBonusValue = 1.6f;
    [Tooltip("Thời gian hiệu lực (giây). 0 = tức thì (speed nên > 0).")]
    public float defaultBonusDuration = 3f;

    /// <summary>
    /// Hàm public để UnityEvent trong Inspector có thể gọi (không cần tham số).
    /// Các bonus pickup (PlayerTriggerEvent.onPlayerEnter) gọi hàm này → đẩy bonus mặc định
    /// (mặc định là 'speed') vào queue cho BonusReceiver xử lý.
    /// </summary>
    public void TriggerBonusUnityEvent()
    {
        TriggerBonus(defaultBonusType, defaultBonusValue, defaultBonusDuration, transform.position, gameObject);
    }

    [System.Serializable]
    public class BonusData
    {
        public string bonusType;        // Loại bonus (speed, shield, coin, etc.)
        public float bonusValue;        // Giá trị bonus
        public float duration;          // Thời gian hiệu lực (0 = tức thì)
        public Vector3 position;        // Vị trí bonus được trigger
        public GameObject triggerObject; // Object đã trigger bonus

        public BonusData(string type, float value, float dur = 0f, Vector3 pos = default, GameObject obj = null)
        {
            bonusType = type;
            bonusValue = value;
            duration = dur;
            position = pos;
            triggerObject = obj;
        }
    }

    // Queue chứa các bonus đang chờ được nhận
    private Queue<BonusData> bonusQueue = new Queue<BonusData>();
    private object queueLock = new object();

    [Header("Debug")]
    [Tooltip("Hiển thị log debug")]
    public bool showDebugLog = true;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Thêm bonus vào hệ thống
    /// </summary>
    public void TriggerBonus(string bonusType, float bonusValue, float duration = 0f, Vector3 position = default, GameObject triggerObject = null)
    {
        if (bonusValue <= 0)
        {
            if (showDebugLog)
                Debug.LogWarning($"Bonus value phải > 0. Nhận được: {bonusValue}");
            return;
        }

        BonusData bonus = new BonusData(bonusType, bonusValue, duration, position, triggerObject);
        
        lock (queueLock)
        {
            bonusQueue.Enqueue(bonus);
        }

        if (showDebugLog)
            Debug.Log($"Bonus triggered: Type={bonusType}, Value={bonusValue}, Duration={duration}s");
    }

    /// <summary>
    /// Lấy bonus tiếp theo từ queue (trả về null nếu không có)
    /// </summary>
    public BonusData GetNextBonus()
    {
        lock (queueLock)
        {
            if (bonusQueue.Count > 0)
            {
                BonusData bonus = bonusQueue.Dequeue();
                
                if (showDebugLog)
                    Debug.Log($"Bonus consumed: Type={bonus.bonusType}, Value={bonus.bonusValue}");
                
                return bonus;
            }
        }
        return null;
    }

    /// <summary>
    /// Kiểm tra có bonus nào đang chờ không
    /// </summary>
    public bool HasPendingBonus()
    {
        lock (queueLock)
        {
            return bonusQueue.Count > 0;
        }
    }

    /// <summary>
    /// Lấy số lượng bonus đang chờ
    /// </summary>
    public int GetPendingBonusCount()
    {
        lock (queueLock)
        {
            return bonusQueue.Count;
        }
    }

    /// <summary>
    /// Xóa tất cả bonus đang chờ
    /// </summary>
    public void ClearAllBonus()
    {
        lock (queueLock)
        {
            bonusQueue.Clear();
        }
        
        if (showDebugLog)
            Debug.Log("All bonus cleared");
    }
}
