using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Áp màu sơn (paint) từ PlayerCarLoadout lên thân xe ở SCENE ĐUA.
///
/// Trong garage, việc này do <see cref="GarageSaveManager.RestorePaint"/> làm (đọc loadout.paint
/// rồi set vào body renderer). Scene đua KHÔNG có GarageSaveManager nên xe CarType_* không tự lên màu
/// — đó là lý do paint từ loadout không hiển thị. Component này thay thế đúng phần "apply paint" đó.
///
/// Nguồn paint (ưu tiên): <see cref="CarLoadoutSlot.loadout"/>.paint trên cùng GameObject,
/// fallback <see cref="ActiveLoadout.Current"/>.paint.
/// Target: mọi <see cref="CarPaintTarget"/> trong children (renderer + materialSlotIndex),
/// và mọi Renderer gắn tag "PaintPart".
/// </summary>
public class LoadoutPaintApplier : MonoBehaviour
{
    [Tooltip("Loadout slot nguồn. Để trống → tự GetComponent trên object này.")]
    [SerializeField] private CarLoadoutSlot loadoutSlot;

    [Tooltip("Tự áp paint khi Start và mỗi lần object được bật lại.")]
    [SerializeField] private bool applyOnEnable = true;

    [Tooltip("Log debug khi áp paint.")]
    [SerializeField] private bool showDebugInfo = false;

    private bool _started;

    private void Awake()
    {
        if (loadoutSlot == null)
            loadoutSlot = GetComponent<CarLoadoutSlot>();
    }

    private void Start()
    {
        _started = true;
        Apply();
    }

    private void OnEnable()
    {
        if (_started && applyOnEnable)
            Apply();
    }

    /// <summary>Áp màu sơn từ loadout lên thân xe. Gọi được từ ngoài (vd LoadSceneController sau khi activate xe).</summary>
    public void Apply()
    {
        Material paint = ResolvePaint();
        if (paint == null)
        {
            if (showDebugInfo)
                Debug.Log($"[LoadoutPaintApplier: {name}] Loadout chưa có paint — bỏ qua.");
            return;
        }

        var seen = new HashSet<Renderer>();
        int applied = 0;

        foreach (CarPaintTarget target in GetComponentsInChildren<CarPaintTarget>(true))
        {
            if (target.bodyRenderer == null) continue;
            seen.Add(target.bodyRenderer);
            if (SetRendererSlot(target.bodyRenderer, target.materialSlotIndex, paint)) applied++;
        }

        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (!seen.Contains(r) && r.CompareTag("PaintPart"))
                if (SetRendererSlot(r, r.sharedMaterials.Length - 1, paint)) applied++;
        }

        if (showDebugInfo)
            Debug.Log($"[LoadoutPaintApplier: {name}] Áp paint '{paint.name}' lên {applied} renderer(s).");
    }

    private Material ResolvePaint()
    {
        if (loadoutSlot != null && loadoutSlot.loadout != null && loadoutSlot.loadout.paint != null)
            return loadoutSlot.loadout.paint;

        return ActiveLoadout.Current != null ? ActiveLoadout.Current.paint : null;
    }

    private static bool SetRendererSlot(Renderer renderer, int slotIndex, Material mat)
    {
        if (renderer == null) return false;
        Material[] mats = renderer.sharedMaterials;
        if (slotIndex < 0 || slotIndex >= mats.Length) return false;

        mats[slotIndex] = mat;
        renderer.sharedMaterials = mats;
        return true;
    }
}
