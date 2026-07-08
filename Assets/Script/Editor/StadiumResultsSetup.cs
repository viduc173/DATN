using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Michsky.UI.Heat;

/// <summary>
/// Menu setup cho bảng kết quả Stadium. Idempotent — bấm nhiều lần không nhân bản thừa.
/// Làm các việc:
///   1. Tạo (nếu chưa có) RaceSettings_Stadium.asset cạnh scene + gán vào RacePositionTracker.
///   2. Duplicate Rank_5 -> Rank_6..Rank_9 dưới cùng Layout Group, gán icon số 6..9.
///   3. Đặt title placeholder cho Rank_1..9 (runtime sẽ ghi đè bằng kết quả thật).
///   4. Thêm RaceResultsController (lên GameObject của tracker) + wire refs:
///      tracker, ModalWindowManager "Achievements", PlayerInventory asset, Layout Group container.
/// Sau khi chạy: tự SaveAssets; scene được đánh dấu dirty — nhớ Ctrl+S.
/// </summary>
public static class StadiumResultsSetup
{
    private const string SettingsAssetPath = "Assets/Scenes/6_Stadium_Sunny/RaceSettings_Stadium.asset";
    private const string NumberSpriteFolder = "Assets/2D/Number";
    // ModalWindowManager.OpenWindow() chạy state "In"/"Out". Panel Achievements mặc định dùng
    // MainPanel.controller (state "Panel In"/"Panel Out") nên Play("In") không khớp -> alpha CanvasGroup
    // kẹt ở 0 -> panel không hiện. Gán controller này (có state In/Out) để animation chạy đúng.
    private const string ModalControllerPath =
        "Assets/Heat - Complete Modern UI/Animations/UI Elements/Modal Window/ModalWindow.controller";
    private const int FirstNewRank = 6;
    private const int LastRank = 12;
    private const string PlaceholderTitle = "---";

    [MenuItem("Tools/Stadium/Setup Results Board")]
    public static void SetupResultsBoard()
    {
        RacePositionTracker tracker = Object.FindObjectOfType<RacePositionTracker>(true);
        if (tracker == null)
        {
            EditorUtility.DisplayDialog("Stadium Setup",
                "Không tìm thấy RacePositionTracker trong scene đang mở. Hãy mở scene Stadium_Sunny rồi chạy lại.",
                "OK");
            return;
        }

        // 1) RaceSettings asset ------------------------------------------------------
        RaceSettings settings = EnsureSettingsAsset(tracker);
        Undo.RecordObject(tracker, "Stadium Setup");
        tracker.raceSettings = settings;
        EditorUtility.SetDirty(tracker);

        // 2) Tìm Rank_5 + container --------------------------------------------------
        AchievementItem rank5 = FindRankItem("Rank_5");
        if (rank5 == null)
        {
            EditorUtility.DisplayDialog("Stadium Setup",
                "Không tìm thấy GameObject 'Rank_5' (có AchievementItem). Kiểm tra lại hierarchy Achievements > ... > Layout Group.",
                "OK");
            return;
        }
        Transform container = rank5.transform.parent;

        // 3) Duplicate Rank_6..Rank_9 ------------------------------------------------
        for (int n = FirstNewRank; n <= LastRank; n++)
        {
            string rankName = "Rank_" + n;
            AchievementItem row = FindRankItemUnder(container, rankName);
            if (row == null)
            {
                GameObject copy = Object.Instantiate(rank5.gameObject, container, false);
                copy.name = rankName;
                copy.transform.SetAsLastSibling();
                Undo.RegisterCreatedObjectUndo(copy, "Stadium Setup");
                row = copy.GetComponent<AchievementItem>();
            }

            AssignIcon(row, n);
        }

        // 4) Title placeholder cho tất cả Rank_1..9 ----------------------------------
        for (int n = 1; n <= LastRank; n++)
        {
            AchievementItem row = FindRankItemUnder(container, "Rank_" + n);
            if (row != null && row.titleObj != null)
            {
                Undo.RecordObject(row.titleObj, "Stadium Setup");
                row.titleObj.text = PlaceholderTitle;
                EditorUtility.SetDirty(row.titleObj);
            }
        }

        // 5) RaceResultsController + wiring ------------------------------------------
        ModalWindowManager window = FindAchievementsWindow();
        bool animatorFixed = FixModalAnimator(window);
        PlayerInventory inventory = LoadFirstAsset<PlayerInventory>("t:PlayerInventory");

        RaceResultsController controller = tracker.GetComponent<RaceResultsController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<RaceResultsController>(tracker.gameObject);
        }

        var so = new SerializedObject(controller);
        SetRef(so, "tracker", tracker);
        SetRef(so, "achievementsWindow", window);
        SetRef(so, "inventory", inventory);
        SetRef(so, "rankBoardContainer", container);
        SetRef(so, "raceSettings", settings);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(controller);

        // 6) Save --------------------------------------------------------------------
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(tracker.gameObject.scene);

        string warn = "";
        if (window == null) warn += "\n- KHÔNG tìm thấy panel 'Achievements' (ModalWindowManager) — gán tay vào controller.";
        else if (!animatorFixed) warn += "\n- KHÔNG gán được ModalWindow.controller cho Animator panel — kiểm tra panel có Animator + đường dẫn controller.";
        if (inventory == null) warn += "\n- KHÔNG tìm thấy PlayerInventory asset — gán tay vào controller.";

        EditorUtility.DisplayDialog("Stadium Setup",
            "Hoàn tất!\n- RaceSettings_Stadium đã gán vào tracker.\n- Rank_6..9 đã tạo + icon số.\n- RaceResultsController đã wire." +
            warn + "\n\nNhớ Ctrl+S để lưu scene.",
            "OK");
    }

    private static RaceSettings EnsureSettingsAsset(RacePositionTracker tracker)
    {
        RaceSettings existing = AssetDatabase.LoadAssetAtPath<RaceSettings>(SettingsAssetPath);
        if (existing != null) return existing;

        var settings = ScriptableObject.CreateInstance<RaceSettings>();
        settings.totalLaps = Mathf.Max(1, tracker.totalLaps);
        settings.autoLoadSceneOnFinish = false; // mở Achievements thay vì auto-load
        settings.endSceneName = "GarageLobby_pc";
        settings.loadSceneDelay = 3f;
        settings.lapWrapThreshold = 0;
        settings.prizeByPosition = new[] { 1000, 600, 300 };

        Directory.CreateDirectory(Path.GetDirectoryName(SettingsAssetPath));
        AssetDatabase.CreateAsset(settings, SettingsAssetPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[StadiumResultsSetup] Đã tạo {SettingsAssetPath}");
        return settings;
    }

    /// <summary>
    /// Gán ModalWindow.controller (có state "In"/"Out") cho Animator của panel Achievements
    /// để ModalWindowManager.OpenWindow() chạy được animation (fade alpha 0->1).
    /// </summary>
    private static bool FixModalAnimator(ModalWindowManager window)
    {
        if (window == null) return false;

        Animator anim = window.GetComponent<Animator>();
        if (anim == null) return false;

        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ModalControllerPath);
        if (ctrl == null)
        {
            Debug.LogWarning($"[StadiumResultsSetup] Không load được controller '{ModalControllerPath}'.");
            return false;
        }

        if (anim.runtimeAnimatorController != ctrl)
        {
            Undo.RecordObject(anim, "Stadium Setup");
            anim.runtimeAnimatorController = ctrl;
            EditorUtility.SetDirty(anim);
            Debug.Log("[StadiumResultsSetup] Đã gán ModalWindow.controller cho Animator panel Achievements.");
        }
        return true;
    }

    private static void AssignIcon(AchievementItem row, int number)
    {
        if (row == null || row.iconObj == null) return;

        string path = $"{NumberSpriteFolder}/{number}.png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogWarning($"[StadiumResultsSetup] Không load được sprite '{path}'.");
            return;
        }

        Undo.RecordObject(row.iconObj, "Stadium Setup");
        row.iconObj.sprite = sprite;
        EditorUtility.SetDirty(row.iconObj);
    }

    private static AchievementItem FindRankItem(string rankName)
    {
        foreach (AchievementItem item in Object.FindObjectsOfType<AchievementItem>(true))
            if (item.gameObject.name == rankName)
                return item;
        return null;
    }

    private static AchievementItem FindRankItemUnder(Transform container, string rankName)
    {
        foreach (Transform child in container)
            if (child.name == rankName)
                return child.GetComponent<AchievementItem>();
        return null;
    }

    private static ModalWindowManager FindAchievementsWindow()
    {
        foreach (ModalWindowManager mw in Object.FindObjectsOfType<ModalWindowManager>(true))
            if (mw.gameObject.name == "Achievements")
                return mw;
        // fallback: bất kỳ ModalWindowManager nào nếu chỉ có 1
        ModalWindowManager[] all = Object.FindObjectsOfType<ModalWindowManager>(true);
        return all.Length == 1 ? all[0] : null;
    }

    private static T LoadFirstAsset<T>(string filter) where T : Object
    {
        string[] guids = AssetDatabase.FindAssets(filter);
        if (guids.Length == 0) return null;
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static void SetRef(SerializedObject so, string propName, Object value)
    {
        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null) prop.objectReferenceValue = value;
    }
}
