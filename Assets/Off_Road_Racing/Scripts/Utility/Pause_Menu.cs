//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ALIyerEdon;

namespace ALIyerEdon
{
    public class Pause_Menu : MonoBehaviour
    {
        public GameObject pauseMenu;
        public Text Loading;
        public AudioSource clickSound;

        public string GarageScene = "Garage";

        [HideInInspector] public bool raceIsStarted = false;

        public void Pause()
        {
            if (raceIsStarted)
            {
                Cursor.visible = true;
                AudioListener.volume = 0;
                Time.timeScale = 0;
                pauseMenu.SetActive(true);
            }
        }

        public void Resume()
        {
#if UNITY_EDITOR
            Cursor.visible = true;
#else
            Cursor.visible = false;
#endif
            AudioListener.volume = 1f;
            Time.timeScale = FindFirstObjectByType<Race_Manager>().timeScale;
            pauseMenu.SetActive(false);
        }

        public void Restart()
        {
            AudioListener.volume = 0;
            Time.timeScale = FindFirstObjectByType<Race_Manager>().timeScale;
            Loading.text = "Loading...";
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
        public void Exit()
        {
            AudioListener.volume = 0;
            Time.timeScale = FindFirstObjectByType<Race_Manager>().timeScale;
            Loading.text = "Loading...";
            UnityEngine.SceneManagement.SceneManager.LoadScene(GarageScene);
        }

        public void SetTrue(GameObject target)
        {
            target.SetActive(true);
        }

        public void SetFalse(GameObject target)
        {
            target.SetActive(false);
        }

        public void ToggleObject(GameObject target)
        {
            target.SetActive(!target.activeSelf);
        }

        public void Click_Sound()
        {
            if (clickSound)
                clickSound.PlayOneShot(clickSound.clip);
        }

        public void Disable_UI_Selection(UI_Selection target)
        {
            target.enabled = false;
        }

        public void Enable_UI_Selection(UI_Selection target)
        {
            target.enabled = true;
        }
    }
}