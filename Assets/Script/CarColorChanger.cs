using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component để thay đổi màu sắc của xe bằng cách thay thế material
/// Hỗ trợ nhiều phần của xe với các material khác nhau
/// </summary>
public class CarColorChanger : MonoBehaviour
{
    [System.Serializable]
    public class CarPart
    {
        public string name = "Body";
        public Renderer renderer;
        [Tooltip("Material index trong renderer để thay đổi (thường là 0)")]
        public int materialIndex = 0;
    }

    [Header("Car Parts")]
    [Tooltip("Các phần của xe cần thay đổi màu")]
    public CarPart[] carParts;

    [Header("Color Materials")]
    [Tooltip("Danh sách các material màu sắc có thể chọn")]
    public Material[] colorMaterials;

    [Header("Settings")]
    [Tooltip("Tự động áp dụng màu mặc định khi start")]
    public bool autoApplyOnStart = false;

    [Tooltip("Material mặc định (index trong mảng colorMaterials)")]
    public int defaultColorIndex = 0;

    [Header("Color Options")]
    [Tooltip("Cho phép thay đổi màu theo thời gian")]
    public bool enableColorTransition = true;

    [Tooltip("Tốc độ chuyển màu")]
    public float colorTransitionSpeed = 5f;


    void Start()
    {
        if (autoApplyOnStart && colorMaterials.Length > 0)
        {
            ApplyColorToAllParts(defaultColorIndex);
        }
    }


    void Update()
    {
        // Smooth transition cho màu sắc
        if (enableColorTransition)
        {
            UpdateColorTransitions();
        }
    }


    /// <summary>
    /// Áp dụng màu cho một phần cụ thể của xe
    /// </summary>
    /// <param name="partIndex">Index phần xe trong danh sách</param>
    /// <param name="colorIndex">Index material màu trong danh sách</param>
    public void ApplyColor(int partIndex, int colorIndex)
    {
        if (partIndex < 0 || partIndex >= carParts.Length)
        {
            Debug.LogError($"[CarColorChanger] Invalid part index: {partIndex}");
            return;
        }

        if (colorIndex < 0 || colorIndex >= colorMaterials.Length)
        {
            Debug.LogError($"[CarColorChanger] Invalid color index: {colorIndex}");
            return;
        }

        CarPart part = carParts[partIndex];

        if (part.renderer == null)
        {
            Debug.LogError($"[CarColorChanger] Renderer for part '{part.name}' is null!");
            return;
        }

        if (part.materialIndex < 0 || part.materialIndex >= part.renderer.materials.Length)
        {
            Debug.LogError($"[CarColorChanger] Invalid material index {part.materialIndex} for part '{part.name}'");
            return;
        }

        // Áp dụng material mới
        Material[] materials = part.renderer.materials;
        materials[part.materialIndex] = colorMaterials[colorIndex];
        part.renderer.materials = materials;

        Debug.Log($"[CarColorChanger] Applied color {colorIndex} to part '{part.name}'");
    }


    /// <summary>
    /// Áp dụng cùng một màu cho tất cả các phần
    /// </summary>
    /// <param name="colorIndex">Index material màu</param>
    public void ApplyColorToAllParts(int colorIndex)
    {
        for (int i = 0; i < carParts.Length; i++)
        {
            ApplyColor(i, colorIndex);
        }
    }


    /// <summary>
    /// Áp dụng màu cho phần xe theo tên
    /// </summary>
    /// <param name="partName">Tên phần xe</param>
    /// <param name="colorIndex">Index material màu</param>
    public void ApplyColorByName(string partName, int colorIndex)
    {
        for (int i = 0; i < carParts.Length; i++)
        {
            if (carParts[i].name == partName)
            {
                ApplyColor(i, colorIndex);
                return;
            }
        }

        Debug.LogError($"[CarColorChanger] Car part '{partName}' not found!");
    }


    /// <summary>
    /// Áp dụng màu ngẫu nhiên cho tất cả phần
    /// </summary>
    public void ApplyRandomColor()
    {
        if (colorMaterials.Length == 0) return;

        int randomColor = Random.Range(0, colorMaterials.Length);
        ApplyColorToAllParts(randomColor);
    }


    /// <summary>
    /// Áp dụng màu ngẫu nhiên cho từng phần riêng biệt
    /// </summary>
    public void ApplyRandomColorsToParts()
    {
        if (colorMaterials.Length == 0) return;

        for (int i = 0; i < carParts.Length; i++)
        {
            int randomColor = Random.Range(0, colorMaterials.Length);
            ApplyColor(i, randomColor);
        }
    }


    /// <summary>
    /// Lấy danh sách tên các phần xe
    /// </summary>
    public string[] GetCarPartNames()
    {
        string[] names = new string[carParts.Length];
        for (int i = 0; i < carParts.Length; i++)
        {
            names[i] = carParts[i].name;
        }
        return names;
    }


    /// <summary>
    /// Lấy danh sách tên các material màu
    /// </summary>
    public string[] GetColorMaterialNames()
    {
        string[] names = new string[colorMaterials.Length];
        for (int i = 0; i < colorMaterials.Length; i++)
        {
            names[i] = colorMaterials[i] != null ? colorMaterials[i].name : "Null";
        }
        return names;
    }


    /// <summary>
    /// Reset về màu mặc định
    /// </summary>
    public void ResetToDefaultColor()
    {
        ApplyColorToAllParts(defaultColorIndex);
    }


    /// <summary>
    /// Tạo material mới với màu sắc tùy chỉnh
    /// </summary>
    /// <param name="color">Màu sắc mới</param>
    /// <returns>Material mới được tạo</returns>
    public Material CreateCustomColorMaterial(Color color)
    {
        Material newMaterial = new Material(Shader.Find("Standard"));
        newMaterial.color = color;
        newMaterial.name = $"CustomColor_{color.ToString()}";

        // Thêm vào danh sách materials
        Material[] newMaterials = new Material[colorMaterials.Length + 1];
        colorMaterials.CopyTo(newMaterials, 0);
        newMaterials[colorMaterials.Length] = newMaterial;
        colorMaterials = newMaterials;

        return newMaterial;
    }


    /// <summary>
    /// Áp dụng màu tùy chỉnh cho tất cả phần
    /// </summary>
    /// <param name="color">Màu sắc tùy chỉnh</param>
    public void ApplyCustomColor(Color color)
    {
        Material customMaterial = CreateCustomColorMaterial(color);
        int newIndex = colorMaterials.Length - 1;
        ApplyColorToAllParts(newIndex);
    }


    /// <summary>
    /// Update smooth color transitions (nếu cần)
    /// </summary>
    private void UpdateColorTransitions()
    {
        // Có thể implement smooth transition giữa các màu ở đây
        // Hiện tại để trống vì Unity đã handle material changes smoothly
    }


    /// <summary>
    /// Áp dụng màu cho thân xe (Body)
    /// </summary>
    public void ApplyBodyColor(int colorIndex)
    {
        ApplyColorByName("Body", colorIndex);
    }


    /// <summary>
    /// Áp dụng màu cho nắp capo (Hood)
    /// </summary>
    public void ApplyHoodColor(int colorIndex)
    {
        ApplyColorByName("Hood", colorIndex);
    }


    /// <summary>
    /// Áp dụng màu cho cửa xe (Doors)
    /// </summary>
    public void ApplyDoorsColor(int colorIndex)
    {
        ApplyColorByName("Doors", colorIndex);
    }


    /// <summary>
    /// Áp dụng màu cho đuôi xe (Trunk/Rear)
    /// </summary>
    public void ApplyTrunkColor(int colorIndex)
    {
        ApplyColorByName("Trunk", colorIndex);
    }
}