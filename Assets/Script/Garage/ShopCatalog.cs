using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data asset for one garage shop. UI reads this to know which CarPart assets
/// are currently sold.
/// </summary>
[CreateAssetMenu(fileName = "ShopCatalog", menuName = "Race/Shop Catalog", order = 4)]
public class ShopCatalog : ScriptableObject
{
    [Header("Shop")]
    public string shopName = "Garage Parts Shop";

    [Tooltip("Parts dang ban trong shop nay. Gia lay tu CarPart.costGold.")]
    public List<CarPart> stock = new List<CarPart>();

    [Tooltip("Cac xe (PlayerCarLoadout) dang ban. Gia lay tu PlayerCarLoadout.costGold.")]
    public List<PlayerCarLoadout> carStock = new List<PlayerCarLoadout>();

    public bool HasStock(CarPart part)
        => part != null && stock != null && stock.Contains(part);

    public List<CarPart> GetStockBySlot(CarPart.PartSlot slot)
    {
        var result = new List<CarPart>();
        if (stock == null) return result;

        foreach (CarPart part in stock)
            if (part != null && part.slot == slot)
                result.Add(part);

        return result;
    }
}
