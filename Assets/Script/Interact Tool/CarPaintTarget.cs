using UnityEngine;

/// <summary>
/// Marks the renderer and material slot that can be painted on a garage car.
/// </summary>
public class CarPaintTarget : MonoBehaviour
{
    [Tooltip("Stable paint id for this car, for example CarType_0, CarType_1, CarType_2. Leave empty to use the car root name.")]
    public string carTypeName;

    [Tooltip("Renderer for the paintable car body.")]
    public Renderer bodyRenderer;

    [Tooltip("Material slot on bodyRenderer that should be replaced.")]
    public int materialSlotIndex = 0;
}
