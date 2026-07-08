using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only utility to render prefab previews into Sprite icons for shop UI.
/// </summary>
public static class PrefabIconGenerator
{
    private const string BrakeIconFolder = "Assets/Data/Icons/BrakeCaliper";
    private const int IconSize = 512;

    [MenuItem("Tools/Garage Icons/Generate Brake Icons And Assign To CarParts")]
    public static void GenerateBrakeIconsAndAssignToCarParts()
    {
        EnsureFolder("Assets/Data/Icons");
        EnsureFolder(BrakeIconFolder);

        int generated = 0;
        string[] partGuids = AssetDatabase.FindAssets("t:CarPart", new[] { "Assets/Data/CarParts" });
        foreach (string guid in partGuids)
        {
            string partPath = AssetDatabase.GUIDToAssetPath(guid);
            CarPart part = AssetDatabase.LoadAssetAtPath<CarPart>(partPath);
            if (part == null || part.slot != CarPart.PartSlot.Brakes)
                continue;

            GameObject prefab = part.brakePrefabLeft != null ? part.brakePrefabLeft : part.brakePrefabRight;
            if (prefab == null)
            {
                Debug.LogWarning($"[PrefabIconGenerator] Skip {part.name}: no brake prefab assigned.");
                continue;
            }

            string iconPath = $"{BrakeIconFolder}/{SanitizeFileName(part.name)}.png";
            RenderPrefabToPng(prefab, iconPath, IconSize);
            ConfigureTextureAsSprite(iconPath);

            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (icon != null)
            {
                part.icon = icon;
                EditorUtility.SetDirty(part);
                generated++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PrefabIconGenerator] Generated and assigned {generated} brake icon(s).");
    }

    [MenuItem("Tools/Garage Icons/Generate Icons For Selected Prefabs")]
    public static void GenerateIconsForSelectedPrefabs()
    {
        EnsureFolder("Assets/Data/Icons");
        EnsureFolder(BrakeIconFolder);

        int generated = 0;
        foreach (Object selected in Selection.objects)
        {
            GameObject prefab = selected as GameObject;
            if (prefab == null)
                continue;

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(prefabPath) || !prefabPath.EndsWith(".prefab"))
                continue;

            string iconPath = $"{BrakeIconFolder}/{SanitizeFileName(prefab.name)}.png";
            RenderPrefabToPng(prefab, iconPath, IconSize);
            ConfigureTextureAsSprite(iconPath);
            generated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PrefabIconGenerator] Generated {generated} selected prefab icon(s).");
    }

    private static void RenderPrefabToPng(GameObject prefab, string outputPath, int size)
    {
        var preview = new PreviewRenderUtility();
        preview.camera.orthographic = true;
        preview.camera.clearFlags = CameraClearFlags.SolidColor;
        preview.camera.backgroundColor = new Color(0f, 0f, 0f, 0f);

        GameObject instance = Object.Instantiate(prefab);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        preview.AddSingleGO(instance);

        Bounds bounds = CalculateBounds(instance);
        Vector3 center = bounds.center;
        Vector3 size3 = bounds.size;
        ViewFrame frame = GetLargestFaceFrame(size3);
        float radius = Mathf.Max(bounds.extents.magnitude, 0.01f);
        float distance = radius * 4f;

        preview.camera.transform.position = center + frame.viewDirection * distance;
        preview.camera.transform.rotation = Quaternion.LookRotation(-frame.viewDirection, frame.up);
        preview.camera.orthographicSize = Mathf.Max(frame.verticalSize * 0.55f, frame.horizontalSize * 0.55f, 0.01f);
        preview.camera.nearClipPlane = 0.01f;
        preview.camera.farClipPlane = distance + radius * 8f;

        preview.lights[0].intensity = 1.2f;
        preview.lights[0].transform.rotation = Quaternion.LookRotation(-frame.viewDirection, frame.up);
        preview.lights[1].intensity = 0.45f;

        RenderTexture renderTexture = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;

        preview.camera.targetTexture = renderTexture;
        preview.camera.Render();
        RenderTexture.active = renderTexture;

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        texture.Apply();

        File.WriteAllBytes(outputPath, texture.EncodeToPNG());

        Object.DestroyImmediate(texture);
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);
        preview.Cleanup();
    }

    private static ViewFrame GetLargestFaceFrame(Vector3 size)
    {
        float xyArea = size.x * size.y;
        float xzArea = size.x * size.z;
        float yzArea = size.y * size.z;

        if (xyArea >= xzArea && xyArea >= yzArea)
        {
            return new ViewFrame
            {
                viewDirection = Vector3.back,
                up = Vector3.up,
                horizontalSize = size.x,
                verticalSize = size.y
            };
        }

        if (xzArea >= xyArea && xzArea >= yzArea)
        {
            return new ViewFrame
            {
                viewDirection = Vector3.down,
                up = Vector3.forward,
                horizontalSize = size.x,
                verticalSize = size.z
            };
        }

        return new ViewFrame
        {
            viewDirection = Vector3.left,
            up = Vector3.up,
            horizontalSize = size.z,
            verticalSize = size.y
        };
    }

    private struct ViewFrame
    {
        public Vector3 viewDirection;
        public Vector3 up;
        public float horizontalSize;
        public float verticalSize;
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    private static void ConfigureTextureAsSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string folder = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folder);
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return value;
    }
}
