using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ALIyerEdon
{
    public class Start_Neons : MonoBehaviour
    {
        public Material[] neonMaterials;
        public float emissionIntensity = 1.27f;
        public void Update_Neon(Color color)
        {
            foreach (Material m in neonMaterials)
            {
                m.color = color;
                m.SetColor("_EmissionColor",
                    color * emissionIntensity);
            }
        }
    }
}