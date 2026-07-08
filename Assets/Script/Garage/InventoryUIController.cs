using System;
using System.Collections.Generic;
using Michsky.UI.Heat;
using UnityEngine;

/// <summary>
/// Auto-populates the inventory UI (UI_Main Menu > Part > Inventory).
///
/// Attach to the "Inventory" GameObject (uses itself as the search root, so item
/// buttons in other menus — Shop, CarInfo … — are never touched).
///
/// For each panel it takes the existing item template (named "Item Show Button" for
/// wheel/brake, "Item Button" for engine/suspension/ecu — discovered by the
/// <see cref="ShopButtonManager"/> component, so the name doesn't matter), hides it,
/// and spawns one clone per owned CarPart (<see cref="GarageDisplayedCarContext.GetOwnedParts"/>).
///
/// Button behaviour (reuses ShopButtonManager's two states):
///  - Wheels / Brakes: Default = "Send To Garage", Purchased = "Return To Inventory".
///    Send spawns the real models in the garage staging area (<see cref="GaragePartStaging"/>);
///    Return clears them. This does NOT equip onto the car. Title shows "(N)" available count.
///  - Engine / Suspension / ECU: Default = "Equip", Purchased = "Equipped/Unequip".
///    Equip/unequip the part on the currently displayed car. State follows
///    <see cref="PlayerCarLoadout.HasPart"/>; re-evaluated when the displayed car changes.
///
/// NOTE: ShopButtonManager only auto-wires the Default button (purchaseButton -> onPurchaseClick);
/// the second button (purchasedButton) is wired here manually.
/// </summary>
[DefaultExecutionOrder(-20)]
public class InventoryUIController : MonoBehaviour
{
    [Serializable]
    public class PanelSlotMap
    {
        [Tooltip("GameObject name of the inventory panel (case-insensitive), e.g. 'wheel'.")]
        public string panelName;

        public CarPart.PartSlot slot;
    }

    [Header("Refs (auto-resolved if empty)")]
    [SerializeField] private GarageDisplayedCarContext context;
    [SerializeField] private GaragePartStaging staging;

    [Header("Search root (defaults to this GameObject)")]
    [SerializeField] private Transform inventoryRoot;

    [Header("Panel name -> CarPart slot")]
    [SerializeField]
    private List<PanelSlotMap> panelSlots = new List<PanelSlotMap>
    {
        new PanelSlotMap { panelName = "wheel",      slot = CarPart.PartSlot.Wheels },
        new PanelSlotMap { panelName = "brake",      slot = CarPart.PartSlot.Brakes },
        new PanelSlotMap { panelName = "engine",     slot = CarPart.PartSlot.Engine },
        new PanelSlotMap { panelName = "suspension", slot = CarPart.PartSlot.Suspension },
        new PanelSlotMap { panelName = "ecu",        slot = CarPart.PartSlot.ECU },
        new PanelSlotMap { panelName = "spray",      slot = CarPart.PartSlot.Paint },
    };

    [Header("Behaviour")]
    [SerializeField] private bool disableLocalization = true;
    [SerializeField] private bool rebuildOnEnable = true;

    [Tooltip("Tên panel CHỌN XE (loadout) trong inventory — chỉ liệt kê xe đã sở hữu, bấm = đổi xe đang hiển thị.")]
    [SerializeField] private string carPanelName = "car";

    private readonly Dictionary<CarPart.PartSlot, ShopButtonManager> _templates =
        new Dictionary<CarPart.PartSlot, ShopButtonManager>();

    private ShopButtonManager _carTemplate;

    private readonly List<GameObject> _spawned = new List<GameObject>();

    private void Awake()
    {
        if (context == null) context = FindFirstObjectByType<GarageDisplayedCarContext>();
        if (staging == null) staging = FindFirstObjectByType<GaragePartStaging>();
        if (inventoryRoot == null) inventoryRoot = transform;
    }

    private void OnEnable()
    {
        if (context != null)
        {
            context.onPartPurchased.AddListener(HandlePartChanged);
            context.onPartEquipped.AddListener(HandlePartChanged);
            context.onPartUnequipped.AddListener(HandlePartChanged);
            context.onCarBought.AddListener(HandleCarBought);
            context.onDisplayedCarChanged.AddListener(Rebuild);
        }

        // Physical mount/unmount (drag wheel onto socket) changes inventory via PartInventoryBridge.
        PartInventoryBridge.Changed += Rebuild;

        if (rebuildOnEnable) Rebuild();
    }

    private void OnDisable()
    {
        if (context != null)
        {
            context.onPartPurchased.RemoveListener(HandlePartChanged);
            context.onPartEquipped.RemoveListener(HandlePartChanged);
            context.onPartUnequipped.RemoveListener(HandlePartChanged);
            context.onCarBought.RemoveListener(HandleCarBought);
            context.onDisplayedCarChanged.RemoveListener(Rebuild);
        }

        PartInventoryBridge.Changed -= Rebuild;

        ClearSpawned();
    }

    [ContextMenu("Rebuild Inventory")]
    public void Rebuild()
    {
        if (inventoryRoot == null) inventoryRoot = transform;
        if (context == null) context = FindFirstObjectByType<GarageDisplayedCarContext>();
        if (context == null)
        {
            Debug.LogWarning("[InventoryUIController] No GarageDisplayedCarContext found.", this);
            return;
        }

        ClearSpawned();
        DiscoverTemplates();

        foreach (KeyValuePair<CarPart.PartSlot, ShopButtonManager> entry in _templates)
            BuildPanel(entry.Value, entry.Key);

        BuildCarPanel();
    }

    private void HandlePartChanged(CarPart _) => Rebuild();
    private void HandleCarBought(PlayerCarLoadout _) => Rebuild();

    private void DiscoverTemplates()
    {
        if (_templates.Count > 0 || _carTemplate != null) return;

        foreach (ShopButtonManager button in inventoryRoot.GetComponentsInChildren<ShopButtonManager>(true))
        {
            if (TryResolveSlot(button.transform, out CarPart.PartSlot slot))
            {
                button.gameObject.SetActive(false);
                if (!_templates.ContainsKey(slot))
                    _templates.Add(slot, button);
            }
            else if (IsUnderPanel(button.transform, carPanelName))
            {
                button.gameObject.SetActive(false);
                if (_carTemplate == null)
                    _carTemplate = button;
            }
        }
    }

    private bool IsUnderPanel(Transform buttonTransform, string panelName)
    {
        if (string.IsNullOrEmpty(panelName)) return false;
        Transform stopAt = inventoryRoot != null ? inventoryRoot.parent : null;
        for (Transform t = buttonTransform; t != null && t != stopAt; t = t.parent)
            if (string.Equals(t.name, panelName, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private bool TryResolveSlot(Transform t, out CarPart.PartSlot slot)
    {
        slot = default;
        Transform stopAt = inventoryRoot != null ? inventoryRoot.parent : null;

        for (Transform cur = t; cur != null && cur != stopAt; cur = cur.parent)
            foreach (PanelSlotMap map in panelSlots)
                if (!string.IsNullOrEmpty(map.panelName) &&
                    string.Equals(cur.name, map.panelName, StringComparison.OrdinalIgnoreCase))
                {
                    slot = map.slot;
                    return true;
                }

        return false;
    }

    private void BuildPanel(ShopButtonManager template, CarPart.PartSlot slot)
    {
        if (template == null) return;

        Transform container = template.transform.parent;
        if (container == null) return;

        foreach (CarPart part in context.GetOwnedParts(slot))
        {
            if (part == null) continue;

            ShopButtonManager button = Instantiate(template, container);
            button.gameObject.name = $"Item Show Button - {part.partName}";
            ConfigureButton(button, part);
            button.gameObject.SetActive(true);

            _spawned.Add(button.gameObject);
        }
    }

    private void ConfigureButton(ShopButtonManager button, CarPart part)
    {
        bool isStaging = IsStagingSlot(part.slot);   // wheel / brake / spray(paint) -> Send/Return spawn
        bool showCount = IsQuantitySlot(part.slot);  // chỉ wheel/brake hiện "(N)"; spray permanent thì không

        if (disableLocalization) button.useLocalization = false;
        button.enableIcon = part.icon != null;
        button.enableTitle = true;
        button.enableDescription = true;
        button.enablePrice = false; // inventory: no price

        button.buttonIcon = part.icon;
        button.buttonTitle = showCount
            ? $"{part.partName} ({GetQuantity(part)})"
            : part.partName;
        button.buttonDescription = part.description;
        button.UpdateUI();

        if (isStaging)
        {
            bool shown = staging != null && staging.IsSpawned(part);
            ApplyState(button, shown);

            // Default button = Send To Garage
            button.onPurchaseClick.RemoveAllListeners();
            button.onPurchaseClick.AddListener(() => OnSendToGarage(part, button));
            // Purchased button = Return To Inventory (not auto-wired by ShopButtonManager)
            WirePurchasedButton(button, () => OnReturnToInventory(part, button));
        }
        else
        {
            bool equipped = context.DisplayedLoadout != null && context.DisplayedLoadout.HasPart(part);
            ApplyState(button, equipped);

            // Default button = Equip
            button.onPurchaseClick.RemoveAllListeners();
            button.onPurchaseClick.AddListener(() => OnEquip(part, button));
            // Purchased button = Unequip
            WirePurchasedButton(button, () => OnUnequip(part, button));
        }
    }

    // ----- Wheel / Brake: spawn / clear physical models (no equip) -----

    private void OnSendToGarage(CarPart part, ShopButtonManager button)
    {
        if (staging == null)
        {
            Debug.LogWarning("[InventoryUIController] No GaragePartStaging found.", this);
            return;
        }

        if (staging.Spawn(part, GetQuantity(part)))
            ApplyState(button, true);
    }

    private void OnReturnToInventory(CarPart part, ShopButtonManager button)
    {
        staging?.Clear(part);
        ApplyState(button, false);
    }

    // ----- Engine / Suspension / ECU: equip / unequip on displayed car -----

    private void OnEquip(CarPart part, ShopButtonManager button)
    {
        if (context.EquipOwnedPart(part))
            ApplyState(button, true);
    }

    private void OnUnequip(CarPart part, ShopButtonManager button)
    {
        if (context.UnequipOwnedPart(part))
            ApplyState(button, false);
    }

    // ----- Car: select (đổi xe đang hiển thị của màn) -----

    private void BuildCarPanel()
    {
        if (_carTemplate == null) return;

        Transform container = _carTemplate.transform.parent;
        if (container == null) return;

        PlayerCarLoadout active = context.DisplayedLoadout;

        foreach (PlayerCarLoadout car in context.GetOwnedCars())
        {
            if (car == null) continue;

            ShopButtonManager button = Instantiate(_carTemplate, container);
            button.gameObject.name = $"Item Show Button - {car.loadoutName}";
            ConfigureCarButton(button, car, car == active);
            button.gameObject.SetActive(true);

            _spawned.Add(button.gameObject);
        }
    }

    private void ConfigureCarButton(ShopButtonManager button, PlayerCarLoadout car, bool isActive)
    {
        if (disableLocalization) button.useLocalization = false;
        button.enableIcon = car.icon != null;
        button.enableTitle = true;
        button.enableDescription = !string.IsNullOrEmpty(car.description);
        button.enablePrice = false; // inventory: no price

        button.buttonIcon = car.icon;
        button.buttonTitle = car.loadoutName;
        button.buttonDescription = car.description;
        button.UpdateUI();

        // Xe đang hiển thị = "Selected" (Purchased); xe khác = "Select" (Default).
        ApplyState(button, isActive);

        button.onPurchaseClick.RemoveAllListeners();
        button.onPurchaseClick.AddListener(() => OnSelectCar(car));
        // Nút Purchased (đang chọn) bấm lại = no-op (đã là xe hiện tại).
    }

    private void OnSelectCar(PlayerCarLoadout car)
    {
        // SelectCar -> SetActiveCar -> onDisplayedCarChanged -> Rebuild (cập nhật xe nào đang Selected).
        context.SelectCar(car);
    }

    /// <summary>
    /// Toggle the button between Default (purchaseButton) and Purchased (purchasedButton)
    /// ourselves. ShopButtonManager.SetState/UpdateState bails out when purchasedIndicator
    /// is null (the wheel/brake "Item Show Button" templates have none), so the built-in
    /// swap silently does nothing — this is null-safe.
    /// </summary>
    private static void ApplyState(ShopButtonManager button, bool purchased)
    {
        button.state = purchased ? ShopButtonManager.State.Purchased : ShopButtonManager.State.Default;
        if (button.purchaseButton != null) button.purchaseButton.gameObject.SetActive(!purchased);
        if (button.purchasedButton != null) button.purchasedButton.gameObject.SetActive(purchased);
        if (button.purchasedIndicator != null) button.purchasedIndicator.SetActive(purchased);
    }

    // ----- helpers -----

    private int GetQuantity(CarPart part)
        => context.Inventory != null ? context.Inventory.GetAvailableQuantity(part) : 0;

    private static void WirePurchasedButton(ShopButtonManager button, UnityEngine.Events.UnityAction action)
    {
        if (button.purchasedButton == null) return;
        button.purchasedButton.onClick.RemoveAllListeners();
        button.purchasedButton.onClick.AddListener(action);
    }

    private static bool IsQuantitySlot(CarPart.PartSlot slot)
        => slot == CarPart.PartSlot.Wheels || slot == CarPart.PartSlot.Brakes;

    // Nhóm "staging": Send/Return spawn mô hình ở CartPartPlace (wheel/brake + spray=Paint).
    private static bool IsStagingSlot(CarPart.PartSlot slot)
        => slot == CarPart.PartSlot.Wheels || slot == CarPart.PartSlot.Brakes || slot == CarPart.PartSlot.Paint;

    private void ClearSpawned()
    {
        foreach (GameObject go in _spawned)
        {
            if (go == null) continue;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

        _spawned.Clear();
    }
}
