using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ALIyerEdon
{
    [ExecuteInEditMode]
    public class Imposter_Exposure : MonoBehaviour
    {
        public Color exposure = Color.white;

        public bool apply = false;
        // Start is called before the first frame update
        void Start()
        {
            Apply_Imposter_Exposure(exposure);
        }

        // Update is called once per frame
        void Update()
        {
            if(apply)
            {
                Apply_Imposter_Exposure(exposure);

                apply = false;
            }
        }

        public void Apply_Imposter_Exposure(Color exposureValue)
        {
            Shader.SetGlobalColor("ImposterExposure", exposureValue);
        }
    }
}