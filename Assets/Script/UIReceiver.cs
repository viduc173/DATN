using UnityEngine;
using TMPro;

public class UIReceiver : MonoBehaviour
{
    [Header("References")]
    [Tooltip("RacePositionTracker để lấy dữ liệu")]
    public RacePositionTracker racePositionTracker;

    [Header("UI Text Fields")]
    [Tooltip("Text hiển thị vị trí 1")]
    public TextMeshProUGUI position1Text;
    
    [Tooltip("Text hiển thị vị trí 2")]
    public TextMeshProUGUI position2Text;
    
    [Tooltip("Text hiển thị vị trí 3")]
    public TextMeshProUGUI position3Text;
    
    [Tooltip("Text hiển thị vị trí của player")]
    public TextMeshProUGUI playerPositionText;

    [Header("Settings")]
    [Tooltip("Tự động tìm RacePositionTracker nếu chưa assign")]
    public bool autoFindTracker = true;
    
    [Tooltip("Cập nhật mỗi frame (false = manual update)")]
    public bool autoUpdate = true;
    
    [Header("Toggle Objects")]
    [Tooltip("Object để toggle khi bấm T")]
    public GameObject toggleObject;

    void Start()
    {
        if (autoFindTracker && racePositionTracker == null)
        {
            racePositionTracker = FindObjectOfType<RacePositionTracker>();
            if (racePositionTracker == null)
                Debug.LogError("[UIReceiver] RacePositionTracker not found in scene!");
        }

        // Auto-assign text fields từ RaceUILabel marker — không cần drag-drop tay
        // Chỉ gán khi field đang null (Inspector assignment được ưu tiên)
        foreach (var label in FindObjectsOfType<RaceUILabel>())
        {
            switch (label.role)
            {
                case RaceUIRole.Position1:      if (position1Text      == null) position1Text      = label.TMP; break;
                case RaceUIRole.Position2:      if (position2Text      == null) position2Text      = label.TMP; break;
                case RaceUIRole.Position3:      if (position3Text      == null) position3Text      = label.TMP; break;
                case RaceUIRole.PlayerPosition: if (playerPositionText == null) playerPositionText = label.TMP; break;
            }
        }
    }

    void Update()
    {
        if (autoUpdate)
        {
            UpdateUI();
        }
        
        // Toggle 3 objects khi bấm T
        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleObjects();
        }
    }

    /// <summary>
    /// Cập nhật tất cả UI từ RacePositionTracker
    /// </summary>
    public void UpdateUI()
    {
        if (racePositionTracker == null)
        {
            return;
        }

        var ranked = racePositionTracker.GetRankedRacers();
        var checkpointsCount = racePositionTracker.checkpoints.Count;
        var totalLaps = racePositionTracker.totalLaps;

        // Cập nhật top 1
        if (position1Text != null)
        {
            if (ranked.Count > 0 && ranked[0].racerTransform != null)
            {
                var racer = ranked[0];
                int displayLap = racer.hasFinished ? totalLaps : Mathf.Clamp(racer.lapCount + 1, 1, totalLaps);
                string displayText = $"{racer.currentPosition}. {racer.racerName} | CP: {racer.currentCheckpointIndex + 1}/{checkpointsCount} | Lap: {displayLap}/{totalLaps}";
                position1Text.text = displayText;
                position1Text.color = racer.racerName == "Player" ? Color.yellow : Color.white;
            }
            else
            {
                position1Text.text = "1st ---";
                position1Text.color = Color.gray;
            }
        }

        // Cập nhật top 2
        if (position2Text != null)
        {
            if (ranked.Count > 1 && ranked[1].racerTransform != null)
            {
                var racer = ranked[1];
                int displayLap = racer.hasFinished ? totalLaps : Mathf.Clamp(racer.lapCount + 1, 1, totalLaps);
                string displayText = $"{racer.currentPosition}. {racer.racerName} | CP: {racer.currentCheckpointIndex + 1}/{checkpointsCount} | Lap: {displayLap}/{totalLaps}";
                position2Text.text = displayText;
                position2Text.color = racer.racerName == "Player" ? Color.yellow : Color.white;
            }
            else
            {
                position2Text.text = "2nd ---";
                position2Text.color = Color.gray;
            }
        }

        // Cập nhật top 3
        if (position3Text != null)
        {
            if (ranked.Count > 2 && ranked[2].racerTransform != null)
            {
                var racer = ranked[2];
                int displayLap = racer.hasFinished ? totalLaps : Mathf.Clamp(racer.lapCount + 1, 1, totalLaps);
                string displayText = $"{racer.currentPosition}. {racer.racerName} | CP: {racer.currentCheckpointIndex + 1}/{checkpointsCount} | Lap: {displayLap}/{totalLaps}";
                position3Text.text = displayText;
                position3Text.color = racer.racerName == "Player" ? Color.yellow : Color.white;
            }
            else
            {
                position3Text.text = "3rd ---";
                position3Text.color = Color.gray;
            }
        }

        // Cập nhật player position
        if (playerPositionText != null)
        {
            var playerData = ranked.Find(r => r.racerName == "Player");
            if (playerData != null)
            {
                int displayLap = playerData.hasFinished ? totalLaps : Mathf.Clamp(playerData.lapCount + 1, 1, totalLaps);
                string displayText = $"{playerData.currentPosition}. {playerData.racerName} | CP: {playerData.currentCheckpointIndex + 1}/{checkpointsCount} | Lap: {displayLap}/{totalLaps}";
                playerPositionText.text = displayText;
                playerPositionText.color = Color.yellow;
            }
            else
            {
                playerPositionText.text = "Player: --";
                playerPositionText.color = Color.white;
            }
        }
    }

    /// <summary>
    /// Set RacePositionTracker reference
    /// </summary>
    public void SetRacePositionTracker(RacePositionTracker tracker)
    {
        racePositionTracker = tracker;
    }
    
    /// <summary>
    /// Toggle active state của 3 object khi bấm T
    /// </summary>
    private void ToggleObjects()
    {
        if (toggleObject != null)
        {
            toggleObject.SetActive(!toggleObject.activeSelf);
        }
        
        Debug.Log("Toggled 3 objects visibility");
    }
}
