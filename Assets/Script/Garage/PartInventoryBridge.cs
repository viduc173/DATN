using UnityEngine;

/// <summary>
/// Bridges the physical wheel/brake attach–detach system to <see cref="PlayerInventory"/>:
///   - mounting a wheel/brake onto a socket consumes 1 from inventory,
///   - removing it returns 1.
/// <see cref="WheelStats"/> / <see cref="BrakeStats"/> call <see cref="Consume"/> /
/// <see cref="Return"/> on genuine player attach/detach.
///
/// <see cref="Suppressed"/> is raised while the loadout is mirrored programmatically
/// (CarLoadoutSlot.SyncSocketsFromLoadout spawns/attaches wheels to match the loadout,
/// e.g. on car switch) so those attaches/detaches do NOT touch inventory. Combined with
/// PlayerInventory.TryConsumePart only consuming when stock &gt; 0, default/baked wheels
/// (not in inventory) are naturally ignored.
/// </summary>
public static class PartInventoryBridge
{
    /// <summary>True while wheels are being attached/detached to mirror the loadout (no inventory side-effects).</summary>
    public static bool Suppressed { get; set; }

    /// <summary>Raised after a genuine player mount/unmount actually changed inventory (so UI can refresh).</summary>
    public static event System.Action Changed;

    private static PlayerInventory _inventory;

    private static PlayerInventory Inventory
    {
        get
        {
            if (_inventory == null)
            {
                GarageDisplayedCarContext ctx = Object.FindFirstObjectByType<GarageDisplayedCarContext>();
                if (ctx != null) _inventory = ctx.Inventory;
            }
            return _inventory;
        }
    }

    public static void Consume(CarPart part)
    {
        if (Suppressed || !IsQuantitySlot(part) || Inventory == null) return;
        if (Inventory.TryConsumePart(part))
            Changed?.Invoke();
    }

    public static void Return(CarPart part)
    {
        if (Suppressed || !IsQuantitySlot(part) || Inventory == null) return;
        Inventory.ReturnPart(part);
        Changed?.Invoke();
    }

    private static bool IsQuantitySlot(CarPart part)
        => part != null && (part.slot == CarPart.PartSlot.Wheels || part.slot == CarPart.PartSlot.Brakes);
}
