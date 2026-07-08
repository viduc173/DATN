using UnityEngine;

/// <summary>
/// Component đặt trên object bonus (coin, powerup, v.v.) để trigger bonus khi va chạm
/// </summary>
[RequireComponent(typeof(Collider))]
public class BonusTrigger : MonoBehaviour
{
    [Header("Bonus Settings")]
    [Tooltip("Loại bonus (speed, shield, coin, etc.)")]
    public string bonusType = "coin";
    
    [Tooltip("Giá trị bonus")]
    public float bonusValue = 10f;
    
    [Tooltip("Thời gian hiệu lực (0 = tức thì, ngay lập tức)")]
    public float bonusDuration = 0f;

    [Header("Trigger Settings")]
    [Tooltip("Tag của object có thể nhận bonus (Player, Vehicle, v.v.)")]
    public string targetTag = "Player";
    
    [Tooltip("Tự động destroy object sau khi trigger")]
    public bool destroyAfterTrigger = true;
    
    [Tooltip("Delay trước khi destroy (giây)")]
    public float destroyDelay = 0f;

    [Header("Visual Effects")]
    [Tooltip("Particle effect khi trigger (optional)")]
    public GameObject collectEffect;
    
    [Tooltip("Audio clip khi trigger (optional)")]
    public AudioClip collectSound;

    [Header("Debug")]
    public bool showDebugLog = false;

    private bool hasTriggered = false;

    void Start()
    {
        // Đảm bảo collider là trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"[{gameObject.name}] Collider should be set as Trigger. Auto-fixing...");
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        // Kiểm tra tag
        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag))
            return;

        // Kiểm tra có BonusReceiver không (optional)
        BonusReceiver receiver = other.GetComponent<BonusReceiver>();
        if (receiver == null)
            receiver = other.GetComponentInParent<BonusReceiver>();

        if (receiver == null)
        {
            if (showDebugLog)
                Debug.LogWarning($"Object {other.name} không có BonusReceiver component!");
            return;
        }

        TriggerBonus();
    }

    /// <summary>
    /// Trigger bonus (có thể gọi từ bên ngoài)
    /// </summary>
    public void TriggerBonus()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        if (showDebugLog)
            Debug.Log($"[{gameObject.name}] Triggering bonus: Type={bonusType}, Value={bonusValue}, Duration={bonusDuration}");

        // Gửi bonus vào hệ thống
        BonusEvent.Instance.TriggerBonus(bonusType, bonusValue, bonusDuration, transform.position, gameObject);

        // Visual effects
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        // Sound effect
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        // Destroy object nếu cần
        if (destroyAfterTrigger)
        {
            if (destroyDelay > 0)
                Destroy(gameObject, destroyDelay);
            else
                Destroy(gameObject);
        }
    }

    /// <summary>
    /// Reset trigger để có thể trigger lại
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    void OnDrawGizmos()
    {
        // Vẽ icon trong Scene view để dễ nhìn
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
