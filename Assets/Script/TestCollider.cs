using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SceneColliderScanner : MonoBehaviour
{
    public float scanRadius = 2f;
    public Transform[] wheelVisuals;
    public float rayDown = 1.5f;

    void FixedUpdate()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, scanRadius);
        foreach (var c in cols)
        {
            Debug.Log($"Nearby collider: {c.gameObject.name} (layer: {LayerMask.LayerToName(c.gameObject.layer)})");
        }

        if (wheelVisuals != null && wheelVisuals.Length > 0)
        {
            for (int i = 0; i < wheelVisuals.Length; i++)
            {
                Ray r = new Ray(wheelVisuals[i].position + Vector3.up * 0.1f, Vector3.down);
                if (Physics.Raycast(r, out RaycastHit hit, rayDown))
                {
                    Debug.Log($"Wheel[{i}] ray hit: {hit.collider.name} at {hit.point}, normal={hit.normal}, dist={hit.distance}");
                }
                else Debug.Log($"Wheel[{i}] ray: no hit");
            }
        }
    }
}
