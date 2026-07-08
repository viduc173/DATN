using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ALIyerEdon
{
    public class Rain_Effect : MonoBehaviour
    {

        public string rainScene = "Race_Track_2";
        public GameObject rainEffect;
        public bool enableWheelEffects = false;

        void Start()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name ==
                rainScene)
            {
                rainEffect.SetActive(true);
                GetComponent<EasyCarController>().enableWheelEffects = enableWheelEffects;
            }
            else
                rainEffect.SetActive(false);
        }
    }
}