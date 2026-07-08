using UnityEngine;

public class SystemController : MonoBehaviour
{
    public static SystemController Instance { get; private set; }

    [Header("Car Selection")]
    [Tooltip("Số hiệu xe được chọn (0, 1, 2, 3, ...)")]
    public int selectedCarIndex = 0;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Set số hiệu xe được chọn
    /// </summary>
    public void SetSelectedCar(int carIndex)
    {
        selectedCarIndex = carIndex;
        Debug.Log($"Selected car index: {carIndex}");
    }

    /// <summary>
    /// Lấy số hiệu xe hiện tại
    /// </summary>
    public int GetSelectedCarIndex()
    {
        return selectedCarIndex;
    }
}
