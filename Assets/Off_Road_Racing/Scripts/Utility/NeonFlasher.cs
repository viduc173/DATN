using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ALIyerEdon
{
    public class NeonFlasher : MonoBehaviour
    {
        public float delay = 1f;
        public float smoothTime = 1f;
        public float minExposure = 1f;
        public float maxExposure = 3f;
        public Color color;

        Material mat;
        bool state;

        IEnumerator Start()
        {
            mat = GetComponent<MeshRenderer>().sharedMaterial;
            mat.SetColor("_EmissionColor", color * minExposure);

            while (true)
            {

                yield return new WaitForSeconds(delay);

                state = !state;
            }
        }

        float exposureValue;

        void Update()
        {

            if (state)
            {
                exposureValue = Mathf.Lerp(minExposure, maxExposure, Time.deltaTime * smoothTime);
                mat.SetColor("_EmissionColor", color * exposureValue);
            }
            else
            {
                exposureValue = Mathf.Lerp(maxExposure, minExposure, Time.deltaTime * smoothTime);
                mat.SetColor("_EmissionColor", color * exposureValue);
            }
        }
    }
}