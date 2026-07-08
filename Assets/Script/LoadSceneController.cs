using System.Collections.Generic;
using UnityEngine;

// Chạy SỚM (trước RacePositionTracker/MatchWaitTime/LevelController) để xe player đúng
// được kích hoạt trước khi các hệ khác resolve player theo tag/transform.
[DefaultExecutionOrder(-110)]
public class LoadSceneController : MonoBehaviour
{
    [Header("Car List")]
    [Tooltip("Cars by selected index. Runtime also resolves CarType_0/1/2 under PlayerCarManager.")]
    public List<GameObject> cars = new List<GameObject>();
    public Transform carContainer;

    [Header("References")]
    public RacePositionTracker racePositionTracker;
    public MatchWaitTime matchWaitTime;

    [Header("Follow Active Car")]
    public Transform uiController;
    public Transform bonus;
    public string uiAnchorName = "UI";
    public string bonusAnchorName = "NitroFx";
    public bool resetAnchoredLocalTransform = true;

    [Header("Settings")]
    public int defaultCarIndex = 0;
    public bool autoFindTracker = true;
    public bool autoFindMatchWaitTime = true;
    public bool autoFindSystemController = true;

    private GameObject currentActiveCar;

    private void Start()
    {
        LoadSelectedCar();
    }

    private void LoadSelectedCar()
    {
        RefreshCarList();

        int carIndex = ResolveSelectedCarIndex(out string source);
        Debug.Log($"[LoadSceneController] Loading car index {carIndex} (source: {source})");
        ActivateCar(carIndex);
    }

    /// <summary>
    /// Thứ tự ưu tiên nguồn chọn xe:
    ///   1) ActiveLoadout.SavedCarIndex — xe đang display ở garage (GarageCarManager ghi, persist PlayerPrefs).
    ///   2) SystemController.GetSelectedCarIndex() — flow garage cũ (nếu còn dùng).
    ///   3) Xe đang active sẵn trong scene.
    ///   4) defaultCarIndex (Inspector).
    /// LƯU Ý: index = thứ tự slot xe trong garage (con của CarPlace) và PHẢI khớp tên CarType_{index}
    /// trong scene đua. Nếu thứ tự 2 bên lệch nhau thì xe load ra sẽ sai dù index đúng.
    /// </summary>
    private int ResolveSelectedCarIndex(out string source)
    {
        if (ActiveLoadout.HasSavedCar)
        {
            source = "ActiveLoadout (garage)";
            return ActiveLoadout.SavedCarIndex;
        }

        if (SystemController.Instance != null)
        {
            source = "SystemController";
            return SystemController.Instance.GetSelectedCarIndex();
        }

        int activeIndex = ResolveActiveCarIndex();
        if (activeIndex >= 0)
        {
            source = "active-in-scene";
            return activeIndex;
        }

        source = "defaultCarIndex";
        return defaultCarIndex;
    }

    private void ActivateCar(int carIndex)
    {
        RefreshCarList();

        if (cars.Count == 0)
        {
            Debug.LogError("Car list is empty!");
            return;
        }

        GameObject selectedCar = ResolveCarByIndex(carIndex);

        if (selectedCar == null)
        {
            Debug.LogWarning($"Invalid car index {carIndex}. Using default index {defaultCarIndex} instead.");
            carIndex = defaultCarIndex;
            selectedCar = ResolveCarByIndex(carIndex);
        }

        if (selectedCar == null)
        {
            Debug.LogError($"Car at index {carIndex} is null!");
            return;
        }

        foreach (GameObject car in cars)
        {
            if (car != null)
                car.SetActive(false);
        }

        selectedCar.SetActive(true);
        currentActiveCar = selectedCar;
        Debug.Log($"Activated car at index {carIndex}: {selectedCar.name}");

        AttachSceneObjectsToActiveCar();
        UpdateRacePositionTracker();
        UpdateMatchWaitTime();
        ApplyLoadoutPaint();
    }

    /// <summary>
    /// Áp màu sơn từ loadout lên xe đang active. Scene đua không có GarageSaveManager nên phải
    /// tự làm; dùng LoadoutPaintApplier (tự thêm nếu xe chưa gắn sẵn).
    /// </summary>
    private void ApplyLoadoutPaint()
    {
        if (currentActiveCar == null) return;

        var applier = currentActiveCar.GetComponent<LoadoutPaintApplier>();
        if (applier == null)
            applier = currentActiveCar.AddComponent<LoadoutPaintApplier>();

        applier.Apply();
    }

    private void RefreshCarList()
    {
        if (carContainer == null)
        {
            GameObject container = GameObject.Find("PlayerCarManager");
            if (container != null)
                carContainer = container.transform;
        }

        if (carContainer == null) return;

        for (int i = 0; i <= 2; i++)
        {
            Transform car = FindDirectChild(carContainer, $"CarType_{i}");
            if (car != null && !cars.Contains(car.gameObject))
                cars.Add(car.gameObject);
        }
    }

    private GameObject ResolveCarByIndex(int carIndex)
    {
        if (carContainer != null)
        {
            Transform car = FindDirectChild(carContainer, $"CarType_{carIndex}");
            if (car != null)
                return car.gameObject;
        }

        if (carIndex >= 0 && carIndex < cars.Count)
            return cars[carIndex];

        return null;
    }

    private int ResolveActiveCarIndex()
    {
        RefreshCarList();

        for (int i = 0; i <= 2; i++)
        {
            GameObject car = ResolveCarByIndex(i);
            if (car != null && car.activeSelf)
                return i;
        }

        for (int i = 0; i < cars.Count; i++)
        {
            if (cars[i] != null && cars[i].activeSelf)
                return i;
        }

        return -1;
    }

    private void AttachSceneObjectsToActiveCar()
    {
        if (currentActiveCar == null) return;

        Transform uiAnchor = FindChildRecursive(currentActiveCar.transform, uiAnchorName);
        Transform bonusAnchor = FindChildRecursive(currentActiveCar.transform, bonusAnchorName);

        AttachToAnchor(ResolveUiController(), uiAnchor, "UIController", uiAnchorName);
        AttachToAnchor(ResolveBonus(), bonusAnchor, "Bonus", bonusAnchorName);
    }

    private Transform ResolveUiController()
    {
        if (uiController != null) return uiController;

        GameObject taggedUi = GameObject.FindGameObjectWithTag("PlayerUI");
        if (taggedUi != null)
        {
            uiController = taggedUi.transform;
            return uiController;
        }

        GameObject namedUi = GameObject.Find("UIController");
        if (namedUi != null)
            uiController = namedUi.transform;

        return uiController;
    }

    private Transform ResolveBonus()
    {
        if (bonus != null) return bonus;

        GameObject playerController = GameObject.Find("PlayerController");
        if (playerController != null)
        {
            Transform childBonus = FindDirectChild(playerController.transform, "Bonus");
            if (childBonus != null)
            {
                bonus = childBonus;
                return bonus;
            }
        }

        return bonus;
    }

    private void AttachToAnchor(Transform target, Transform anchor, string targetName, string anchorName)
    {
        if (target == null)
        {
            Debug.LogWarning($"{targetName} not found, cannot attach to active car.");
            return;
        }

        if (anchor == null)
        {
            Debug.LogWarning($"Anchor '{anchorName}' not found on active car '{currentActiveCar.name}'.");
            return;
        }

        target.SetParent(anchor, false);

        if (resetAnchoredLocalTransform)
        {
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
        }

        Debug.Log($"Attached {targetName} to {currentActiveCar.name}/{anchorName}");
    }

    private void UpdateRacePositionTracker()
    {
        if (racePositionTracker == null && autoFindTracker)
        {
            racePositionTracker = FindObjectOfType<RacePositionTracker>();

            if (racePositionTracker == null)
            {
                Debug.LogWarning("RacePositionTracker not found in scene!");
                return;
            }
        }

        if (racePositionTracker == null)
        {
            Debug.LogWarning("RacePositionTracker is not assigned and auto-find is disabled!");
            return;
        }

        if (currentActiveCar == null)
        {
            Debug.LogWarning("No active car to update RacePositionTracker!");
            return;
        }

        racePositionTracker.SetPlayer(currentActiveCar.transform);
        Debug.Log($"Updated RacePositionTracker player to: {currentActiveCar.name}");
    }

    /// <summary>Đẩy xe player đang active sang MatchWaitTime để countdown khoá/mở đúng xe.</summary>
    private void UpdateMatchWaitTime()
    {
        if (matchWaitTime == null && autoFindMatchWaitTime)
            matchWaitTime = GetComponent<MatchWaitTime>() ?? FindObjectOfType<MatchWaitTime>();

        if (matchWaitTime == null)
        {
            Debug.LogWarning("MatchWaitTime not found — countdown sẽ không biết xe player nào.");
            return;
        }

        if (currentActiveCar == null) return;

        matchWaitTime.SetPlayerVehicle(currentActiveCar);
        Debug.Log($"Updated MatchWaitTime player vehicle to: {currentActiveCar.name}");
    }

    public void SwitchCar(int newCarIndex)
    {
        // Cập nhật cả nguồn chính (ActiveLoadout) lẫn legacy (SystemController) cho nhất quán.
        ActiveLoadout.SavedCarIndex = newCarIndex;

        if (SystemController.Instance != null)
            SystemController.Instance.SetSelectedCar(newCarIndex);

        ActivateCar(newCarIndex);
    }

    public GameObject GetCurrentCar()
    {
        return currentActiveCar;
    }

    public int GetCurrentCarIndex()
    {
        if (currentActiveCar != null && cars.Contains(currentActiveCar))
            return cars.IndexOf(currentActiveCar);

        return -1;
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null) return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null) return null;

        if (parent.name == childName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChildRecursive(parent.GetChild(i), childName);
            if (result != null)
                return result;
        }

        return null;
    }
}
