using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global player inventory asset.
/// Engine/Suspension/ECU are permanent unlocks.
/// Wheels/Brakes are limited quantities: equipping consumes one, unequipping returns one.
/// </summary>
[CreateAssetMenu(fileName = "PlayerInventory", menuName = "Race/Player Inventory", order = 3)]
public class PlayerInventory : ScriptableObject
{
    [Serializable]
    public class PartStack
    {
        public CarPart part;
        [Min(0)] public int quantity = 1;
    }

    [Header("Permanent Unlocks")]
    public List<CarPart> ownedEngines = new List<CarPart>();
    public List<CarPart> ownedSuspensions = new List<CarPart>();
    public List<CarPart> ownedEcus = new List<CarPart>();
    public List<CarPart> ownedOtherParts = new List<CarPart>();

    [Header("Limited Inventory")]
    [Tooltip("Available wheel quantities. Installing one wheel consumes 1 quantity.")]
    public List<PartStack> ownedWheels = new List<PartStack>();

    [Tooltip("Available brake quantities. Installing one brake consumes 1 quantity.")]
    public List<PartStack> ownedBrakes = new List<PartStack>();

    [HideInInspector]
    [Tooltip("Legacy flat list. Kept only to migrate old PlayerInventory assets.")]
    public List<CarPart> ownedParts = new List<CarPart>();

    [Header("Cars (loadouts)")]
    [Tooltip("Các xe (PlayerCarLoadout) người chơi đã sở hữu. Mua xe trong shop = thêm vào đây.")]
    public List<PlayerCarLoadout> ownedLoadouts = new List<PlayerCarLoadout>();

    [Header("Currency")]
    [Min(0)] public int gold = 0;

    private const string PrefKey = "PlayerInventory_v3";

    private void OnEnable()
    {
        EnsureLists();
        MigrateLegacyOwnedParts();
    }

    public bool OwnsPart(CarPart part)
    {
        if (part == null) return false;

        switch (part.slot)
        {
            case CarPart.PartSlot.Engine: return ownedEngines.Contains(part);
            case CarPart.PartSlot.Suspension: return ownedSuspensions.Contains(part);
            case CarPart.PartSlot.ECU: return ownedEcus.Contains(part);
            case CarPart.PartSlot.Wheels:
            case CarPart.PartSlot.Brakes:
                return GetAvailableQuantity(part) > 0;
            default:
                return ownedOtherParts.Contains(part);
        }
    }

    public int GetAvailableQuantity(CarPart part)
    {
        if (part == null) return 0;

        List<PartStack> stacks = GetStackList(part.slot);
        if (stacks == null) return OwnsPermanentPart(part) ? 1 : 0;

        PartStack stack = FindStack(stacks, part);
        return stack != null ? stack.quantity : 0;
    }

    public List<CarPart> GetOwnedBySlot(CarPart.PartSlot slot)
    {
        if (slot == CarPart.PartSlot.Wheels || slot == CarPart.PartSlot.Brakes)
            return GetAvailablePartsFromStacks(GetStackList(slot));

        return new List<CarPart>(GetPermanentList(slot));
    }

    public List<CarPart> GetAllOwnedParts()
    {
        var result = new List<CarPart>();
        AddRangeUnique(result, ownedEngines);
        AddRangeUnique(result, ownedSuspensions);
        AddRangeUnique(result, ownedEcus);
        AddPartsFromStacks(result, ownedWheels);
        AddPartsFromStacks(result, ownedBrakes);
        AddRangeUnique(result, ownedOtherParts);
        return result;
    }

    public bool TryBuyPart(CarPart part)
    {
        if (part == null) return false;
        if (IsPermanentSlot(part.slot) && OwnsPermanentPart(part)) return false;
        if (gold < part.costGold) return false;

        gold -= part.costGold;
        AddOwnedPart(part, UnitsPerPurchase(part));
        MarkDirty();

        Debug.Log($"[PlayerInventory] Bought: {part.partName} (-{part.costGold}g). Gold left: {gold}g");
        return true;
    }

    /// <summary>
    /// How many inventory units one purchase grants. Brakes are sold as a set of 2 calipers
    /// (left + right), each counted individually; everything else is 1.
    /// </summary>
    private static int UnitsPerPurchase(CarPart part)
        => part != null && part.slot == CarPart.PartSlot.Brakes ? 2 : 1;

    // ── Cars (loadouts) ──────────────────────────────────────────────────────────

    public bool OwnsLoadout(PlayerCarLoadout loadout)
        => loadout != null && ownedLoadouts != null && ownedLoadouts.Contains(loadout);

    /// <summary>Buy a car (loadout): one-time, deducts gold, adds to ownedLoadouts.</summary>
    public bool TryBuyLoadout(PlayerCarLoadout loadout)
    {
        if (loadout == null) return false;
        ownedLoadouts ??= new List<PlayerCarLoadout>();
        if (ownedLoadouts.Contains(loadout)) return false;
        if (gold < loadout.costGold) return false;

        gold -= loadout.costGold;
        ownedLoadouts.Add(loadout);
        MarkDirty();

        Debug.Log($"[PlayerInventory] Bought car: {loadout.loadoutName} (-{loadout.costGold}g). Gold left: {gold}g");
        return true;
    }

    public bool UnlockPart(CarPart part)
    {
        if (part == null) return false;
        if (IsPermanentSlot(part.slot) && OwnsPermanentPart(part)) return false;

        AddOwnedPart(part, 1);
        MarkDirty();
        return true;
    }

    public bool TryConsumePart(CarPart part)
    {
        if (part == null) return false;

        List<PartStack> stacks = GetStackList(part.slot);
        if (stacks == null)
            return OwnsPermanentPart(part);

        PartStack stack = FindStack(stacks, part);
        if (stack == null || stack.quantity <= 0) return false;

        stack.quantity--;
        MarkDirty();
        return true;
    }

    public void ReturnPart(CarPart part)
    {
        if (part == null) return;

        List<PartStack> stacks = GetStackList(part.slot);
        if (stacks == null) return;

        AddToStack(stacks, part, 1);
        MarkDirty();
    }

    public void AddGold(int amount)
    {
        gold = Mathf.Max(0, gold + amount);
        MarkDirty();
    }

    public void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetInt(PrefKey + "_gold", gold);
        PlayerPrefs.SetString(PrefKey + "_engines", JoinNames(ownedEngines));
        PlayerPrefs.SetString(PrefKey + "_suspensions", JoinNames(ownedSuspensions));
        PlayerPrefs.SetString(PrefKey + "_ecus", JoinNames(ownedEcus));
        PlayerPrefs.SetString(PrefKey + "_wheels", JoinStacks(ownedWheels));
        PlayerPrefs.SetString(PrefKey + "_brakes", JoinStacks(ownedBrakes));
        PlayerPrefs.Save();
    }

    public void LoadFromPlayerPrefs(IEnumerable<CarPart> catalog)
    {
        if (!PlayerPrefs.HasKey(PrefKey + "_gold")) return;

        ClearOwnedParts();
        gold = PlayerPrefs.GetInt(PrefKey + "_gold", 0);

        Dictionary<string, CarPart> partsByName = BuildCatalog(catalog);
        LoadPermanentList(PlayerPrefs.GetString(PrefKey + "_engines", ""), ownedEngines, partsByName);
        LoadPermanentList(PlayerPrefs.GetString(PrefKey + "_suspensions", ""), ownedSuspensions, partsByName);
        LoadPermanentList(PlayerPrefs.GetString(PrefKey + "_ecus", ""), ownedEcus, partsByName);
        LoadStackList(PlayerPrefs.GetString(PrefKey + "_wheels", ""), ownedWheels, partsByName);
        LoadStackList(PlayerPrefs.GetString(PrefKey + "_brakes", ""), ownedBrakes, partsByName);

        MarkDirty();
    }

    private void AddOwnedPart(CarPart part, int quantity)
    {
        if (part == null || quantity <= 0) return;

        List<PartStack> stacks = GetStackList(part.slot);
        if (stacks != null)
        {
            AddToStack(stacks, part, quantity);
            return;
        }

        List<CarPart> list = GetPermanentList(part.slot);
        if (!list.Contains(part))
            list.Add(part);
    }

    private bool OwnsPermanentPart(CarPart part)
    {
        if (part == null) return false;
        return GetStackList(part.slot) == null && GetPermanentList(part.slot).Contains(part);
    }

    private List<CarPart> GetPermanentList(CarPart.PartSlot slot)
    {
        EnsureLists();
        switch (slot)
        {
            case CarPart.PartSlot.Engine: return ownedEngines;
            case CarPart.PartSlot.Suspension: return ownedSuspensions;
            case CarPart.PartSlot.ECU: return ownedEcus;
            default: return ownedOtherParts;
        }
    }

    private List<PartStack> GetStackList(CarPart.PartSlot slot)
    {
        EnsureLists();
        switch (slot)
        {
            case CarPart.PartSlot.Wheels: return ownedWheels;
            case CarPart.PartSlot.Brakes: return ownedBrakes;
            default: return null;
        }
    }

    private static bool IsPermanentSlot(CarPart.PartSlot slot)
        => slot != CarPart.PartSlot.Wheels && slot != CarPart.PartSlot.Brakes;

    private void ClearOwnedParts()
    {
        EnsureLists();
        ownedEngines.Clear();
        ownedSuspensions.Clear();
        ownedEcus.Clear();
        ownedOtherParts.Clear();
        ownedWheels.Clear();
        ownedBrakes.Clear();
        ownedParts.Clear();
        ownedLoadouts.Clear();
    }

    private void MigrateLegacyOwnedParts()
    {
        if (ownedParts == null || ownedParts.Count == 0) return;

        bool changed = false;
        foreach (CarPart part in ownedParts)
        {
            if (part == null) continue;
            AddOwnedPart(part, 1);
            changed = true;
        }

        ownedParts.Clear();

        if (changed)
            MarkDirty();
    }

    private void EnsureLists()
    {
        ownedEngines ??= new List<CarPart>();
        ownedSuspensions ??= new List<CarPart>();
        ownedEcus ??= new List<CarPart>();
        ownedOtherParts ??= new List<CarPart>();
        ownedWheels ??= new List<PartStack>();
        ownedBrakes ??= new List<PartStack>();
        ownedParts ??= new List<CarPart>();
        ownedLoadouts ??= new List<PlayerCarLoadout>();
    }

    private static void AddToStack(List<PartStack> stacks, CarPart part, int quantity)
    {
        PartStack stack = FindStack(stacks, part);
        if (stack == null)
        {
            stacks.Add(new PartStack { part = part, quantity = quantity });
            return;
        }

        stack.quantity = Mathf.Max(0, stack.quantity + quantity);
    }

    private static PartStack FindStack(List<PartStack> stacks, CarPart part)
    {
        if (stacks == null) return null;
        return stacks.Find(s => s != null && s.part == part);
    }

    private static List<CarPart> GetAvailablePartsFromStacks(List<PartStack> stacks)
    {
        var result = new List<CarPart>();
        if (stacks == null) return result;

        foreach (PartStack stack in stacks)
            if (stack != null && stack.part != null && stack.quantity > 0 && !result.Contains(stack.part))
                result.Add(stack.part);

        return result;
    }

    private static void AddRangeUnique(List<CarPart> target, List<CarPart> source)
    {
        if (source == null) return;

        foreach (CarPart part in source)
            if (part != null && !target.Contains(part))
                target.Add(part);
    }

    private static void AddPartsFromStacks(List<CarPart> target, List<PartStack> stacks)
    {
        if (stacks == null) return;

        foreach (PartStack stack in stacks)
            if (stack != null && stack.part != null && stack.quantity > 0 && !target.Contains(stack.part))
                target.Add(stack.part);
    }

    private static string JoinNames(List<CarPart> parts)
    {
        var names = new List<string>();
        if (parts != null)
            foreach (CarPart part in parts)
                if (part != null) names.Add(part.partName);

        return string.Join("|", names);
    }

    private static string JoinStacks(List<PartStack> stacks)
    {
        var entries = new List<string>();
        if (stacks != null)
            foreach (PartStack stack in stacks)
                if (stack != null && stack.part != null && stack.quantity > 0)
                    entries.Add($"{stack.part.partName}:{stack.quantity}");

        return string.Join("|", entries);
    }

    private static Dictionary<string, CarPart> BuildCatalog(IEnumerable<CarPart> catalog)
    {
        var result = new Dictionary<string, CarPart>();
        if (catalog == null) return result;

        foreach (CarPart part in catalog)
            if (part != null && !result.ContainsKey(part.partName))
                result.Add(part.partName, part);
        return result;
    }

    private static void LoadPermanentList(string raw, List<CarPart> target, Dictionary<string, CarPart> partsByName)
    {
        if (string.IsNullOrEmpty(raw)) return;

        foreach (string name in raw.Split('|'))
            if (partsByName.TryGetValue(name, out CarPart part) && !target.Contains(part))
                target.Add(part);
    }

    private static void LoadStackList(string raw, List<PartStack> target, Dictionary<string, CarPart> partsByName)
    {
        if (string.IsNullOrEmpty(raw)) return;

        foreach (string entry in raw.Split('|'))
        {
            string[] parts = entry.Split(':');
            if (parts.Length != 2) continue;
            if (!partsByName.TryGetValue(parts[0], out CarPart part)) continue;
            if (!int.TryParse(parts[1], out int quantity)) continue;

            AddToStack(target, part, quantity);
        }
    }

    private void MarkDirty()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    /// <summary>
    /// Xoá toàn bộ đồ đã sở hữu (engines/suspensions/ecus/wheels/brakes/other + cars).
    /// GIỮ NGUYÊN gold. Truyền resetGoldTo &gt;= 0 để đặt lại gold (mặc định -1 = giữ).
    /// </summary>
    public void ResetInventory(int resetGoldTo = -1)
    {
        ClearOwnedParts();
        if (resetGoldTo >= 0) gold = resetGoldTo;
        MarkDirty();
        Debug.Log($"[PlayerInventory] Inventory reset (owned parts + cars cleared). Gold: {gold}g");
    }

#if UNITY_EDITOR
    [ContextMenu("Add Gold +500 (Debug)")]
    private void DbgAddGold() => AddGold(500);

    [ContextMenu("Log Inventory")]
    private void DbgLog()
    {
        Debug.Log($"[PlayerInventory] Gold: {gold}g");
        foreach (CarPart part in ownedEngines) Debug.Log($"  [Engine] {part?.partName}");
        foreach (CarPart part in ownedSuspensions) Debug.Log($"  [Suspension] {part?.partName}");
        foreach (CarPart part in ownedEcus) Debug.Log($"  [ECU] {part?.partName}");
        foreach (PartStack stack in ownedWheels) Debug.Log($"  [Wheels] {stack?.part?.partName} x{stack?.quantity}");
        foreach (PartStack stack in ownedBrakes) Debug.Log($"  [Brakes] {stack?.part?.partName} x{stack?.quantity}");
    }
#endif
}
