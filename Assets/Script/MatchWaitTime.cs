using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using EVP;
using UnityEngine.Events;

public class MatchWaitTime : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> vehicles = new List<GameObject>();
    [SerializeField]
    private List<GameObject> playerVehicles = new List<GameObject>();
    [SerializeField]
    private float waitTime = 5f;
    [SerializeField]
    private TextMeshProUGUI WaitText;
    [SerializeField]
    private GameObject waitObject;

    [Header("Events")]
    [Tooltip("Gọi khi countdown kết thúc — Race_Manager subscribe để start AI cars")]
    public UnityEvent onRaceStarted;

    private bool initRacing = false;
    private bool raceStartFired = false;

    /// <summary>
    /// Override waitTime trước khi Start() chạy. Gọi từ LevelController.Awake().
    /// </summary>
    public void SetWaitTime(float seconds)
    {
        waitTime = Mathf.Max(0f, seconds);
    }

    /// <summary>
    /// Gán đúng xe player mà LoadSceneController đã kích hoạt. Thay cả list playerVehicles
    /// để countdown chỉ khoá/mở VehicleController của xe đang dùng (không đụng các xe inactive khác).
    /// An toàn gọi trước hoặc sau Start().
    /// </summary>
    public void SetPlayerVehicle(GameObject playerCar)
    {
        playerVehicles.Clear();
        if (playerCar != null)
            playerVehicles.Add(playerCar);

        // Nếu vẫn đang đếm ngược, khoá ngay xe vừa gán cho khớp trạng thái các xe khác.
        if (waitTime > 0f)
            SetVehiclesEnabled(false);
    }

    void Start()
    {
        initRacing = false;

        if (waitObject != null)
            waitObject.SetActive(true);
        else
            Debug.LogWarning("[MatchWaitTime] waitObject chưa gán — countdown UI sẽ không hiển thị, nhưng logic chờ vẫn chạy.");

        if (WaitText == null)
            Debug.LogWarning("[MatchWaitTime] WaitText chưa gán — countdown sẽ log ra Console thay vì hiển thị UI.");

        SetVehiclesEnabled(false);
    }

    void Update()
    {
        if (!initRacing)
        {
            initRacing = true;
        }

        if (waitTime > 0)
        {
            int displaySeconds = Mathf.CeilToInt(waitTime);
            string countdownText = "Race starts in: " + displaySeconds;

            if (WaitText != null)
                WaitText.text = countdownText;

            waitTime -= Time.deltaTime;

            if (waitTime <= 0)
            {
                SetVehiclesEnabled(true);

                if (waitObject != null)
                    waitObject.SetActive(false);

                if (WaitText != null)
                    WaitText.text = "GO!";

                if (!raceStartFired)
                {
                    raceStartFired = true;
                    onRaceStarted?.Invoke();
                }
            }
        }
    }

    private void SetVehiclesEnabled(bool enabled)
    {
        int aiCount = 0, playerCount = 0, aiMissing = 0, playerMissing = 0;

        foreach (var vehicle in vehicles)
        {
            if (vehicle == null) continue;
            var ai = vehicle.GetComponent<CarAIController>();
            if (ai != null)
            {
                ai.enabled = enabled;
                aiCount++;
            }
            else
            {
                aiMissing++;
                Debug.LogWarning($"[MatchWaitTime] AI vehicle '{vehicle.name}' không có CarAIController ở root GameObject — bỏ qua.");
            }
        }

        foreach (var playerVehicle in playerVehicles)
        {
            if (playerVehicle == null) continue;
            var vc = playerVehicle.GetComponent<VehicleController>();
            if (vc != null)
            {
                vc.enabled = enabled;
                playerCount++;
            }
            else
            {
                playerMissing++;
                Debug.LogWarning($"[MatchWaitTime] Player vehicle '{playerVehicle.name}' không có VehicleController ở root GameObject — bỏ qua.");
            }
        }

        Debug.Log($"[MatchWaitTime] SetVehiclesEnabled({enabled}) → AI:{aiCount}/{vehicles.Count} (missing {aiMissing}), Player:{playerCount}/{playerVehicles.Count} (missing {playerMissing})");
    }
}
