using UnityEngine;

public class Destroy_Joint : MonoBehaviour
{
    void OnJointBreak(float breakForce)
    {
        Destroy(gameObject, 10f);
    }
}
