using UnityEngine;
using UnityEngine.Events;

public class PlayerTriggerEvent : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("Tag để kiểm tra (mặc định: Player)")]
    public string targetTag = "Player";
    
    [Tooltip("Chỉ trigger một lần rồi tự hủy")]
    public bool triggerOnce = false;
    
    [Tooltip("Delay trước khi gọi event (giây)")]
    public float eventDelay = 0f;
    
    [Tooltip("Có thể trigger lại sau khi exit")]
    public bool canRetrigger = true;

    [Header("Events")]
    [Tooltip("Event được gọi khi player vào trigger")]
    public UnityEvent onPlayerEnter;
    
    [Tooltip("Event được gọi khi player thoát khỏi trigger")]
    public UnityEvent onPlayerExit;
    
    [Tooltip("Event được gọi khi player đang trong trigger (mỗi frame)")]
    public UnityEvent onPlayerStay;

    [Header("Debug")]
    [Tooltip("Hiển thị debug messages")]
    public bool showDebugLogs = false;

    private bool hasTriggered = false;
    private bool isPlayerInside = false;

    void OnTriggerEnter(Collider other)
    {
        // Kiểm tra tag
        if (!other.CompareTag(targetTag))
            return;

        // Kiểm tra đã trigger chưa
        if (triggerOnce && hasTriggered)
            return;

        // Kiểm tra có thể trigger lại không
        if (!canRetrigger && hasTriggered)
            return;

        hasTriggered = true;
        isPlayerInside = true;

        if (showDebugLogs)
            Debug.Log($"Player entered trigger: {gameObject.name}");

        // Gọi event với delay
        if (eventDelay > 0)
        {
            Invoke(nameof(InvokeEnterEvent), eventDelay);
        }
        else
        {
            InvokeEnterEvent();
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Kiểm tra tag
        if (!other.CompareTag(targetTag))
            return;

        isPlayerInside = false;

        if (showDebugLogs)
            Debug.Log($"Player exited trigger: {gameObject.name}");

        // Gọi exit event
        onPlayerExit?.Invoke();

        // Reset nếu có thể trigger lại
        if (canRetrigger && !triggerOnce)
        {
            hasTriggered = false;
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Kiểm tra tag
        if (!other.CompareTag(targetTag))
            return;

        if (isPlayerInside)
        {
            // Gọi stay event
            onPlayerStay?.Invoke();
        }
    }

    private void InvokeEnterEvent()
    {
        onPlayerEnter?.Invoke();

        // Tự hủy nếu chỉ trigger một lần
        if (triggerOnce)
        {
            if (showDebugLogs)
                Debug.Log($"Destroying trigger object: {gameObject.name}");
            
            Destroy(gameObject, 0.1f);
        }
    }

    /// <summary>
    /// Reset trigger để có thể kích hoạt lại
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
        isPlayerInside = false;
        
        if (showDebugLogs)
            Debug.Log($"Trigger reset: {gameObject.name}");
    }

    /// <summary>
    /// Vô hiệu hóa trigger
    /// </summary>
    public void DisableTrigger()
    {
        enabled = false;
        
        if (showDebugLogs)
            Debug.Log($"Trigger disabled: {gameObject.name}");
    }

    /// <summary>
    /// Kích hoạt trigger
    /// </summary>
    public void EnableTrigger()
    {
        enabled = true;
        
        if (showDebugLogs)
            Debug.Log($"Trigger enabled: {gameObject.name}");
    }

    // Hiển thị trigger zone trong editor
    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && col.isTrigger)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
            }
        }
    }
}
