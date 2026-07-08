using UnityEngine;

public class RenderCam_Settings : MonoBehaviour
{
    public Camera Target;
    
    Camera current;

    void Start()
    {
        current = GetComponent<Camera>();
    }

    void Update()
    {
        current.fieldOfView = Target.fieldOfView;
    }
}
