using UnityEngine;

/// <summary>
/// Quản lý việc cầm nắm vật thể của PC Player, đảm bảo chỉ có thể cầm 1 vật thể cùng lúc.
/// </summary>
public class PCInteractorManager : MonoBehaviour
{
    public static PCInteractorManager Instance { get; private set; }

    [SerializeField, Tooltip("Vật thể hiện đang được cầm trên tay.")]
    private PCInteractorObject _currentlyHeldObject;

    public PCInteractorObject CurrentlyHeldObject => _currentlyHeldObject;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Trả về true nếu player có thể cầm vật thể mới (tức là tay đang không cầm gì).
    /// </summary>
    public bool CanPickup()
    {
        return _currentlyHeldObject == null;
    }

    /// <summary>
    /// Đăng ký vật thể đang cầm. Nếu tay đang cầm vật khác thì không cho phép.
    /// </summary>
    public bool TryRegisterHeldObject(PCInteractorObject interactObj)
    {
        if (_currentlyHeldObject != null && _currentlyHeldObject != interactObj)
        {
            // Đã cầm vật khác, không cho cầm thêm
            return false;
        }
        _currentlyHeldObject = interactObj;
        return true;
    }

    /// <summary>
    /// Hủy đăng ký vật thể khi thả ra.
    /// </summary>
    public void UnregisterHeldObject(PCInteractorObject interactObj)
    {
        if (_currentlyHeldObject == interactObj)
        {
            _currentlyHeldObject = null;
        }
    }
}
