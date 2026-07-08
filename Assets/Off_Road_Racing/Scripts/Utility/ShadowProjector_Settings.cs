using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ALIyerEdon
{
    public class ShadowProjector_Settings : MonoBehaviour
    {
        void Start()
        {
            if (PlayerPrefs.GetInt("Reflection") == 2)
            {
                gameObject.SetActive(false);
            }
        }
    }
}