#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Dev tool: chọn các asset PlayerCarLoadout / PlayerInventory trong Project rồi chạy
/// menu "Tools/Furia Rush/Reset" để reset chúng.
///  - Loadout  → xoá part đã lắp (giữ tên/icon/giá/mô tả/stats/paint).
///  - Inventory → xoá đồ + xe đã sở hữu (giữ gold).
/// </summary>
public static class GarageResetTools
{
    [MenuItem("Tools/Furia Rush/Reset")]
    public static void Reset()
    {
        PlayerCarLoadout[] loadouts = Selection.GetFiltered<PlayerCarLoadout>(SelectionMode.Assets);
        PlayerInventory[] inventories = Selection.GetFiltered<PlayerInventory>(SelectionMode.Assets);

        if (loadouts.Length == 0 && inventories.Length == 0)
        {
            EditorUtility.DisplayDialog("Reset",
                "Chưa chọn PlayerCarLoadout / PlayerInventory nào.\n" +
                "Chọn (Ctrl/Shift để chọn nhiều) các asset cần reset trong Project rồi chạy lại.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Reset",
                $"Reset {loadouts.Length} loadout (chỉ part) + {inventories.Length} inventory (giữ gold)?\n" +
                "Tên / icon / giá / mô tả / stats / paint được GIỮ NGUYÊN.",
                "Reset", "Cancel"))
            return;

        foreach (PlayerCarLoadout loadout in loadouts) loadout.ResetParts();
        foreach (PlayerInventory inv in inventories) inv.ResetInventory();

        AssetDatabase.SaveAssets();
        Debug.Log($"[GarageResetTools] Reset {loadouts.Length} loadout(s) + {inventories.Length} inventory(ies).");
    }

    [MenuItem("Tools/Furia Rush/Start New Game")]
    public static void StartNewGame()
    {
        CarPart stockTire = FindByName<CarPart>("Tires_Stock");
        CarPart normalBrake = FindByName<CarPart>("Brakes_Normal");
        PlayerCarLoadout car0 = FindByName<PlayerCarLoadout>("Loadout_CarType0");

        if (stockTire == null || normalBrake == null || car0 == null)
        {
            EditorUtility.DisplayDialog("Start New Game",
                $"Thiếu asset:\n- Tires_Stock: {(stockTire != null ? "OK" : "MISSING")}\n" +
                $"- Brakes_Normal: {(normalBrake != null ? "OK" : "MISSING")}\n" +
                $"- Loadout_CarType0: {(car0 != null ? "OK" : "MISSING")}", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Start New Game",
                "Reset TẤT CẢ về trạng thái NEW GAME?\n" +
                "- 3 loadout: wheels = Tires_Stock ×4, brakes = Brakes_Normal ×4 (engine/suspension/ecu/paint xoá).\n" +
                "- Inventory: chỉ sở hữu Loadout_CarType0, gold = 100, xoá hết đồ khác.",
                "Start New Game", "Cancel"))
            return;

        // Mọi loadout: chỉ còn stock tires + normal brakes.
        int loadoutCount = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:PlayerCarLoadout"))
        {
            var lo = AssetDatabase.LoadAssetAtPath<PlayerCarLoadout>(AssetDatabase.GUIDToAssetPath(guid));
            if (lo == null) continue;
            lo.engine = null;
            lo.suspension = null;
            lo.ecu = null;
            lo.paint = null;
            lo.wheels = new List<CarPart> { stockTire, stockTire, stockTire, stockTire };
            lo.brakes = new List<CarPart> { normalBrake, normalBrake, normalBrake, normalBrake };
            EditorUtility.SetDirty(lo);
            loadoutCount++;
        }

        // Mọi inventory: xoá sạch + gold 100 + sở hữu xe khởi đầu (CarType0).
        int invCount = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:PlayerInventory"))
        {
            var inv = AssetDatabase.LoadAssetAtPath<PlayerInventory>(AssetDatabase.GUIDToAssetPath(guid));
            if (inv == null) continue;
            inv.ResetInventory(100);          // clear all owned + gold = 100
            inv.ownedLoadouts.Add(car0);      // own starter car
            EditorUtility.SetDirty(inv);
            invCount++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[GarageResetTools] New game applied: {loadoutCount} loadout(s) (stock tires + normal brakes), {invCount} inventory (CarType0 + 100g).");
    }

    private static T FindByName<T>(string assetName) where T : Object
    {
        foreach (string guid in AssetDatabase.FindAssets($"{assetName} t:{typeof(T).Name}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == assetName)
                return AssetDatabase.LoadAssetAtPath<T>(path);
        }
        return null;
    }
}
#endif
