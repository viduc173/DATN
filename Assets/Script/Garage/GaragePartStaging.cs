using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns / clears the physical part models in the garage staging area
/// (CartPartPlace > Wheels / Brake).
///
/// This is what the inventory "Send To Garage" / "Return To Inventory" buttons drive:
/// it ONLY shows/hides the real model taken from the CarPart's prefab data
/// (<see cref="CarPart.wheelPrefab"/>, <see cref="CarPart.brakePrefabLeft"/> /
/// <see cref="CarPart.brakePrefabRight"/>). It does NOT equip anything onto the car
/// and does NOT change inventory quantities — mounting onto the car is a separate step
/// (player drags a spawned WheelItem onto a WheelSocket).
///
/// Attach to the "CartPartPlace" GameObject (it auto-finds its "Wheels"/"Brake" children).
/// </summary>
public class GaragePartStaging : MonoBehaviour
{
    [Header("Anchors (auto-found under this object if empty)")]
    [Tooltip("CartPartPlace > Wheels — parent for spawned wheel models.")]
    [SerializeField] private Transform wheelAnchor;

    [Tooltip("CartPartPlace > Brake — parent for spawned brake models.")]
    [SerializeField] private Transform brakeAnchor;

    [Tooltip("CartPartPlace > Spray — parent for spawned spray-can models (slot = Paint).")]
    [SerializeField] private Transform sprayAnchor;

    [Header("Layout")]
    [Tooltip("Spacing (local units) between spawned models.")]
    [SerializeField] private float spacing = 0.5f;

    [Tooltip("Models per row before wrapping along Z.")]
    [SerializeField] private int perRow = 4;

    [Tooltip("Spawned models fall under gravity (Rigidbody dynamic). Turn off to keep them frozen/kinematic in place.")]
    [SerializeField] private bool spawnWithGravity = true;

    private readonly Dictionary<CarPart, List<GameObject>> _spawned = new Dictionary<CarPart, List<GameObject>>();

    private void Awake()
    {
        if (wheelAnchor == null) wheelAnchor = transform.Find("Wheels");
        if (brakeAnchor == null) brakeAnchor = transform.Find("Brake");
        if (sprayAnchor == null) sprayAnchor = transform.Find("Spray");
    }

    public bool IsSpawned(CarPart part)
        => part != null && _spawned.TryGetValue(part, out List<GameObject> list) && list != null && list.Count > 0;

    /// <summary>Spawn `count` models of this part under its matching anchor. No-op if already spawned.</summary>
    public bool Spawn(CarPart part, int count)
    {
        if (part == null || count <= 0) return false;
        if (IsSpawned(part)) return true;

        Transform anchor = AnchorFor(part.slot);
        if (anchor == null)
        {
            Debug.LogWarning($"[GaragePartStaging] No anchor for slot {part.slot} (need CartPartPlace > Wheels/Brake).", this);
            return false;
        }

        var list = new List<GameObject>();

        if (part.slot == CarPart.PartSlot.Wheels)
        {
            for (int i = 0; i < count; i++)
                AddSpawn(list, part.wheelPrefab, anchor, i);
        }
        else if (part.slot == CarPart.PartSlot.Brakes)
        {
            // Per-caliper: one model per owned caliper. Alternate L/R for the staging display;
            // each caliper re-adapts its mesh to the actual socket side when mounted (BrakeStats).
            for (int i = 0; i < count; i++)
                AddSpawn(list, (i % 2 == 0) ? part.brakePrefabLeft : part.brakePrefabRight, anchor, i);
        }
        else if (part.slot == CarPart.PartSlot.Paint)
        {
            // Spray (paint) is permanent: show the spray-can model so the player can paint with it.
            for (int i = 0; i < count; i++)
                AddSpawn(list, part.sprayPrefab, anchor, i);
        }

        if (list.Count == 0)
        {
            Debug.LogWarning($"[GaragePartStaging] '{part.partName}' has no model prefab to spawn.", this);
            return false;
        }

        _spawned[part] = list;
        return true;
    }

    /// <summary>Destroy all spawned models of this part.</summary>
    public void Clear(CarPart part)
    {
        if (part == null || !_spawned.TryGetValue(part, out List<GameObject> list))
            return;

        foreach (GameObject go in list)
            if (go != null) Destroy(go);

        _spawned.Remove(part);
    }

    public void ClearAll()
    {
        foreach (KeyValuePair<CarPart, List<GameObject>> kv in _spawned)
            foreach (GameObject go in kv.Value)
                if (go != null) Destroy(go);

        _spawned.Clear();
    }

    private void AddSpawn(List<GameObject> list, GameObject prefab, Transform anchor, int index)
    {
        if (prefab == null) return;

        GameObject go = Instantiate(prefab, anchor);
        int safeRow = Mathf.Max(1, perRow);
        int row = index / safeRow;
        int col = index % safeRow;
        go.transform.localPosition = new Vector3(col * spacing, 0f, row * spacing);
        go.transform.localRotation = Quaternion.identity;

        if (go.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = !spawnWithGravity;
            rb.useGravity = spawnWithGravity;
        }

        list.Add(go);
    }

    private Transform AnchorFor(CarPart.PartSlot slot)
    {
        if (slot == CarPart.PartSlot.Wheels) return wheelAnchor;
        if (slot == CarPart.PartSlot.Brakes) return brakeAnchor;
        if (slot == CarPart.PartSlot.Paint) return sprayAnchor;
        return null;
    }
}
