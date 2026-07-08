using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component để thay thế bánh xe của xe
/// Hỗ trợ 4 bánh xe với các prefab khác nhau
/// </summary>
public class WheelReplacer : MonoBehaviour
{
    [System.Serializable]
    public class WheelPosition
    {
        public string name = "Wheel";
        public Transform wheelTransform;
        public GameObject currentWheel;
    }

    [Header("Wheel Positions")]
    [Tooltip("4 vị trí bánh xe trên xe")]
    public WheelPosition[] wheelPositions = new WheelPosition[4];

    [Header("Wheel Prefabs")]
    [Tooltip("Danh sách các prefab bánh xe có thể chọn")]
    public GameObject[] wheelPrefabs;

    [Header("Settings")]
    [Tooltip("Tự động thay thế tất cả bánh xe khi start")]
    public bool autoReplaceOnStart = false;

    [Tooltip("Prefab mặc định để thay thế (index trong mảng wheelPrefabs)")]
    public int defaultWheelIndex = 0;


    void Start()
    {
        if (autoReplaceOnStart && wheelPrefabs.Length > 0)
        {
            ReplaceAllWheels(defaultWheelIndex);
        }
    }


    /// <summary>
    /// Thay thế bánh xe tại vị trí cụ thể
    /// </summary>
    /// <param name="positionIndex">Index vị trí bánh xe (0-3)</param>
    /// <param name="wheelPrefabIndex">Index prefab bánh xe trong danh sách</param>
    public void ReplaceWheel(int positionIndex, int wheelPrefabIndex)
    {
        if (positionIndex < 0 || positionIndex >= wheelPositions.Length)
        {
            Debug.LogError($"[WheelReplacer] Invalid position index: {positionIndex}");
            return;
        }

        if (wheelPrefabIndex < 0 || wheelPrefabIndex >= wheelPrefabs.Length)
        {
            Debug.LogError($"[WheelReplacer] Invalid wheel prefab index: {wheelPrefabIndex}");
            return;
        }

        WheelPosition wheelPos = wheelPositions[positionIndex];

        if (wheelPos.wheelTransform == null)
        {
            Debug.LogError($"[WheelReplacer] Wheel transform at position {positionIndex} is null!");
            return;
        }

        // Xóa bánh xe cũ
        if (wheelPos.currentWheel != null)
        {
            Destroy(wheelPos.currentWheel);
        }

        // Tạo bánh xe mới
        GameObject newWheel = Instantiate(wheelPrefabs[wheelPrefabIndex], wheelPos.wheelTransform);
        newWheel.transform.localPosition = Vector3.zero;
        newWheel.transform.localRotation = Quaternion.identity;
        newWheel.transform.localScale = Vector3.one;

        // Lưu reference
        wheelPos.currentWheel = newWheel;

        Debug.Log($"[WheelReplacer] Replaced wheel at position {positionIndex} with prefab {wheelPrefabIndex}");
    }


    /// <summary>
    /// Thay thế tất cả bánh xe với cùng một prefab
    /// </summary>
    /// <param name="wheelPrefabIndex">Index prefab bánh xe</param>
    public void ReplaceAllWheels(int wheelPrefabIndex)
    {
        for (int i = 0; i < wheelPositions.Length; i++)
        {
            ReplaceWheel(i, wheelPrefabIndex);
        }
    }


    /// <summary>
    /// Thay thế bánh xe theo tên vị trí
    /// </summary>
    /// <param name="positionName">Tên vị trí bánh xe</param>
    /// <param name="wheelPrefabIndex">Index prefab bánh xe</param>
    public void ReplaceWheelByName(string positionName, int wheelPrefabIndex)
    {
        for (int i = 0; i < wheelPositions.Length; i++)
        {
            if (wheelPositions[i].name == positionName)
            {
                ReplaceWheel(i, wheelPrefabIndex);
                return;
            }
        }

        Debug.LogError($"[WheelReplacer] Wheel position '{positionName}' not found!");
    }


    /// <summary>
    /// Lấy danh sách tên các vị trí bánh xe
    /// </summary>
    public string[] GetWheelPositionNames()
    {
        string[] names = new string[wheelPositions.Length];
        for (int i = 0; i < wheelPositions.Length; i++)
        {
            names[i] = wheelPositions[i].name;
        }
        return names;
    }


    /// <summary>
    /// Lấy danh sách tên các prefab bánh xe
    /// </summary>
    public string[] GetWheelPrefabNames()
    {
        string[] names = new string[wheelPrefabs.Length];
        for (int i = 0; i < wheelPrefabs.Length; i++)
        {
            names[i] = wheelPrefabs[i] != null ? wheelPrefabs[i].name : "Null";
        }
        return names;
    }


    /// <summary>
    /// Kiểm tra xem có bánh xe tại vị trí không
    /// </summary>
    public bool HasWheelAtPosition(int positionIndex)
    {
        if (positionIndex < 0 || positionIndex >= wheelPositions.Length)
            return false;

        return wheelPositions[positionIndex].currentWheel != null;
    }


    /// <summary>
    /// Xóa tất cả bánh xe
    /// </summary>
    public void ClearAllWheels()
    {
        for (int i = 0; i < wheelPositions.Length; i++)
        {
            if (wheelPositions[i].currentWheel != null)
            {
                Destroy(wheelPositions[i].currentWheel);
                wheelPositions[i].currentWheel = null;
            }
        }

        Debug.Log("[WheelReplacer] Cleared all wheels");
    }


    /// <summary>
    /// Thay thế bánh xe FL (Front Left)
    /// </summary>
    public void ReplaceFrontLeftWheel(int wheelIndex)
    {
        ReplaceWheel(0, wheelIndex);
    }


    /// <summary>
    /// Thay thế bánh xe FR (Front Right)
    /// </summary>
    public void ReplaceFrontRightWheel(int wheelIndex)
    {
        ReplaceWheel(1, wheelIndex);
    }


    /// <summary>
    /// Thay thế bánh xe RL (Rear Left)
    /// </summary>
    public void ReplaceRearLeftWheel(int wheelIndex)
    {
        ReplaceWheel(2, wheelIndex);
    }


    /// <summary>
    /// Thay thế bánh xe RR (Rear Right)
    /// </summary>
    public void ReplaceRearRightWheel(int wheelIndex)
    {
        ReplaceWheel(3, wheelIndex);
    }
}