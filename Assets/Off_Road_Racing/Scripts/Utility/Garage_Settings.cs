//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ALIyerEdon;

namespace ALIyerEdon
{
    public class Garage_Settings : MonoBehaviour
    {
        public GameObject[] disableObjects;

        // Start is called before the first frame update
        void Awake()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Garage"))
            {
                foreach (GameObject target in disableObjects)
                    target.SetActive(false);
                
                GetComponent<Car_Position>().enabled = false;
                
                GetComponent<Car_AI>().enabled = false;
                
                GetComponent<EasyCarAudio>().Stop_Effects();
                GetComponent<EasyCarAudio>().enabled = false;
                GetComponent<Nitro_Feature>().enabled = false;
                GetComponent<CameraSwitch>().enabled = false;
               
                if(GetComponent<CarDamage>())
                    GetComponent<CarDamage>().enabled = false;

                if (GetComponent<Random_Car_Speed>())
                    GetComponent<Random_Car_Speed>().enabled = false;

                GetComponent<EasyCarController>().Clutch = true;
                GetComponent<EasyCarController>().enableWheelEffects = false;
            }
        }
    }
}