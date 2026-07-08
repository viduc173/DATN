using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using Michsky.UI.Heat;

/// <summary>
/// Công cụ 1-click chuyển <c>UI_Main Menu</c> (UGUI 2D Screen-Space) sang UI VR bấm được bằng tia,
/// theo đúng quy trình <c>Assets/Scenes/Design/Convert_3D_UI_to_VR_UI.md</c> +
/// <c>README_GarageLobby_VR.md</c>. Thao tác TRỰC TIẾP trên scene đang mở (không sửa YAML tay → không
/// lệ thuộc fileID), an toàn + tái dùng cho mọi scene.
///
/// Việc nó làm (idempotent — chạy lại không nhân đôi):
///   1. Canvas <c>UI_Main Menu</c> → World Space; RectTransform có sizeDelta hợp lệ (= reference resolution),
///      pivot giữa, scale nhỏ; CanvasScaler Dynamic Pixels Per Unit ≥ 3 (rõ nét).
///   2. Thêm <see cref="TrackedDeviceGraphicRaycaster"/> (giữ GraphicRaycaster cũ cho chuột PC).
///   3. Thêm <see cref="VRWorldSpaceMenu"/> (đặt/orbit-bám trước mặt) + <see cref="VRUIRenderOnTop"/> (đè vật cản).
///   4. EventSystem → thay input module cũ bằng <see cref="XRUIInputModule"/>.
///   5. Mỗi <c>RightHand Controller</c> (scene đua có 3 rig theo CarType) → thêm GO con <c>Ray</c>
///      (<see cref="XRRayInteractor"/> + LineRenderer + <see cref="XRInteractorLineVisual"/>, tia luôn hiện).
///   6. Điều khiển mở UI bằng nút tay (lên EventSystem — luôn active), TỰ chọn theo loại scene:
///      • Scene ĐUA (có <see cref="RaceResultsController"/>): UI_Main Menu là container chứa modal (bảng kết
///        quả tự mở + modal Exit/Pause) → GIỮ canvas active, thêm <see cref="VRRaceMenu"/> (A/X mở modal Exit
///        thay phím Tab, tự hiện tia khi có modal). Gỡ VRMenuToggle nếu lỡ có.
///      • Scene MENU thường (vd garage): thêm <see cref="VRMenuToggle"/> (A/X bật/tắt cả canvas) + wire tia.
///   7. Thêm shader <c>UI/Overlay Always On Top</c> vào Always Included Shaders (tránh strip lúc build).
///
/// Dùng: mở scene VR (vd Stadium_Sunny_vr) → menu <b>Tools/VR/Convert Main Menu to VR</b> → Ctrl+S.
/// </summary>
public static class VRMainMenuConverter
{
    // Tên canvas menu cần tìm (theo thứ tự ưu tiên). Scene splash/main menu dùng "UI_Menu".
    private static readonly string[] MenuNames = { "UI_Main Menu", "UI_Menu" };
    private const string RayName = "Ray";
    private const string RightHandName = "RightHand Controller";
    private const string OverlayShaderName = "UI/Overlay Always On Top";

    [MenuItem("Tools/VR/Convert Main Menu to VR")]
    public static void Convert()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("VR Convert", "Chưa có scene nào được mở.", "OK");
            return;
        }

        List<Transform> all = GatherAll(scene);

        GameObject menuGO = null;
        foreach (var n in MenuNames) { menuGO = FindByName(all, n); if (menuGO != null) break; }
        if (menuGO == null) { Fail($"Không tìm thấy canvas menu ({string.Join(" / ", MenuNames)}) trong scene."); return; }

        Canvas canvas = menuGO.GetComponent<Canvas>();
        if (canvas == null) { Fail($"'{menuGO.name}' không có component Canvas."); return; }

        EventSystem es = FindActiveOrFirst<EventSystem>(all);
        if (es == null) { Fail("Không tìm thấy EventSystem trong scene."); return; }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Convert Main Menu to VR");
        int group = Undo.GetCurrentGroup();
        int changes = 0;

        // 1) Canvas → World Space + RectTransform/CanvasScaler hợp lệ cho world-space ----------------
        Undo.RecordObject(canvas, "Canvas");
        if (canvas.renderMode != RenderMode.WorldSpace) { canvas.renderMode = RenderMode.WorldSpace; changes++; }

        var rt = menuGO.GetComponent<RectTransform>();
        var scaler = menuGO.GetComponent<CanvasScaler>();
        Vector2 refRes = scaler != null ? scaler.referenceResolution : new Vector2(1920, 1280);
        if (refRes.x < 1f || refRes.y < 1f) refRes = new Vector2(1920, 1280);

        if (rt != null)
        {
            Undo.RecordObject(rt, "Canvas RectTransform");
            if (rt.sizeDelta.x < 1f || rt.sizeDelta.y < 1f) { rt.sizeDelta = refRes; changes++; }
            rt.pivot = new Vector2(0.5f, 0.5f);
            if (rt.localScale.x < 1e-5f) rt.localScale = Vector3.one * 0.001f;
        }

        if (scaler != null)
        {
            Undo.RecordObject(scaler, "CanvasScaler");
            if (scaler.dynamicPixelsPerUnit < 3f) { scaler.dynamicPixelsPerUnit = 3f; changes++; }
        }

        // 2) TrackedDeviceGraphicRaycaster (giữ GraphicRaycaster cũ cho chuột PC) ---------------------
        if (menuGO.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
        { Undo.AddComponent<TrackedDeviceGraphicRaycaster>(menuGO); changes++; }

        // 3) VRWorldSpaceMenu + VRUIRenderOnTop -------------------------------------------------------
        var placer = menuGO.GetComponent<VRWorldSpaceMenu>();
        if (placer == null) { placer = Undo.AddComponent<VRWorldSpaceMenu>(menuGO); changes++; }
        if (menuGO.GetComponent<VRUIRenderOnTop>() == null)
        { Undo.AddComponent<VRUIRenderOnTop>(menuGO); changes++; }

        // 4) EventSystem → XRUIInputModule -----------------------------------------------------------
        GameObject esGO = es.gameObject;

        // EventSystem PHẢI active mới xử lý được input (tia/UI). Nếu bị tắt sẵn trong scene → bật lên,
        // nếu không thì XRUIInputModule + VRRaceMenu nằm trên object chết, bấm gì cũng không ăn.
        if (!esGO.activeSelf)
        {
            Undo.RecordObject(esGO, "Activate EventSystem");
            esGO.SetActive(true);
            changes++;
            Debug.Log("[VRMainMenuConverter] EventSystem đang INACTIVE → đã bật active (bắt buộc để xử lý input).");
        }

        // Chỉ giữ DUY NHẤT XRUIInputModule. 2 input module trên 1 EventSystem (vd còn InputSystemUIInputModule /
        // StandaloneInputModule) sẽ tranh nhau → tia bấm KHÔNG ăn nút. Gỡ hết module non-XR.
        if (esGO.GetComponent<XRUIInputModule>() == null)
            Undo.AddComponent<XRUIInputModule>(esGO);
        foreach (var module in esGO.GetComponents<BaseInputModule>())
        {
            if (module is XRUIInputModule) continue;
            Debug.Log($"[VRMainMenuConverter] Gỡ input module cũ '{module.GetType().Name}' khỏi EventSystem.");
            Undo.DestroyObjectImmediate(module);
            changes++;
        }
        // Fallback an toàn: nếu còn sót module non-XR (không gỡ được) → tắt enabled để không xung đột.
        foreach (var module in esGO.GetComponents<BaseInputModule>())
            if (!(module is XRUIInputModule) && module.enabled)
            { Undo.RecordObject(module, "Disable input module"); module.enabled = false; changes++; }

        // 5) Ray trên từng RightHand Controller (3 rig CarType) ---------------------------------------
        List<GameObject> rightHands = FindAllByName(all, RightHandName);
        var rays = new List<GameObject>();
        foreach (var rh in rightHands)
        {
            GameObject ray = FindDirectChild(rh.transform, RayName);
            if (ray == null)
            {
                ray = new GameObject(RayName);
                Undo.RegisterCreatedObjectUndo(ray, "Create Ray");
                ray.transform.SetParent(rh.transform, false);
                ray.transform.localPosition = Vector3.zero;
                ray.transform.localRotation = Quaternion.identity;
                ray.transform.localScale = Vector3.one;
                ConfigureRay(ray);
                changes++;
            }
            rays.Add(ray);
        }
        if (rays.Count == 0)
            Debug.LogWarning($"[VRMainMenuConverter] Không tìm thấy '{RightHandName}' nào — tia chưa được gắn. " +
                             "Kiểm tra rig XR trong scene.");

        // 6) Điều khiển mở/ẩn UI bằng nút tay ---------------------------------------------------------
        // Scene ĐUA (có RaceResultsController): UI_Main Menu là CONTAINER chứa modal (bảng kết quả tự mở +
        //   modal Exit/Pause). Phải LUÔN ACTIVE — tắt cả canvas là mất luôn modal. Dùng VRRaceMenu:
        //   nút tay mở modal Exit (thay phím Tab/PCHotkeyManager), tự hiện tia khi có modal mở.
        // Scene MENU thường (vd garage): UI_Main Menu chính là menu → VRMenuToggle (ẩn/hiện cả canvas).
        bool isRaceUI = FindComponent<RaceResultsController>(all) != null;
        string mode;
        if (isRaceUI)
        {
            // Gỡ VRMenuToggle nếu lỡ có (nó sẽ tắt cả canvas → mất modal kết quả/exit).
            var oldToggle = esGO.GetComponent<VRMenuToggle>();
            if (oldToggle != null) { Undo.DestroyObjectImmediate(oldToggle); changes++; }

            var raceMenu = esGO.GetComponent<VRRaceMenu>();
            if (raceMenu == null) { raceMenu = Undo.AddComponent<VRRaceMenu>(esGO); changes++; }
            var so = new SerializedObject(raceMenu);
            SetObjRef(so, "placer", placer);
            SetObjRef(so, "modalRoot", menuGO.transform);
            SetObjRef(so, "exitModal", GuessExitModal(menuGO, all));
            SetRaysArray(so, "rayObjects", rays);
            so.ApplyModifiedProperties();
            mode = "VRRaceMenu (UI_Main Menu giữ active; A/X mở modal Exit)";
        }
        else
        {
            var toggle = esGO.GetComponent<VRMenuToggle>();
            if (toggle == null) { toggle = Undo.AddComponent<VRMenuToggle>(esGO); changes++; }
            var so = new SerializedObject(toggle);
            SetObjRef(so, "menu", menuGO);
            SetObjRef(so, "placer", placer);
            SetRaysArray(so, "rayObjects", rays);
            // Menu đang active sẵn (vd splash/main menu) → hiện ngay từ đầu; menu kiểu popup (tắt sẵn) → ẩn.
            var hide = so.FindProperty("hideOnStart");
            if (hide != null) hide.boolValue = !menuGO.activeInHierarchy;
            so.ApplyModifiedProperties();
            mode = $"VRMenuToggle (A/X bật/tắt '{menuGO.name}', hideOnStart={!menuGO.activeInHierarchy})";
        }

        // 7) Shader overlay vào Always Included Shaders (tránh strip lúc build) ------------------------
        EnsureAlwaysIncludedShader(OverlayShaderName);
        // TMP "... Overlay" — VRUIRenderOnTop đổi material chữ sang biến thể này (ZTest Always) lúc runtime,
        // ép renderQueue về gốc. Build phải có sẵn để Shader.Find không null.
        EnsureAlwaysIncludedShader("TextMeshPro/Distance Field Overlay");
        EnsureAlwaysIncludedShader("TextMeshPro/Mobile/Distance Field Overlay");

        EditorUtility.SetDirty(menuGO);
        EditorUtility.SetDirty(esGO);
        EditorSceneManager.MarkSceneDirty(scene);
        Undo.CollapseUndoOperations(group);

        string msg = $"Đã chuyển '{menuGO.name}' sang VR.\n" +
                     $"• Canvas → World Space + raycaster + VRWorldSpaceMenu/VRUIRenderOnTop\n" +
                     $"• EventSystem → XRUIInputModule\n" +
                     $"• Tia gắn vào {rays.Count} RightHand Controller\n" +
                     $"• Điều khiển: {mode}\n\n" +
                     $"≈{changes} thay đổi. Nhớ Ctrl+S để lưu scene.";
        Debug.Log("[VRMainMenuConverter] " + msg.Replace("\n", " "));
        EditorUtility.DisplayDialog("VR Convert — xong", msg, "OK");
    }

    [MenuItem("Tools/VR/Convert Main Menu to VR", true)]
    private static bool ConvertValidate() => SceneManager.GetActiveScene().isLoaded;

    // ---------------------------------------------------------------- Ray config

    private static void ConfigureRay(GameObject rayGO)
    {
        var ray = rayGO.AddComponent<XRRayInteractor>();   // thêm trước → thoả RequireComponent của LineVisual
        ray.enableUIInteraction = true;
        ray.lineType = XRRayInteractor.LineType.StraightLine;
        ray.maxRaycastDistance = 30f;

        var visual = rayGO.AddComponent<XRInteractorLineVisual>(); // tự kéo theo LineRenderer
        visual.lineLength = 3f;
        visual.overrideInteractorLineLength = true;
        visual.validColorGradient = SolidGradient(new Color(0.30f, 0.80f, 1f, 1f)); // xanh khi trúng nút
        visual.invalidColorGradient = SolidGradient(Color.white);                   // trắng, alpha=1 → tia LUÔN hiện

        var lr = rayGO.GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
            lr.widthMultiplier = 0.005f;
        }
    }

    private static Gradient SolidGradient(Color c)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(c.a, 1f) });
        return g;
    }

    // ---------------------------------------------------------------- Always Included Shaders

    private static void EnsureAlwaysIncludedShader(string shaderName)
    {
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogWarning($"[VRMainMenuConverter] Không tìm thấy shader '{shaderName}' — bỏ qua bước Always Included.");
            return;
        }
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (assets == null || assets.Length == 0) return;

        var so = new SerializedObject(assets[0]);
        var arr = so.FindProperty("m_AlwaysIncludedShaders");
        if (arr == null) return;

        for (int i = 0; i < arr.arraySize; i++)
            if (arr.GetArrayElementAtIndex(i).objectReferenceValue == shader) return; // đã có

        int idx = arr.arraySize;
        arr.InsertArrayElementAtIndex(idx);
        arr.GetArrayElementAtIndex(idx).objectReferenceValue = shader;
        so.ApplyModifiedProperties();
        Debug.Log($"[VRMainMenuConverter] Đã thêm '{shaderName}' vào Always Included Shaders.");
    }

    // ---------------------------------------------------------------- helpers

    private static void SetObjRef(SerializedObject so, string prop, Object value)
    {
        var p = so.FindProperty(prop);
        if (p != null) p.objectReferenceValue = value;
    }

    private static void SetRaysArray(SerializedObject so, string prop, List<GameObject> rays)
    {
        var p = so.FindProperty(prop);
        if (p == null) return;
        p.arraySize = rays.Count;
        for (int i = 0; i < rays.Count; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = rays[i];
    }

    // Đoán modal Exit = ModalWindowManager trong UI_Main Menu KHÁC bảng kết quả (achievementsWindow).
    private static ModalWindowManager GuessExitModal(GameObject menuGO, List<Transform> all)
    {
        var modals = menuGO.GetComponentsInChildren<ModalWindowManager>(true);
        if (modals == null || modals.Length == 0) return null;

        ModalWindowManager results = null;
        var rrc = FindComponent<RaceResultsController>(all);
        if (rrc != null) results = rrc.AchievementsWindow;

        foreach (var m in modals)
            if (m != null && m != results) return m;
        return modals[0];
    }

    private static List<Transform> GatherAll(Scene scene)
    {
        var list = new List<Transform>();
        foreach (var root in scene.GetRootGameObjects())
            list.AddRange(root.GetComponentsInChildren<Transform>(true));
        return list;
    }

    private static GameObject FindByName(List<Transform> all, string name)
        => all.FirstOrDefault(t => t.name == name)?.gameObject;

    private static List<GameObject> FindAllByName(List<Transform> all, string name)
        => all.Where(t => t.name == name).Select(t => t.gameObject).ToList();

    private static T FindComponent<T>(List<Transform> all) where T : Component
    {
        foreach (var t in all) { var c = t.GetComponent<T>(); if (c != null) return c; }
        return null;
    }

    // Ưu tiên component trên GO đang active (vd EventSystem); nếu không có cái nào active → trả cái đầu tiên.
    private static T FindActiveOrFirst<T>(List<Transform> all) where T : Component
    {
        T first = null;
        foreach (var t in all)
        {
            var c = t.GetComponent<T>();
            if (c == null) continue;
            if (t.gameObject.activeInHierarchy) return c;
            if (first == null) first = c;
        }
        return first;
    }

    private static GameObject FindDirectChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name) return parent.GetChild(i).gameObject;
        return null;
    }

    private static void Fail(string reason)
    {
        Debug.LogError("[VRMainMenuConverter] " + reason);
        EditorUtility.DisplayDialog("VR Convert — lỗi", reason, "OK");
    }
}
