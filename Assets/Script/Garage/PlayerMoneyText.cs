using TMPro;
using UnityEngine;

/// <summary>
/// Shows the player's current gold on a TextMeshPro label.
/// Intended for: Shop > Content > Panel Content > Money > Normal > Text.
///
/// Reads <see cref="PlayerInventory.gold"/> (via <see cref="GarageDisplayedCarContext"/>),
/// refreshes when a part is purchased, and also polls so gold changes from any other
/// source (rewards, debug, etc.) are reflected.
///
/// Setup: attach to the "Text" GameObject (it auto-grabs the TMP component on itself,
/// auto-finds the context, and reads its Inventory). No manual wiring required.
/// </summary>
[DefaultExecutionOrder(-10)]
public class PlayerMoneyText : MonoBehaviour
{
    [Header("Refs (auto-resolved if empty)")]
    [SerializeField] private GarageDisplayedCarContext context;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private TMP_Text label;

    [Header("Format")]
    [Tooltip("{0} = gold amount. e.g. \"{0:N0}\" -> 1,000  |  \"{0}\" -> 1000  |  \"$ {0:N0}\".")]
    [SerializeField] private string format = "{0:N0}";

    private int _shown = int.MinValue;

    private void Reset()
    {
        label = GetComponent<TMP_Text>();
    }

    private void Awake()
    {
        if (label == null)
            label = GetComponent<TMP_Text>();

        ResolveInventory();
    }

    private void OnEnable()
    {
        if (context != null)
            context.onPartPurchased.AddListener(HandlePartPurchased);

        Refresh();
    }

    private void OnDisable()
    {
        if (context != null)
            context.onPartPurchased.RemoveListener(HandlePartPurchased);
    }

    private void Update()
    {
        // Catch gold changes coming from anywhere, not just purchases.
        if (inventory != null && inventory.gold != _shown)
            Refresh();
    }

    [ContextMenu("Refresh")]
    public void Refresh()
    {
        ResolveInventory();

        if (label == null || inventory == null)
            return;

        _shown = inventory.gold;
        label.text = string.Format(format, _shown);
    }

    private void HandlePartPurchased(CarPart _) => Refresh();

    private void ResolveInventory()
    {
        if (inventory != null)
            return;

        if (context == null)
            context = FindFirstObjectByType<GarageDisplayedCarContext>();

        if (context != null)
            inventory = context.Inventory;
    }
}
