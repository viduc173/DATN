using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Paint can for garage cars.
/// It resolves the active car through GarageCarManager -> CarPaintTarget, then
/// chooses a paint material for that specific car.
/// </summary>
public class CarPaintCan : MonoBehaviour
{
    [Serializable]
    private class CarPaintMaterial
    {
        public string carName;
        public Material material;
    }

    [Header("Body Override")]
    [Tooltip("Optional manual renderer override. Leave empty to use the active car's CarPaintTarget.")]
    [SerializeField] private Renderer carBodyRenderer;

    [Tooltip("Material slot when using the manual renderer override.")]
    [SerializeField] private int materialSlotIndex = 0;

    [Header("Paint Material")]
    [Tooltip("Fallback paint material. Also used for CarType_0 in this scene.")]
    [SerializeField] private Material paintMaterial;

    [Tooltip("Paint material overrides by active car name, for example CarType_1 or CarType_2.")]
    [SerializeField] private CarPaintMaterial[] carPaintMaterials;

    [Header("Paint VFX")]
    [SerializeField] private GameObject vfxPrefab;
    [SerializeField] private Transform vfxSpawnPoint;
    [SerializeField] private Vector3 vfxScale = new Vector3(0.3f, 0.3f, 0.3f);
    [SerializeField] private float vfxLifetime = 1f;

    [Header("Events")]
    public UnityEvent onPreviewStarted;
    public UnityEvent onPaintApplied;
    public UnityEvent onPreviewCancelled;

    private Renderer[] _previewRenderers;
    private int[] _previewSlots;
    private Material[] _previewOriginals;
    private bool _isPreviewing;

    private void Start()
    {
        if (GarageCarManager.Instance != null)
            GarageCarManager.Instance.onCarChanged.AddListener(OnActiveCarChanged);
    }

    private void OnDestroy()
    {
        if (GarageCarManager.Instance != null)
            GarageCarManager.Instance.onCarChanged.RemoveListener(OnActiveCarChanged);
    }

    private void OnActiveCarChanged(int _) => CancelPreview();

    private bool TryResolveTargets(out Renderer[] renderers, out int[] slots, out Material material)
    {
        string carTypeName = string.Empty;

        if (carBodyRenderer != null)
        {
            carTypeName = GarageCarManager.Instance?.ActiveCarName ?? string.Empty;
            material = ResolvePaintMaterial(carTypeName);
            renderers = new[] { carBodyRenderer };
            slots = new[] { materialSlotIndex };
            return ValidateTargets(renderers, slots, material);
        }

        if (GarageCarManager.Instance == null)
        {
            Debug.LogWarning($"[CarPaintCan: {name}] GarageCarManager not found.");
            renderers = null;
            slots = null;
            material = null;
            return false;
        }

        if (!GarageCarManager.Instance.TryGetActivePaintTargets(out renderers, out slots, out carTypeName))
        {
            Debug.LogWarning($"[CarPaintCan: {name}] Active car '{GarageCarManager.Instance.ActiveCarName}' has no valid PaintPart renderer.");
            material = null;
            return false;
        }

        material = ResolvePaintMaterial(carTypeName);
        return ValidateTargets(renderers, slots, material);
    }

    private Material ResolvePaintMaterial(string carTypeName)
    {
        if (!string.IsNullOrWhiteSpace(carTypeName) && carPaintMaterials != null)
        {
            foreach (CarPaintMaterial entry in carPaintMaterials)
            {
                if (entry == null || entry.material == null || string.IsNullOrWhiteSpace(entry.carName))
                    continue;

                if (string.Equals(entry.carName, carTypeName, StringComparison.OrdinalIgnoreCase))
                    return entry.material;
            }
        }

        return paintMaterial;
    }

    private bool ValidateTargets(Renderer[] renderers, int[] slots, Material material)
    {
        if (renderers == null || slots == null || renderers.Length == 0 || renderers.Length != slots.Length)
        {
            Debug.LogWarning($"[CarPaintCan: {name}] Paint target list is invalid.");
            return false;
        }

        if (material == null)
        {
            Debug.LogWarning($"[CarPaintCan: {name}] No paint material configured for the active car.");
            return false;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            int slot = slots[i];
            if (renderer == null)
            {
                Debug.LogWarning($"[CarPaintCan: {name}] Renderer is null.");
                return false;
            }

            if (slot < 0 || slot >= renderer.sharedMaterials.Length)
            {
                Debug.LogError($"[CarPaintCan: {name}] materialSlotIndex={slot} is outside '{renderer.name}' material slots ({renderer.sharedMaterials.Length}).");
                return false;
            }
        }

        return true;
    }

    public void PreviewPaint()
    {
        if (!TryResolveTargets(out Renderer[] renderers, out int[] slots, out Material material))
            return;

        if (_isPreviewing)
            CancelPreview();

        _previewRenderers = renderers;
        _previewSlots = slots;
        _previewOriginals = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            _previewOriginals[i] = renderers[i].sharedMaterials[slots[i]];

        _isPreviewing = true;

        SwapMaterials(renderers, slots, material);
        SpawnVFX();
        onPreviewStarted?.Invoke();
        Debug.Log($"[CarPaintCan: {name}] Preview {GarageCarManager.Instance?.ActiveCarName} -> {renderers.Length} PaintPart renderer(s) = {material.name}");
    }

    public void ApplyPaint()
    {
        if (!TryResolveTargets(out Renderer[] renderers, out int[] slots, out Material material))
            return;

        string carName = GarageCarManager.Instance?.ActiveCarName ?? string.Empty;

        _previewRenderers = null;
        _previewSlots = null;
        _previewOriginals = null;
        _isPreviewing = false;

        SwapMaterials(renderers, slots, material);
        GarageSaveManager.Instance?.RecordPaint(carName, material);
        SpawnVFX();
        onPaintApplied?.Invoke();
        Debug.Log($"[CarPaintCan: {name}] Apply {carName} -> {renderers.Length} PaintPart renderer(s) = {material.name}");
    }

    public void CancelPreview()
    {
        if (!_isPreviewing)
            return;

        if (_previewRenderers != null && _previewSlots != null && _previewOriginals != null)
        {
            int count = Mathf.Min(_previewRenderers.Length, _previewSlots.Length, _previewOriginals.Length);
            for (int i = 0; i < count; i++)
                if (_previewRenderers[i] != null)
                    SwapMaterial(_previewRenderers[i], _previewSlots[i], _previewOriginals[i]);
        }

        _previewRenderers = null;
        _previewSlots = null;
        _previewOriginals = null;
        _isPreviewing = false;

        onPreviewCancelled?.Invoke();
        Debug.Log($"[CarPaintCan: {name}] Preview cancelled.");
    }

    public void TriggerVFX() => SpawnVFX();

    public void SetPaintMaterial(Material newMaterial)
    {
        if (newMaterial == null)
            return;

        paintMaterial = newMaterial;
    }

    /// <summary>
    /// Trả về tất cả Material mà bình sơn này có thể dùng.
    /// Dùng bởi GarageSaveManager để build cache material khi khởi động.
    /// </summary>
    public System.Collections.Generic.IEnumerable<Material> GetAllMaterials()
    {
        if (paintMaterial != null) yield return paintMaterial;
        if (carPaintMaterials != null)
            foreach (CarPaintMaterial cm in carPaintMaterials)
                if (cm?.material != null) yield return cm.material;
    }

    private void SwapMaterial(Renderer renderer, int slot, Material material)
    {
        Material[] materials = renderer.sharedMaterials;
        materials[slot] = material;
        renderer.sharedMaterials = materials;
    }

    private void SwapMaterials(Renderer[] renderers, int[] slots, Material material)
    {
        int count = Mathf.Min(renderers.Length, slots.Length);
        for (int i = 0; i < count; i++)
        {
            if (renderers[i] != null)
                SwapMaterial(renderers[i], slots[i], material);
        }
    }

    private void SpawnVFX()
    {
        if (vfxPrefab == null)
            return;

        Vector3 spawnPos = vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position;
        Quaternion spawnRot = vfxSpawnPoint != null ? vfxSpawnPoint.rotation : transform.rotation;

        GameObject vfxInstance = Instantiate(vfxPrefab, spawnPos, spawnRot);
        vfxInstance.transform.localScale = vfxScale;

        ParticleSystem ps = vfxInstance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
        }

        Destroy(vfxInstance, vfxLifetime);
    }

#if UNITY_EDITOR
    [ContextMenu("Preview")] private void DbgPreview() => PreviewPaint();
    [ContextMenu("Apply")] private void DbgApply() => ApplyPaint();
    [ContextMenu("Cancel")] private void DbgCancel() => CancelPreview();
    [ContextMenu("Test VFX")] private void DbgVFX() => SpawnVFX();
#endif
}
