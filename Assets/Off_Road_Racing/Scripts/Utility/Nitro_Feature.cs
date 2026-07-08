//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ALIyerEdon
{
    public enum NitroBoostPower
    {
        X1,X2,X3
    }

    public class Nitro_Feature : MonoBehaviour
    {
        [Space(10)]
        // Enabled or disable nitro feature for this player car
        public bool enableNitro = true;

        [Space(5)]
        // Add force amount to the player rigidbody at nitro
        public NitroBoostPower nitroBoost = NitroBoostPower.X2;

        // Reduce nitro rate when is using
        public float reduceRate = 50f;
        public float increaseRate = 10f;
        public float nitroLightIntensity = 1f;

        // Nitro sound effect
        public AudioSource nitroSource;
        float nitroVolume;

        public Light[] NitroLights;

        // Nitro particle effects
        public GameObject[] nitroParticles;

        [HideInInspector] public float Amount = 100f;
        [HideInInspector] public bool raceIsStarted;

        void Start()
        {
            nitroVolume = nitroSource.volume;

            if (PlayerPrefs.GetString("CarSFX") == "Low")
            {
                nitroSource.volume = 0.140f;
            }
            if (PlayerPrefs.GetString("CarSFX") == "Medium")
            {
                nitroSource.volume = 0.190f;

            }
            if (PlayerPrefs.GetString("CarSFX") == "High")
            {
                nitroSource.volume = 0.240f;

            }

            for (int a = 0; a <  nitroParticles.Length; a++)
            {
                var emi = nitroParticles[a].GetComponent<ParticleSystem>().emission;
                emi.enabled = false;
            }

            Toggle_Nitro_Lights(false);
        }

        public void Toggle_Nitro_Lights(bool state)
        {
            for (int a = 0; a < NitroLights.Length; a++)
            {
                if (state)
                {
                    NitroLights[a].intensity =
                                   Mathf.Lerp(NitroLights[a].intensity, nitroLightIntensity, Time.deltaTime * 10f);
                }
                else
                {
                    NitroLights[a].intensity =
                                                       Mathf.Lerp(NitroLights[a].intensity, 0, Time.deltaTime * 10f);
                }
            }
        }
    }
}