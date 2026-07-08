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
    public class Start_Counter : MonoBehaviour
    {
        [ColorUsageAttribute(true, true)]
        public Color color_0,color_1, color_2, color_3, color_4;
        public GameObject counter_0, counter_1,
            counter_2,
            counter_3;

        int currentCount;
        AudioSource counterAudio;
        [HideInInspector] public float timeScale;
        Start_Neons startNeons;

        private void Start()
        {
            if (FindFirstObjectByType<Start_Neons>())
            {
                startNeons = FindFirstObjectByType<Start_Neons>();
                startNeons.Update_Neon(color_0);
            }
        }
        public void StartCounter()
        {
            StartCoroutine(StartRaceCounter());
        }

        IEnumerator StartRaceCounter()
        {

            if (GetComponent<AudioSource>())
                counterAudio = GetComponent<AudioSource>();

            // Hide all counter lights icon
            if(startNeons)
                startNeons.Update_Neon(color_1);

            counter_0.SetActive(true);
            counter_1.SetActive(false);
            counter_2.SetActive(false);
            counter_3.SetActive(false);
            counterAudio.Play();


            // Counter light 1
            yield return new WaitForSeconds(timeScale);
            if (startNeons)
                startNeons.Update_Neon(color_2);
            counter_0.SetActive(false);
            counter_1.SetActive(true);
            counter_2.SetActive(false);
            counter_3.SetActive(false);
            counterAudio.Play();

            // Counter light 2
            yield return new WaitForSeconds(timeScale);
            if (startNeons)
                startNeons.Update_Neon(color_3);
            counter_0.SetActive(false);
            counter_1.SetActive(false);
            counter_2.SetActive(true);
            counter_3.SetActive(false);
            counterAudio.Play();

            // Counter light 3
            yield return new WaitForSeconds(timeScale);
            if (startNeons)
                startNeons.Update_Neon(color_4);
            counter_0.SetActive(false);
            counter_1.SetActive(false);
            counter_2.SetActive(false);
            counter_3.SetActive(true);
            counterAudio.pitch = 1.23f;
            counterAudio.Play();

            yield return new WaitForSeconds(timeScale * 1.5f);

            // Hide all counter lights icon
            if (startNeons)
                startNeons.Update_Neon(color_4);
            counter_0.SetActive(false);
            counter_1.SetActive(false);
            counter_2.SetActive(false);
            counter_3.SetActive(false);

            // Enable local position display on top of the cars
            foreach (Car_Position carPos in FindObjectsOfType<Car_Position>())
            {
                // Show or hide car position on the top of the car
                carPos.GetComponent<Car_Position>().displayPosition =
                    FindFirstObjectByType<Race_Manager>().showLocalPosition;
            }

            yield return new WaitForSeconds(FindFirstObjectByType<EasyCarAudio>().startSkidDuration);

            foreach (EasyCarAudio carAudio in FindObjectsOfType<EasyCarAudio>())
            {
                carAudio.raceIsStarted = true;
            }

            // Disable counter completely
            gameObject.SetActive(false);

        }
    }
}