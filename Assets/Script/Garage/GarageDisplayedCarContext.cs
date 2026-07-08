using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Bridge for garage UI. It exposes the currently displayed car/loadout and
/// provides purchase/equip methods that UI buttons can call.
/// </summary>
[DefaultExecutionOrder(-30)]
public class GarageDisplayedCarContext : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private ShopCatalog shopCatalog;

    [Header("Garage")]
    [SerializeField] private GarageCarManager carManager;

    [Header("Events")]
    public UnityEvent<CarPart> onPartPurchased;
    public UnityEvent<CarPart> onPartEquipped;
    public UnityEvent<CarPart> onPartUnequipped;
    public UnityEvent<PlayerCarLoadout> onCarBought;
    public UnityEvent onDisplayedCarChanged;

    public PlayerInventory Inventory => playerInventory;
    public ShopCatalog Shop => shopCatalog;

    public GarageCarSlot DisplayedCarSlot => ResolveCarManager()?.ActiveSlot;

    public CarLoadoutSlot DisplayedLoadoutSlot
    {
        get
        {
            GarageCarSlot slot = DisplayedCarSlot;
            return slot != null ? slot.GetComponent<CarLoadoutSlot>() : null;
        }
    }

    public PlayerCarLoadout DisplayedLoadout => DisplayedLoadoutSlot != null
        ? DisplayedLoadoutSlot.loadout
        : ActiveLoadout.Current;

    private void Awake()
    {
        ResolveCarManager();
    }

    private void OnEnable()
    {
        GarageCarManager manager = ResolveCarManager();
        if (manager != null)
            manager.onCarChanged.AddListener(HandleCarChanged);
    }

    private void OnDisable()
    {
        if (carManager != null)
            carManager.onCarChanged.RemoveListener(HandleCarChanged);
    }

    public List<CarPart> GetOwnedParts(CarPart.PartSlot slot)
        => playerInventory != null ? playerInventory.GetOwnedBySlot(slot) : new List<CarPart>();

    public List<CarPart> GetShopParts(CarPart.PartSlot slot)
        => shopCatalog != null ? shopCatalog.GetStockBySlot(slot) : new List<CarPart>();

    public bool BuyPart(CarPart part)
    {
        if (part == null || playerInventory == null) return false;
        if (shopCatalog != null && !shopCatalog.HasStock(part)) return false;

        bool bought = playerInventory.TryBuyPart(part);
        if (!bought) return false;

        onPartPurchased?.Invoke(part);
        return true;
    }

    // ── Cars (loadouts) ──────────────────────────────────────────────────────────

    public List<PlayerCarLoadout> GetShopCars()
        => shopCatalog != null ? new List<PlayerCarLoadout>(shopCatalog.carStock) : new List<PlayerCarLoadout>();

    public bool OwnsLoadout(PlayerCarLoadout loadout)
        => playerInventory != null && playerInventory.OwnsLoadout(loadout);

    public bool BuyLoadout(PlayerCarLoadout loadout)
    {
        if (loadout == null || playerInventory == null) return false;

        bool bought = playerInventory.TryBuyLoadout(loadout);
        if (!bought) return false;

        onCarBought?.Invoke(loadout);
        return true;
    }

    /// <summary>Xe đã sở hữu (để inventory liệt kê + select).</summary>
    public List<PlayerCarLoadout> GetOwnedCars()
        => playerInventory != null ? new List<PlayerCarLoadout>(playerInventory.ownedLoadouts) : new List<PlayerCarLoadout>();

    /// <summary>Chọn 1 xe đã sở hữu làm xe đang hiển thị của màn (đổi active car để gắn part vào).</summary>
    public bool SelectCar(PlayerCarLoadout loadout)
    {
        if (loadout == null || !OwnsLoadout(loadout)) return false;
        GarageCarManager manager = ResolveCarManager();
        return manager != null && manager.SelectByLoadout(loadout);
    }

    public bool EquipOwnedPart(CarPart part)
    {
        if (part == null || playerInventory == null || !playerInventory.OwnsPart(part))
            return false;

        if (IsQuantitySlot(part.slot))
            return false;

        CarLoadoutSlot loadoutSlot = DisplayedLoadoutSlot;
        if (loadoutSlot == null || loadoutSlot.loadout == null)
            return false;

        loadoutSlot.EquipPart(part);
        loadoutSlot.SyncSocketsFromLoadout();
        ActiveLoadout.Current = loadoutSlot.loadout;

        onPartEquipped?.Invoke(part);
        return true;
    }

    public bool UnequipOwnedPart(CarPart part)
    {
        if (part == null || IsQuantitySlot(part.slot))
            return false;

        CarLoadoutSlot loadoutSlot = DisplayedLoadoutSlot;
        if (loadoutSlot == null || loadoutSlot.loadout == null || !loadoutSlot.loadout.HasPart(part))
            return false;

        loadoutSlot.UnequipPart(part);
        loadoutSlot.SyncSocketsFromLoadout();
        ActiveLoadout.Current = loadoutSlot.loadout;

        onPartUnequipped?.Invoke(part);
        return true;
    }

    public bool EquipOwnedWheel(CarPart part, string wheelSocketName)
    {
        if (!CanEquipOwnedSocketPart(part, CarPart.PartSlot.Wheels, wheelSocketName, out CarLoadoutSlot loadoutSlot))
            return false;

        CarPart current = loadoutSlot.GetTireAt(wheelSocketName);
        if (current == part)
            return true;

        if (!playerInventory.TryConsumePart(part))
            return false;

        if (current != null)
            playerInventory.ReturnPart(current);

        loadoutSlot.EquipTires(part, wheelSocketName);
        loadoutSlot.SyncSocketsFromLoadout();
        ActiveLoadout.Current = loadoutSlot.loadout;
        onPartEquipped?.Invoke(part);
        return true;
    }

    public bool EquipOwnedBrake(CarPart part, string brakeSocketName)
    {
        if (!CanEquipOwnedSocketPart(part, CarPart.PartSlot.Brakes, brakeSocketName, out CarLoadoutSlot loadoutSlot))
            return false;

        CarPart current = loadoutSlot.GetBrakeAt(brakeSocketName);
        if (current == part)
            return true;

        if (!playerInventory.TryConsumePart(part))
            return false;

        if (current != null)
            playerInventory.ReturnPart(current);

        loadoutSlot.EquipBrake(part, brakeSocketName);
        loadoutSlot.SyncSocketsFromLoadout();
        ActiveLoadout.Current = loadoutSlot.loadout;
        onPartEquipped?.Invoke(part);
        return true;
    }

    public bool UnequipOwnedWheel(string wheelSocketName)
    {
        CarLoadoutSlot loadoutSlot = DisplayedLoadoutSlot;
        if (loadoutSlot == null || string.IsNullOrWhiteSpace(wheelSocketName))
            return false;

        CarPart current = loadoutSlot.GetTireAt(wheelSocketName);
        if (current == null) return false;

        loadoutSlot.UnequipTires(current, wheelSocketName);
        playerInventory?.ReturnPart(current);
        loadoutSlot.SyncSocketsFromLoadout();
        ActiveLoadout.Current = loadoutSlot.loadout;
        onPartUnequipped?.Invoke(current);
        return true;
    }

    public bool UnequipOwnedBrake(string brakeSocketName)
    {
        CarLoadoutSlot loadoutSlot = DisplayedLoadoutSlot;
        if (loadoutSlot == null || string.IsNullOrWhiteSpace(brakeSocketName))
            return false;

        CarPart current = loadoutSlot.GetBrakeAt(brakeSocketName);
        if (current == null) return false;

        loadoutSlot.UnequipBrake(current, brakeSocketName);
        playerInventory?.ReturnPart(current);
        loadoutSlot.SyncSocketsFromLoadout();
        ActiveLoadout.Current = loadoutSlot.loadout;
        onPartUnequipped?.Invoke(current);
        return true;
    }

    private bool CanEquipOwnedSocketPart(
        CarPart part,
        CarPart.PartSlot expectedSlot,
        string socketName,
        out CarLoadoutSlot loadoutSlot)
    {
        loadoutSlot = DisplayedLoadoutSlot;
        return part != null
            && part.slot == expectedSlot
            && !string.IsNullOrWhiteSpace(socketName)
            && playerInventory != null
            && playerInventory.OwnsPart(part)
            && loadoutSlot != null
            && loadoutSlot.loadout != null;
    }

    private static bool IsQuantitySlot(CarPart.PartSlot slot)
        => slot == CarPart.PartSlot.Wheels || slot == CarPart.PartSlot.Brakes;

    private GarageCarManager ResolveCarManager()
    {
        if (carManager == null)
            carManager = GarageCarManager.Instance;

        return carManager;
    }

    private void HandleCarChanged(int _)
    {
        ActiveLoadout.Current = DisplayedLoadout;
        onDisplayedCarChanged?.Invoke();
    }
}
