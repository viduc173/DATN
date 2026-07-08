using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Component để chuyển scene trong Unity
/// Có thể trigger bằng code hoặc UnityEvent
/// </summary>
public class SceneChanger : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Tên của scene cần chuyển đến")]
    public string targetSceneName = "";
    
    [Tooltip("Index của scene cần chuyển đến (ưu tiên hơn tên nếu > 0)")]
    public int targetSceneIndex = -1;
    
    [Tooltip("Chế độ load scene")]
    public LoadSceneMode loadMode = LoadSceneMode.Single;
    
    [Header("Options")]
    [Tooltip("Tự động chuyển scene khi component được enable")]
    public bool autoLoadOnEnable = false;
    
    [Tooltip("Tự động chuyển scene khi component được start")]
    public bool autoLoadOnStart = false;
    
    [Tooltip("Delay trước khi chuyển scene (giây)")]
    public float delayBeforeLoad = 0f;


    void OnEnable()
    {
        if (autoLoadOnEnable)
        {
            LoadSceneWithDelay();
        }
    }


    void Start()
    {
        if (autoLoadOnStart)
        {
            LoadSceneWithDelay();
        }
    }


    /// <summary>
    /// Chuyển scene ngay lập tức
    /// </summary>
    public void LoadScene()
    {
        LoadSceneInternal();
    }


    /// <summary>
    /// Chuyển scene với delay
    /// </summary>
    public void LoadSceneWithDelay()
    {
        if (delayBeforeLoad > 0f)
        {
            Invoke(nameof(LoadSceneInternal), delayBeforeLoad);
        }
        else
        {
            LoadSceneInternal();
        }
    }


    /// <summary>
    /// Chuyển scene với delay tùy chỉnh
    /// </summary>
    public void LoadSceneWithDelay(float customDelay)
    {
        if (customDelay > 0f)
        {
            Invoke(nameof(LoadSceneInternal), customDelay);
        }
        else
        {
            LoadSceneInternal();
        }
    }


    /// <summary>
    /// Chuyển scene theo tên
    /// </summary>
    public void LoadSceneByName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName, loadMode);
            Debug.Log($"[SceneChanger] Loading scene: {sceneName}");
        }
        else
        {
            Debug.LogError("[SceneChanger] Scene name is empty!");
        }
    }


    /// <summary>
    /// Chuyển scene theo index
    /// </summary>
    public void LoadSceneByIndex(int sceneIndex)
    {
        if (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(sceneIndex, loadMode);
            Debug.Log($"[SceneChanger] Loading scene index: {sceneIndex}");
        }
        else
        {
            Debug.LogError($"[SceneChanger] Invalid scene index: {sceneIndex}");
        }
    }


    /// <summary>
    /// Reload scene hiện tại
    /// </summary>
    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Debug.Log("[SceneChanger] Reloading current scene");
    }


    /// <summary>
    /// Load scene tiếp theo trong build settings
    /// </summary>
    public void LoadNextScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;
        
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex, loadMode);
            Debug.Log($"[SceneChanger] Loading next scene: {nextIndex}");
        }
        else
        {
            Debug.LogWarning("[SceneChanger] No next scene available");
        }
    }


    /// <summary>
    /// Load scene trước đó trong build settings
    /// </summary>
    public void LoadPreviousScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int prevIndex = currentIndex - 1;
        
        if (prevIndex >= 0)
        {
            SceneManager.LoadScene(prevIndex, loadMode);
            Debug.Log($"[SceneChanger] Loading previous scene: {prevIndex}");
        }
        else
        {
            Debug.LogWarning("[SceneChanger] No previous scene available");
        }
    }


    /// <summary>
    /// Thực hiện load scene (internal method)
    /// </summary>
    private void LoadSceneInternal()
    {
        if (targetSceneIndex >= 0)
        {
            // Ưu tiên load theo index
            LoadSceneByIndex(targetSceneIndex);
        }
        else if (!string.IsNullOrEmpty(targetSceneName))
        {
            // Load theo tên
            LoadSceneByName(targetSceneName);
        }
        else
        {
            Debug.LogError("[SceneChanger] No target scene specified!");
        }
    }


    /// <summary>
    /// Lấy tên scene hiện tại
    /// </summary>
    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }


    /// <summary>
    /// Lấy index scene hiện tại
    /// </summary>
    public int GetCurrentSceneIndex()
    {
        return SceneManager.GetActiveScene().buildIndex;
    }


    /// <summary>
    /// Kiểm tra xem scene có tồn tại trong build settings không
    /// </summary>
    public bool SceneExists(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneManager.GetSceneByBuildIndex(i).path;
            string sceneFileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            if (sceneFileName == sceneName)
            {
                return true;
            }
        }
        return false;
    }


    /// <summary>
    /// Lấy danh sách tất cả scene trong build settings
    /// </summary>
    public string[] GetAllScenesInBuild()
    {
        string[] sceneNames = new string[SceneManager.sceneCountInBuildSettings];
        
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneManager.GetSceneByBuildIndex(i).path;
            sceneNames[i] = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        }
        
        return sceneNames;
    }
}