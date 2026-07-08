using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ALIyerEdon
{
    public class FPSCounter : MonoBehaviour
    {
        public float SmoothSpeed = 1f;
        float fps, smoothFps;
        public Text text;
        public GameObject panel;

        private void Start()
        {
            Update_DisplayFPS_UI();
        }

        private void Update()
        {
            fps = 1f / Time.unscaledDeltaTime;
            if (Time.timeSinceLevelLoad < 0.1f) smoothFps = fps;
            smoothFps += (fps - smoothFps) * Mathf.Clamp(Time.unscaledDeltaTime * SmoothSpeed, 0, 1);
            text.text = ((int)smoothFps).ToString() + " fps";
        }

        public void Update_DisplayFPS_UI()
        {
            if (PlayerPrefs.GetString("Display_FPS") == "On")
                panel.SetActive(true);
            else
                panel.SetActive(false);
        }
    }
}