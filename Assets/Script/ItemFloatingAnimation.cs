using UnityEngine;

public class ItemFloatingAnimation : MonoBehaviour
{
    [Header("Float Animation")]
    [Tooltip("Khoảng cách nâng lên/hạ xuống")]
    public float floatAmplitude = 0.5f;
    
    [Tooltip("Tốc độ nâng lên/hạ xuống")]
    public float floatSpeed = 2f;
    
    [Tooltip("Bật/tắt hiệu ứng nâng lên hạ xuống")]
    public bool enableFloating = true;

    [Header("Rotation Animation")]
    [Tooltip("Bật/tắt xoay")]
    public bool enableRotation = true;
    
    [Tooltip("Tốc độ xoay (độ/giây)")]
    public float rotationSpeed = 50f;
    
    [Tooltip("Trục xoay")]
    public Vector3 rotationAxis = Vector3.up;

    [Header("Scale Pulse")]
    [Tooltip("Bật/tắt hiệu ứng phóng to thu nhỏ")]
    public bool enableScalePulse = false;
    
    [Tooltip("Mức độ phóng to/thu nhỏ")]
    public float scaleAmplitude = 0.1f;
    
    [Tooltip("Tốc độ phóng to/thu nhỏ")]
    public float scaleSpeed = 3f;

    private Vector3 startPosition;
    private Vector3 startScale;
    private float timeOffset;

    void Start()
    {
        // Lưu vị trí và scale ban đầu
        startPosition = transform.position;
        startScale = transform.localScale;
        
        // Random offset để các vật phẩm không đồng bộ hoàn toàn
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
    }

    void Update()
    {
        float time = Time.time + timeOffset;

        // Tính toán position mới dựa trên startPosition
        Vector3 newPosition = startPosition;

        // Hiệu ứng nâng lên hạ xuống
        if (enableFloating)
        {
            float floatOffset = Mathf.Sin(time * floatSpeed) * floatAmplitude;
            newPosition.y += floatOffset;
        }

        // Áp dụng position (children sẽ tự động theo)
        transform.position = newPosition;

        // Hiệu ứng xoay
        if (enableRotation)
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
        }

        // Hiệu ứng phóng to thu nhỏ
        if (enableScalePulse)
        {
            float scale = 1f + Mathf.Sin(time * scaleSpeed) * scaleAmplitude;
            transform.localScale = startScale * scale;
        }
    }

    /// <summary>
    /// Bật tất cả hiệu ứng
    /// </summary>
    public void EnableAllEffects()
    {
        enableFloating = true;
        enableRotation = true;
        enableScalePulse = true;
    }

    /// <summary>
    /// Tắt tất cả hiệu ứng
    /// </summary>
    public void DisableAllEffects()
    {
        enableFloating = false;
        enableRotation = false;
        enableScalePulse = false;
        
        // Trả về vị trí và scale ban đầu
        transform.position = startPosition;
        transform.localScale = startScale;
    }

    /// <summary>
    /// Reset vị trí ban đầu (dùng khi di chuyển vật phẩm)
    /// </summary>
    public void ResetStartPosition()
    {
        startPosition = transform.position;
    }

    /// <summary>
    /// Đặt vị trí mới cho vật phẩm
    /// </summary>
    public void SetPosition(Vector3 newPosition)
    {
        startPosition = newPosition;
        transform.position = newPosition;
    }

    // Visualize float range in editor
    void OnDrawGizmosSelected()
    {
        Vector3 pos = Application.isPlaying ? startPosition : transform.position;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos + Vector3.up * floatAmplitude, 0.1f);
        Gizmos.DrawWireSphere(pos - Vector3.up * floatAmplitude, 0.1f);
        Gizmos.DrawLine(pos + Vector3.up * floatAmplitude, pos - Vector3.up * floatAmplitude);
    }
}
