// Source : 
// https://discussions.unity.com/t/how-to-make-a-physically-real-stable-car-with-wheelcolliders/415132/70

using UnityEngine;
using System.Collections;
namespace ALIyerEdon
{
    public class AntiRoll : MonoBehaviour
    {
        public WheelCollider WheelRight;
        public WheelCollider WheelLeft;
        public float AntiRollValue = 5000.0f;

        void FixedUpdate()
        {
            WheelHit hit;

            float travelL = 1.0f;
            float travelR = 1.0f;

            bool groundedL = WheelLeft.GetGroundHit(out hit);

            if (groundedL)
                travelL = (-WheelLeft.transform.InverseTransformPoint(hit.point).y - WheelLeft.radius) / WheelLeft.suspensionDistance;

            bool groundedR = WheelRight.GetGroundHit(out hit);

            if (groundedR)
                travelR = (-WheelRight.transform.InverseTransformPoint(hit.point).y - WheelRight.radius) / WheelRight.suspensionDistance;

            float antiRollForce = (travelL - travelR) * AntiRollValue;

            if (groundedL)
                GetComponent<Rigidbody>().AddForceAtPosition(WheelLeft.transform.up * -antiRollForce,
                       WheelLeft.transform.position);
            if (groundedR)
                GetComponent<Rigidbody>().AddForceAtPosition(WheelRight.transform.up * antiRollForce,
                       WheelRight.transform.position);
        }
    }
}