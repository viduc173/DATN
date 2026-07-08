using System;
using System.Collections.Generic;
using Michsky.UI.Heat;
using UnityEngine;

/// <summary>
/// Auto-populates the garage shop (UI_Main Menu > Part > Shop).
///
/// Attach this to the "Shop" GameObject (it uses itself as the search root, so
/// "Shop Button"s living in other menus — Inventory, CarInfo … — are never touched).
///
/// For every shop panel found under the root it:
///  1. Locates the existing "Shop Button" as the template (path:
///     ... > Panels > {panel} > Content > List > Layout Group > Shop Button).
///  2. Hides the template and duplicates it once per CarPart sold for that panel's
///     slot. Part data is read from <see cref="GarageDisplayedCarContext.GetShopParts"/>
///     (backed by the ShopCatalog / the CarPart assets under Assets/Data/CarParts).
///  3. Assigns icon, title (partName), description and price (costGold) to each clone
///     and wires its Buy button to the purchase flow.
///
/// Purchase behaviour:
///  - Wheels / Brakes (quantity slots): buying adds 1 to inventory; the button stays
///    buyable so the player can stock more.
///  - Engine / Suspension / ECU (permanent slots): buying flips the button to the
///    "Purchased" state and locks it. Already-owned permanent parts start Purchased.
///
/// Which panel maps to which CarPart slot is driven by <see cref="panelSlots"/>
/// (matched against the panel GameObject's name, case-insensitive).
/// </summary>
[DefaultExecutionOrder(-20)]
public class ShopUIController : MonoBehaviour
{
    [Serializable]
    public class PanelSlotMap
    {
        [Tooltip("GameObject name of the shop panel (case-insensitive), e.g. 'wheel'.")]
        public string panelName;

        public CarPart.PartSlot slot;
    }

    [Header("Context (auto-found if empty)")]
    [SerializeField] private GarageDisplayedCarContext context;

    [Header("Search root (defaults to this GameObject)")]
    [Tooltip("Only ShopButtonManagers under this transform are populated. " +
             "Leave empty to use the GameObject this component is on.")]
    [SerializeField] private Transform shopRoot;

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
    [Tooltip("Disable Heat UI localization on spawned buttons so the part name/description we set is not overwritten.")]
    [SerializeField] private bool disableLocalization = true;

    [Tooltip("Rebuild the shop every time this object is enabled (e.g. each time the Shop panel opens).")]
    [SerializeField] private bool rebuildOnEnable = true;

    [Tooltip("Tên panel bán XE (loadout). Panel này lấy data từ ShopCatalog.carStock (PlayerCarLoadout), không phải CarPart.")]
    [SerializeField] private string carPanelName = "car";

    [Header("Result modals (optional — Modal Windows > Purchase Success / Fail)")]
    [Tooltip("Opened when a purchase succeeds.")]
    [SerializeField] private ModalWindowManager purchaseSuccessModal;

    [Tooltip("Opened when a purchase fails (not enough gold / out of stock).")]
    [SerializeField] private ModalWindowManager purchaseFailModal;

    // Cached templates per slot (the original "Shop Button"s, kept inactive). Cached so
    // repeated rebuilds never mistake an already-spawned clone for the template.
    private readonly Dictionary<CarPart.PartSlot, ShopButtonManager> _templates =
        new Dictionary<CarPart.PartSlot, ShopButtonManager>();

    // Template cho panel "car" (xe = PlayerCarLoadout, không phải CarPart).
    private ShopButtonManager _carTemplate;

    private readonly List<GameObject> _spawned = new List<GameObject>();

    private void Awake()
    {
        if (context == null)
            context = FindFirstObjectByType<GarageDisplayedCarContext>();
        if (shopRoot == null)
            shopRoot = transform;
    }

    private void OnEnable()
    {
        if (rebuildOnEnable)
            Rebuild();
    }

    private void OnDisable()
    {
        ClearSpawned();
    }

    [ContextMenu("Rebuild Shop")]
    public void Rebuild()
    {
        if (shopRoot == null)
            shopRoot = transform;

        if (context == null)
            context = FindFirstObjectByType<GarageDisplayedCarContext>();

        if (context == null)
        {
            Debug.LogWarning("[ShopUIController] No GarageDisplayedCarContext found.", this);
            return;
        }

        ClearSpawned();
        DiscoverTemplates();

        foreach (KeyValuePair<CarPart.PartSlot, ShopButtonManager> entry in _templates)
            BuildPanel(entry.Value, entry.Key);

        BuildCarPanel();
    }

    private void DiscoverTemplates()
    {
        if (_templates.Count > 0 || _carTemplate != null)
            return;

        foreach (ShopButtonManager button in shopRoot.GetComponentsInChildren<ShopButtonManager>(true))
        {
            if (TryResolveSlot(button.transform, out CarPart.PartSlot slot))
            {
                // Hide every pre-existing shop button (template + any leftover samples).
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
        Transform stopAt = shopRoot != null ? shopRoot.parent : null;
        for (Transform t = buttonTransform; t != null && t != stopAt; t = t.parent)
            if (string.Equals(t.name, panelName, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private bool TryResolveSlot(Transform buttonTransform, out CarPart.PartSlot slot)
    {
        slot = default;
        Transform stopAt = shopRoot != null ? shopRoot.parent : null;

        for (Transform t = buttonTransform; t != null && t != stopAt; t = t.parent)
        {
            foreach (PanelSlotMap map in panelSlots)
            {
                if (string.IsNullOrEmpty(map.panelName))
                    continue;

                if (string.Equals(t.name, map.panelName, StringComparison.OrdinalIgnoreCase))
                {
                    slot = map.slot;
                    return true;
                }
            }
        }

        return false;
    }

    private void BuildPanel(ShopButtonManager template, CarPart.PartSlot slot)
    {
        if (template == null)
            return;

        Transform container = template.transform.parent;
        if (container == null)
            return;

        foreach (CarPart part in context.GetShopParts(slot))
        {
            if (part == null)
                continue;

            // Template is inactive, so the clone starts inactive: configure first, then show.
            ShopButtonManager button = Instantiate(template, container);
            button.gameObject.name = $"Shop Button - {part.partName}";
            ConfigureButton(button, part);
            button.gameObject.SetActive(true);

            _spawned.Add(button.gameObject);
        }
    }

    // ── Car panel (xe = PlayerCarLoadout) ────────────────────────────────────────

    private void BuildCarPanel()
    {
        if (_carTemplate == null)
            return;

        Transform container = _carTemplate.transform.parent;
        if (container == null)
            return;

        foreach (PlayerCarLoadout car in context.GetShopCars())
        {
            if (car == null)
                continue;

            ShopButtonManager button = Instantiate(_carTemplate, container);
            button.gameObject.name = $"Shop Button - {car.loadoutName}";
            ConfigureCarButton(button, car);
            button.gameObject.SetActive(true);

            _spawned.Add(button.gameObject);
        }
    }

    private void ConfigureCarButton(ShopButtonManager button, PlayerCarLoadout car)
    {
        if (disableLocalization)
            button.useLocalization = false;

        button.enableIcon = car.icon != null;
        button.enableTitle = true;
        button.enableDescription = !string.IsNullOrEmpty(car.description);
        button.enablePrice = true;

        button.buttonIcon = car.icon;
        button.buttonTitle = car.loadoutName;
        button.buttonDescription = car.description;
        button.priceText = car.costGold.ToString();
        button.UpdateUI();

        ApplyState(button, context.OwnsLoadout(car));

        button.onPurchaseClick.RemoveAllListeners();
        button.onPurchaseClick.AddListener(() => OnBuyCar(car, button));
    }

    private void OnBuyCar(PlayerCarLoadout car, ShopButtonManager button)
    {
        if (context.BuyLoadout(car))
        {
            ApplyState(button, true); // one-time purchase -> lock
            if (purchaseSuccessModal != null) purchaseSuccessModal.OpenWindow();
        }
        else
        {
            Debug.Log($"[ShopUIController] Cannot buy car '{car.loadoutName}' (not enough gold or already owned).", this);
            if (purchaseFailModal != null) purchaseFailModal.OpenWindow();
        }
    }

    private void ConfigureButton(ShopButtonManager button, CarPart part)
    {
        if (disableLocalization)
            button.useLocalization = false;

        button.enableIcon = part.icon != null;
        button.enableTitle = true;
        button.enableDescription = true;
        button.enablePrice = true;

        button.buttonIcon = part.icon;
        button.buttonTitle = part.partName;
        button.buttonDescription = part.description;
        button.priceText = part.costGold.ToString();
        button.UpdateUI();

        ApplyState(button, ShouldStartPurchased(part));

        // Buy button -> onPurchaseClick (see ShopButtonManager.InitializePurchaseEvents).
        button.onPurchaseClick.RemoveAllListeners();
        button.onPurchaseClick.AddListener(() => OnBuyClicked(part, button));
    }

    /// <summary>
    /// Toggle Default(purchaseButton) vs Purchased(purchasedButton) ourselves — null-safe even when
    /// a template has no purchasedIndicator (ShopButtonManager.UpdateState bails out in that case).
    /// </summary>
    private static void ApplyState(ShopButtonManager button, bool purchased)
    {
        button.state = purchased ? ShopButtonManager.State.Purchased : ShopButtonManager.State.Default;
        if (button.purchaseButton != null) button.purchaseButton.gameObject.SetActive(!purchased);
        if (button.purchasedButton != null) button.purchasedButton.gameObject.SetActive(purchased);
        if (button.purchasedIndicator != null) button.purchasedIndicator.SetActive(purchased);
    }

    private void OnBuyClicked(CarPart part, ShopButtonManager button)
    {
        bool bought = context.BuyPart(part);
        if (!bought)
        {
            Debug.Log($"[ShopUIController] Cannot buy '{part.partName}' (not enough gold or out of stock).", this);
            if (purchaseFailModal != null)
                purchaseFailModal.OpenWindow();
            return;
        }

        // Permanent parts lock after purchase; wheels/brakes stay buyable to stock more.
        if (IsPermanentSlot(part.slot))
            ApplyState(button, true);

        if (purchaseSuccessModal != null)
            purchaseSuccessModal.OpenWindow();
    }

    private bool ShouldStartPurchased(CarPart part)
    {
        return IsPermanentSlot(part.slot)
            && context.Inventory != null
            && context.Inventory.OwnsPart(part);
    }

    private static bool IsPermanentSlot(CarPart.PartSlot slot)
        => slot != CarPart.PartSlot.Wheels && slot != CarPart.PartSlot.Brakes;

    private void ClearSpawned()
    {
        foreach (GameObject go in _spawned)
        {
            if (go == null)
                continue;

            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }

        _spawned.Clear();
    }
}
