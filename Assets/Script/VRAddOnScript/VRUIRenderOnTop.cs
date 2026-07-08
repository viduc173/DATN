using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Ép một World-Space UI luôn vẽ ĐÈ LÊN vật cản (không bị object che) trong VR — KHÔNG làm mất material gốc.
///
/// Chiến lược giữ nguyên material:
///   - Image dùng material mặc định "UI/Default": gán material overlay (shader y hệt UI/Default nhưng
///     ZTest Always) → nhìn GIỐNG HỆT, mà không có material riêng nào bị mất.
///   - Image/Graphic có material TÙY BIẾN (Heat blur, gradient, mask…): CLONE material đó rồi chỉ bật
///     ZTest Always nếu shader hỗ trợ (_ZTestMode/_ZTest). Không hỗ trợ thì GIỮ NGUYÊN, không đụng tới.
///   - TMP text: CLONE fontMaterial, set _ZTestMode = Always → giữ nguyên toàn bộ thuộc tính chữ.
///
/// Đảo ngược được: <see cref="Revert"/> trả lại material gốc và huỷ các material clone.
/// Gắn lên canvas root, bật applyOnStart hoặc gọi menu chuột phải "Apply Render On Top".
/// </summary>
[DisallowMultipleComponent]
public class VRUIRenderOnTop : MonoBehaviour
{
    [Tooltip("Shader overlay (ZTest Always). Để trống = tự tìm 'UI/Overlay Always On Top'.")]
    [SerializeField] private Shader overlayShader;
    [Tooltip("Đẩy canvas lên trên các canvas khác.")]
    [SerializeField] private int sortingOrder = 30000;
    [Tooltip("Tự chạy khi Start.")]
    [SerializeField] private bool applyOnStart = true;
    [Tooltip("Xử lý Image/Graphic (nền, icon…).")]
    [SerializeField] private bool affectGraphics = true;
    [Tooltip("Xử lý chữ TMP.")]
    [SerializeField] private bool affectText = true;

    private const int ZTEST_ALWAYS = 8; // UnityEngine.Rendering.CompareFunction.Always

    private readonly List<(Graphic g, Material orig)> _graphicBackup = new List<(Graphic, Material)>();
    private readonly List<(TMP_Text t, Material orig)> _tmpBackup = new List<(TMP_Text, Material)>();
    private readonly List<Material> _created = new List<Material>();
    private bool _applied;

    private void Start()
    {
        if (applyOnStart) Apply();
    }

    [ContextMenu("Apply Render On Top")]
    public void Apply()
    {
        if (_applied) Revert();
        if (overlayShader == null) overlayShader = Shader.Find("UI/Overlay Always On Top");

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        if (affectGraphics)
        {
            foreach (Graphic g in GetComponentsInChildren<Graphic>(true))
            {
                if (g is TMP_Text) continue;                 // TMP xử lý riêng
                Material src = g.material;
                if (src == null || src.shader == null) continue;

                if (src.shader.name == "UI/Default")
                {
                    // Material mặc định -> overlay (giống hệt + ZTest Always). Mỗi graphic 1 instance để
                    // không phá stencil của Mask. Sprite/màu là per-renderer nên vẫn đúng.
                    if (overlayShader == null) { WarnNoShader(); break; }
                    Material ov = new Material(overlayShader) { name = "VRUIOverlay (Instance)" };
                    _created.Add(ov);
                    _graphicBackup.Add((g, g.material));
                    g.material = ov;
                }
                else if (HasZTest(src))
                {
                    // Material tùy biến nhưng shader có ZTest -> clone, giữ nguyên look, chỉ bật ZTest Always
                    Material clone = new Material(src);
                    SetZTestAlways(clone);
                    _created.Add(clone);
                    _graphicBackup.Add((g, g.material));
                    g.material = clone;
                }
                // else: material tùy biến không có ZTest -> GIỮ NGUYÊN (có thể vẫn bị che); không đụng.
            }
        }

        if (affectText)
        {
            foreach (TMP_Text t in GetComponentsInChildren<TMP_Text>(true))
            {
                Material srcShared = t.fontSharedMaterial;
                if (srcShared == null || srcShared.shader == null) continue;
                Material clone = new Material(srcShared);     // clone -> giữ nguyên mọi thuộc tính chữ
                ApplyTextOnTop(clone);
                // KHÔNG đổi renderQueue: giữ nguyên queue gốc để thứ tự vẽ UI không bị đảo (tránh che đen).
                _created.Add(clone);
                _tmpBackup.Add((t, srcShared));
                t.fontMaterial = clone;
            }
        }

        _applied = true;
    }

    [ContextMenu("Revert")]
    public void Revert()
    {
        foreach (var (g, orig) in _graphicBackup) if (g != null) g.material = orig;
        _graphicBackup.Clear();
        foreach (var (t, orig) in _tmpBackup) if (t != null) t.fontSharedMaterial = orig;
        _tmpBackup.Clear();
        foreach (Material m in _created) if (m != null) Destroy(m);
        _created.Clear();
        _applied = false;
    }

    private static bool HasZTest(Material m) => m.HasProperty("_ZTestMode") || m.HasProperty("_ZTest");

    private static void SetZTestAlways(Material m)
    {
        if (m.HasProperty("_ZTestMode")) m.SetFloat("_ZTestMode", ZTEST_ALWAYS);
        if (m.HasProperty("_ZTest"))     m.SetFloat("_ZTest", ZTEST_ALWAYS);
    }

    /// <summary>
    /// Chữ TMP render đè vật cản. TMP SDF dùng <c>ZTest [unity_GUIZTestMode]</c> (canvas ép LEqual → chữ bị
    /// vật cản 3D che). Biến thể shader "... Overlay" CHỈ khác standard đúng 2 chỗ: <c>ZTest Always</c> +
    /// <c>Queue "Overlay"(4000)</c>. Ta lấy <b>ZTest Always</b> của nó NHƯNG <b>ép renderQueue về queue gốc</b>
    /// (Transparent ≈3000) — vì Queue 4000 làm lệch thứ tự vẽ UI &amp; hỏng stencil mask (chữ biến mất / đè sai,
    /// lan cả scene khác). Stencil/Cull/ZWrite của 2 shader y hệt nhau → kết quả = material gốc + đúng mỗi ZTest.
    /// </summary>
    private static void ApplyTextOnTop(Material m)
    {
        string n = m.shader != null ? m.shader.name : "";
        if (!string.IsNullOrEmpty(n) && !n.EndsWith(" Overlay"))
        {
            Shader overlay = Shader.Find(n + " Overlay");
            if (overlay != null)
            {
                int srcQueue = m.renderQueue;   // queue gốc (≈3000 Transparent)
                m.shader = overlay;             // ZTest Always
                m.renderQueue = srcQueue;       // GIỮ queue gốc → không nhảy lên 4000 → mask/thứ tự vẽ không lỗi
                return;
            }
        }
        // shader tùy biến không có biến thể "Overlay" → chỉ thử property ZTest (an toàn), KHÔNG đổi shader.
        SetZTestAlways(m);
    }

    private void WarnNoShader() =>
        Debug.LogWarning("[VRUIRenderOnTop] Không tìm thấy shader 'UI/Overlay Always On Top' (thêm vào Always Included Shaders nếu chạy build).", this);

    private void OnDestroy() => Revert();
}
