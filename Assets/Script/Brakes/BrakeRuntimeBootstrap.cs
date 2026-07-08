using UnityEngine;

public static class BrakeRuntimeBootstrap
{
    public static BrakeItem EnsureItem(GameObject go, CarPart partData, Material ghostMaterial, float ghostPreviewDistance)
    {
        if (go == null) return null;

        BrakeItem item = go.GetComponent<BrakeItem>();
        if (item == null)
            item = go.AddComponent<BrakeItem>();

        item.EnsureRuntimeComponents();

        BrakeStats stats = go.GetComponent<BrakeStats>();
        if (stats == null)
            stats = go.AddComponent<BrakeStats>();

        if (partData != null)
            stats.partData = partData;

        PCInteractorObject interactor = go.GetComponent<PCInteractorObject>();
        if (interactor == null)
            interactor = go.AddComponent<PCInteractorObject>();

        interactor.ConfigureGhost(ghostMaterial, ghostPreviewDistance);
        return item;
    }
}
