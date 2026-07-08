using System.Collections.Generic;
using Michsky.UI.Heat;
using UnityEngine;

/// <summary>
/// Populates the read-only CarInfo screen (UI_Main Menu > Part > CarInfo) for the car
/// currently displayed in the garage. Everything here is display-only (no buttons).
///
///  - **Car card** (panel "Car", a <see cref="ShopButtonManager"/> template): shows the current
///    car's icon / title (loadoutName) / description. Buy/Purchased buttons are hidden.
///  - **Part list** (panel "Part", an <see cref="AchievementItem"/> template named "Item"):
///    one entry per equipped part from <see cref="PlayerCarLoadout.AllParts"/> (lists ALL parts,
///    incl. each of the 4 wheels/brakes), with icon / title (partName) / description.
///  - **Stats** (panel "Info") are handled automatically by the existing CarStatsUIManager
///    (autoTrackActiveCar) — this controller does NOT touch stats.
///
/// Attach to the "CarInfo" GameObject. Auto-finds the context and the templates by component.
/// Refreshes when the displayed car changes or a part is equipped/unequipped.
/// </summary>
[DefaultExecutionOrder(-20)]
public class CarInfoUIController : MonoBehaviour
{
    [Header("Refs (auto-resolved if empty)")]
    [SerializeField] private GarageDisplayedCarContext context;

    [Header("Search root (defaults to this GameObject)")]
    [SerializeField] private Transform infoRoot;

    [Header("Panel names")]
    [Tooltip("Panel chứa card hiển thị xe (ShopButtonManager template).")]
    [SerializeField] private string carPanelName = "Car";

    [Tooltip("Panel chứa list part đang lắp (AchievementItem template).")]
    [SerializeField] private string partsPanelName = "Part";

    [Header("Behaviour")]
    [SerializeField] private bool disableLocalization = true;
    [SerializeField] private bool rebuildOnEnable = true;

    private ShopButtonManager _carCard;     // dùng trực tiếp làm card (1 xe), không clone
    private AchievementItem _partTemplate;  // template để clone cho từng part
    private readonly List<GameObject> _spawnedParts = new List<GameObject>();

    private void Awake()
    {
        if (context == null) context = FindFirstObjectByType<GarageDisplayedCarContext>();
        if (infoRoot == null) infoRoot = transform;
    }

    private void OnEnable()
    {
        if (context != null)
        {
            context.onDisplayedCarChanged.AddListener(Rebuild);
            context.onPartEquipped.AddListener(HandlePartChanged);
            context.onPartUnequipped.AddListener(HandlePartChanged);
        }
        if (rebuildOnEnable) Rebuild();
    }

    private void OnDisable()
    {
        if (context != null)
        {
            context.onDisplayedCarChanged.RemoveListener(Rebuild);
            context.onPartEquipped.RemoveListener(HandlePartChanged);
            context.onPartUnequipped.RemoveListener(HandlePartChanged);
        }
        ClearSpawnedParts();
    }

    private void HandlePartChanged(CarPart _) => Rebuild();

    [ContextMenu("Rebuild Car Info")]
    public void Rebuild()
    {
        if (infoRoot == null) infoRoot = transform;
        if (context == null) context = FindFirstObjectByType<GarageDisplayedCarContext>();
        if (context == null)
        {
            Debug.LogWarning("[CarInfoUIController] No GarageDisplayedCarContext found.", this);
            return;
        }

        ClearSpawnedParts();
        DiscoverTemplates();

        BuildCarCard();
        BuildPartList();
    }

    private void DiscoverTemplates()
    {
        if (_carCard == null)
            foreach (ShopButtonManager b in infoRoot.GetComponentsInChildren<ShopButtonManager>(true))
                if (IsUnderPanel(b.transform, carPanelName)) { _carCard = b; break; }

        if (_partTemplate == null)
            foreach (AchievementItem a in infoRoot.GetComponentsInChildren<AchievementItem>(true))
                if (IsUnderPanel(a.transform, partsPanelName)) { a.gameObject.SetActive(false); _partTemplate = a; break; }
    }

    // ── Car card (1 xe hiện tại, display-only) ───────────────────────────────────

    private void BuildCarCard()
    {
        if (_carCard == null) return;

        PlayerCarLoadout car = context.DisplayedLoadout;
        if (car == null) { _carCard.gameObject.SetActive(false); return; }

        if (disableLocalization) _carCard.useLocalization = false;
        _carCard.enableIcon = car.icon != null;
        _carCard.enableTitle = true;
        _carCard.enableDescription = !string.IsNullOrEmpty(car.description);
        _carCard.enablePrice = false;

        _carCard.buttonIcon = car.icon;
        _carCard.buttonTitle = car.loadoutName;
        _carCard.buttonDescription = car.description;
        _carCard.gameObject.SetActive(true);
        _carCard.UpdateUI();

        // Display-only: ẩn cả 2 nút mua/đã-mua.
        if (_carCard.purchaseButton != null) _carCard.purchaseButton.gameObject.SetActive(false);
        if (_carCard.purchasedButton != null) _carCard.purchasedButton.gameObject.SetActive(false);
    }

    // ── Part list (liệt kê hết part đang lắp) ─────────────────────────────────────

    private void BuildPartList()
    {
        if (_partTemplate == null) return;

        PlayerCarLoadout car = context.DisplayedLoadout;
        if (car == null) return;

        Transform container = _partTemplate.transform.parent;
        if (container == null) return;

        foreach (CarPart part in car.AllParts())
        {
            if (part == null) continue;

            AchievementItem item = Instantiate(_partTemplate, container);
            item.gameObject.name = $"Item - {part.partName}";
            ConfigureItem(item, part);
            item.gameObject.SetActive(true);
            _spawnedParts.Add(item.gameObject);
        }
    }

    private static void ConfigureItem(AchievementItem item, CarPart part)
    {
        if (item.iconObj != null)
        {
            item.iconObj.sprite = part.icon;
            item.iconObj.enabled = part.icon != null;
        }
        if (item.titleObj != null) item.titleObj.text = part.partName;
        if (item.descriptionObj != null) item.descriptionObj.text = part.description;
        if (item.lockedIndicator != null) item.lockedIndicator.SetActive(false);
        if (item.unlockedIndicator != null) item.unlockedIndicator.SetActive(false);
    }

    // ── helpers ───────────────────────────────────────────────────────────────────

    private bool IsUnderPanel(Transform t, string panelName)
    {
        if (string.IsNullOrEmpty(panelName)) return false;
        Transform stopAt = infoRoot != null ? infoRoot.parent : null;
        for (Transform cur = t; cur != null && cur != stopAt; cur = cur.parent)
            if (string.Equals(cur.name, panelName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private void ClearSpawnedParts()
    {
        foreach (GameObject go in _spawnedParts)
        {
            if (go == null) continue;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
        _spawnedParts.Clear();
    }
}
