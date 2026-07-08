using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor-only repair pass for garage scene references after GUID/meta churn.
/// It only rebinds CarLoadoutSlot.loadout and never fills default parts.
/// </summary>
public static class GarageDataAutoRepair
{
    private const string LoadoutsFolder = "Assets/Data/Loadouts";

    [MenuItem("Tools/Garage/Repair Data References")]
    public static void RepairGarageData()
    {
        AssetDatabase.ImportAsset(LoadoutsFolder, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
        RepairOpenSceneLoadoutSlots();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void RepairOpenSceneLoadoutSlots()
    {
        CarLoadoutSlot[] slots = Object.FindObjectsByType<CarLoadoutSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (CarLoadoutSlot slot in slots)
        {
            if (slot == null)
                continue;

            string path = $"{LoadoutsFolder}/Loadout_{slot.gameObject.name}.asset";
            PlayerCarLoadout loadout = AssetDatabase.LoadAssetAtPath<PlayerCarLoadout>(path);
            if (loadout == null || slot.loadout == loadout)
                continue;

            slot.loadout = loadout;
            EditorUtility.SetDirty(slot);
            EditorSceneManager.MarkSceneDirty(slot.gameObject.scene);
            Debug.Log($"[GarageDataAutoRepair] Rebound {slot.name}.loadout -> {path}", slot);
        }
    }
}
