using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only utility de render xe (scene object hoac prefab) thanh Sprite icon cho UI garage.
/// Khac voi PrefabIconGenerator (chup mat phang lon nhat cua part), script nay chup xe o goc
/// 3/4 phoi canh cho dep, nen trong suot.
///
/// Cach dung:
///   1. Chon CarType_0 (hoac nhieu xe) trong Hierarchy / Project.
///   2. Tools > Garage Icons > Generate Icon For Selected Car(s).
///   3. Icon PNG xuat ra Assets/Data/Icons/Cars/<ten>.png, da config san la Sprite.
/// </summary>
public static class CarIconGenerator
{
    private const string CarIconFolder = "Assets/Data/Icons/Cars";
    private const int IconSize = 1024;

    // Goc nhin 3/4 truoc: camera o phia truoc (+Z), lech sang phai (+X), hoi cao (+Y).
    // Day la huong tu TAM XE -> CAMERA. Doi neu muon goc khac.
    private static readonly Vector3 ViewOffsetDir = new Vector3(0.85f, 0.45f, 1f).normalized;
    private const float FieldOfView = 30f;

    // Zoom: < 1 keo camera lai gan cho xe to len. 0.72 ~ cat bot ~28% le.
    private const float FillFactor = 0.82f;

    [MenuItem("Tools/Garage Icons/Generate Icon For Selected Car(s)")]
    public static void GenerateIconForSelectedCars()
    {
        EnsureFolder("Assets/Data/Icons");
        EnsureFolder(CarIconFolder);

        int generated = 0;
        foreach (Object selected in Selection.objects)
        {
            GameObject source = selected as GameObject;
            if (source == null)
                continue;

            string iconPath = $"{CarIconFolder}/{SanitizeFileName(source.name)}.png";
            RenderCarToPng(source, iconPath, IconSize);
            ConfigureTextureAsSprite(iconPath);
            generated++;
            Debug.Log($"[CarIconGenerator] Saved car icon: {iconPath}");
        }

        if (generated == 0)
        {
            Debug.LogWarning("[CarIconGenerator] Khong co GameObject nao duoc chon. Hay chon xe trong Hierarchy hoac prefab trong Project.");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CarIconGenerator] Generated {generated} car icon(s).");
    }

    private static void RenderCarToPng(GameObject source, string outputPath, int size)
    {
        var preview = new PreviewRenderUtility();
        preview.camera.orthographic = false;
        preview.camera.fieldOfView = FieldOfView;
        preview.camera.clearFlags = CameraClearFlags.SolidColor;
        preview.camera.backgroundColor = new Color(0f, 0f, 0f, 0f);

        GameObject instance = Object.Instantiate(source);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = source.transform.lossyScale;
        preview.AddSingleGO(instance);

        Bounds bounds = CalculateBounds(instance);
        Vector3 center = bounds.center;
        float radius = Mathf.Max(bounds.extents.magnitude, 0.01f);

        // Fit chinh xac 8 goc hop bao len khung hinh thay vi fit bounding sphere
        // (sphere de thua rat nhieu le voi xe dep/dai). Camera nhin theo -ViewOffsetDir.
        Vector3 forward = -ViewOffsetDir;
        Vector3 right = Vector3.Normalize(Vector3.Cross(Vector3.up, forward));
        Vector3 camUp = Vector3.Cross(forward, right);
        float tanHalf = Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad);

        Vector3 ext = bounds.extents;
        float distance = 0.01f;
        for (int sx = -1; sx <= 1; sx += 2)
        for (int sy = -1; sy <= 1; sy += 2)
        for (int sz = -1; sz <= 1; sz += 2)
        {
            // Goc hop tinh tu tam.
            Vector3 c = new Vector3(ext.x * sx, ext.y * sy, ext.z * sz);
            float along = Vector3.Dot(c, ViewOffsetDir);          // bu lai do sau theo huong nhin
            float h = Mathf.Abs(Vector3.Dot(c, right));
            float v = Mathf.Abs(Vector3.Dot(c, camUp));
            // Khoang cach toi thieu de goc nay nam trong FOV (ca chieu ngang & doc).
            float dReq = along + Mathf.Max(h, v) / tanHalf;
            if (dReq > distance) distance = dReq;
        }
        distance *= FillFactor; // < 1 = zoom vao cho xe lap day khung (giam le)

        Vector3 camPos = center + ViewOffsetDir * distance;
        preview.camera.transform.position = camPos;
        preview.camera.transform.rotation = Quaternion.LookRotation(center - camPos, Vector3.up);
        preview.camera.nearClipPlane = 0.01f;
        preview.camera.farClipPlane = distance + radius * 8f;

        // Anh sang: key light chieu tu huong camera, fill nhe.
        preview.lights[0].intensity = 1.25f;
        preview.lights[0].transform.rotation = Quaternion.Euler(35f, 200f, 0f);
        preview.lights[1].intensity = 0.5f;

        // antiAliasing 8x de net, het mo rang cua.
        RenderTexture renderTexture = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 8);
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
        Object.DestroyImmediate(instance);
        preview.Cleanup();
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
