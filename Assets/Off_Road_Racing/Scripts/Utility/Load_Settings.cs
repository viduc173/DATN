//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________
using ALIyerEdon;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ALIyerEdon
{
    public class Load_Settings : MonoBehaviour
    {
        public bool nightMode;
        public bool rainMode;

        public AudioSource music;
        public AudioSource[] trumpetSound;

        IEnumerator Start()
        {

            // Don't apply quality settings for when is in the first runing mode
            if (PlayerPrefs.GetInt("FirstLoadSettings") != 1)
                PlayerPrefs.SetInt("FirstLoadSettings", 1);
            else
                Set_QualityLevel();

            Update_ControlMode();
            Update_SSR();
            Update_MotionBlur();
            Update_DOF();
            Update_AO();
            Update_MusicVolume();
            Update_LocalPosition_UI();
            Update_SSLensFlare();

            yield return new WaitForEndOfFrame();

            // Enable or Disable rain particle for player
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                var carAudio = playerObj.GetComponent<EasyCarAudio>();
                if (carAudio != null && carAudio.rainParticle != null)
                    carAudio.rainParticle.SetActive(rainMode);
            }


            foreach (EasyCarController controller in FindObjectsOfType<EasyCarController>())
            {

                if (nightMode)
                    controller.nightLight = true;
                else
                    controller.nightLight = false;

                controller.Toggle_FrontLights(nightMode);
            }
        }

        public void Update_MusicVolume()
        {
            if (PlayerPrefs.GetString("MusicVolume") == "Low")
                music.volume = 0.1f;
            if (PlayerPrefs.GetString("MusicVolume") == "Medium")
                music.volume = 0.3f;
            if (PlayerPrefs.GetString("MusicVolume") == "High")
                music.volume = 0.5f;

            if (trumpetSound.Length > 0)
            {
                foreach (AudioSource audio in trumpetSound)
                {
                    if (PlayerPrefs.GetString("MusicVolume") == "Low")
                        audio.volume = 0.1f;
                    if (PlayerPrefs.GetString("MusicVolume") == "Medium")
                        audio.volume = 0.3f;
                    if (PlayerPrefs.GetString("MusicVolume") == "High")
                        audio.volume = 0.5f;
                }
            }
        }
        public void Update_CarSFX()
        {
            foreach (EasyCarAudio carAudio in FindObjectsOfType<EasyCarAudio>())
                carAudio.Update_VolumeSettings();
        }

        public void Update_AO()
        {
            /*Volume volume = FindFirstObjectByType<Volume>();

            ScreenSpaceAmbientOcclusion AO;
            volume.profile.TryGet<ScreenSpaceAmbientOcclusion>(out AO);

            if (PlayerPrefs.GetString("AO") == "On")
                AO.active = true;
            else
                AO.active = false;*/
        }

        public void Update_DOF()
        {
            Volume volume = FindFirstObjectByType<Volume>();

            DepthOfField dof;
            volume.profile.TryGet<DepthOfField>(out dof);

            if (PlayerPrefs.GetString("DOF") == "On")
                dof.active = true;
            else
                dof.active = false;
        }

        public void Update_MotionBlur()
        {
            Volume volume = FindFirstObjectByType<Volume>();

            MotionBlur mb;
            volume.profile.TryGet<MotionBlur>(out mb);

            if (PlayerPrefs.GetString("MotionBlur") == "On")
                mb.active = true;
            else
                mb.active = false;
        }

        public void Update_SSR()
        {

            Volume volume = FindFirstObjectByType<Volume>();

            ScreenSpaceReflection ssr;
            volume.profile.TryGet<ScreenSpaceReflection>(out ssr);

            if (PlayerPrefs.GetString("QualityLevel") == "VeryLow"
                || PlayerPrefs.GetString("QualityLevel") == "Low"
                || PlayerPrefs.GetString("QualityLevel") == "Medium")
            {
                ssr.active = false;
            }
            else
            {
                if (PlayerPrefs.GetString("SSR") == "On")
                    ssr.active = true;
                else
                    ssr.active = false;
            }
        }

        public void Update_SSLensFlare()
        {
            Volume volume = FindFirstObjectByType<Volume>();

            ScreenSpaceLensFlare sslf;
            volume.profile.TryGet<ScreenSpaceLensFlare>(out sslf);

            if (PlayerPrefs.GetString("SSLensFlare") == "On")
                sslf.active = true;
            else
                sslf.active = false;
        }

        public void Set_QualityLevel()
        {
            string level = PlayerPrefs.GetString("QualityLevel");

            if (level == "VeryLow")
                QualitySettings.SetQualityLevel(0);
            if (level == "Low")
                QualitySettings.SetQualityLevel(1);
            if (level == "Medium")
                QualitySettings.SetQualityLevel(2);
            if (level == "High")
                QualitySettings.SetQualityLevel(3);
            if (level == "Ultra")
                QualitySettings.SetQualityLevel(4);

            // UniversalRenderPipelineAsset data = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

            // Set render scale to the 1 because we want to control it manually
            /*  if (data)
                  data.renderScale = 1f;*/

            if (level == "VeryLow")
            {

                Screen.SetResolution((int)(PlayerPrefs.GetFloat("VeryLow_width")),
                (int)(PlayerPrefs.GetFloat("VeryLow_height")), true);

                foreach (Camera cam in FindObjectsOfType<Camera>())
                {
                    cam.GetComponent<UniversalAdditionalCameraData>()
                        .antialiasing = AntialiasingMode.None;
                }

                if (FindFirstObjectByType<Terrain>())
                {
                    FindFirstObjectByType<Terrain>().heightmapPixelError = 170f;
                    FindFirstObjectByType<Terrain>().detailObjectDistance = 0;
                }
            }
            if (level == "Low")
            {

                Screen.SetResolution((int)(PlayerPrefs.GetFloat("Low_width")),
            (int)(PlayerPrefs.GetFloat("Low_height")), true);

                foreach (Camera cam in FindObjectsOfType<Camera>())
                {
                    cam.GetComponent<UniversalAdditionalCameraData>()
                        .antialiasing = AntialiasingMode.None;
                }

                if (FindFirstObjectByType<Terrain>())
                {
                    FindFirstObjectByType<Terrain>().heightmapPixelError = 170f;
                    FindFirstObjectByType<Terrain>().detailObjectDistance = 0;
                }
            }
            if (level == "Medium")
            {

                Screen.SetResolution((int)(PlayerPrefs.GetFloat("Medium_width")),
            (int)(PlayerPrefs.GetFloat("Medium_height")), true);

                foreach (Camera cam in FindObjectsOfType<Camera>())
                {
                    cam.GetComponent<UniversalAdditionalCameraData>()
                        .antialiasing = AntialiasingMode.TemporalAntiAliasing;
                    cam.GetComponent<UniversalAdditionalCameraData>()
                            .taaSettings.quality = TemporalAAQuality.Medium;
                }

                if (FindFirstObjectByType<Terrain>())
                {
                    FindFirstObjectByType<Terrain>().heightmapPixelError = 170f;
                    FindFirstObjectByType<Terrain>().detailObjectDistance = 0;
                }
            }
            if (level == "High")
            {
                Screen.SetResolution((int)(PlayerPrefs.GetFloat("High_width")),
            (int)(PlayerPrefs.GetFloat("High_height")), true);

                foreach (Camera cam in FindObjectsOfType<Camera>())
                {
                    cam.GetComponent<UniversalAdditionalCameraData>()
                        .antialiasing = AntialiasingMode.TemporalAntiAliasing;
                    cam.GetComponent<UniversalAdditionalCameraData>()
                            .taaSettings.quality = TemporalAAQuality.VeryHigh;
                }

                if (FindFirstObjectByType<Terrain>())
                {
                    FindFirstObjectByType<Terrain>().heightmapPixelError = 1f;
                    FindFirstObjectByType<Terrain>().detailObjectDistance = 139f;
                }
            }
            if (level == "Ultra")
            {
                Screen.SetResolution((int)(PlayerPrefs.GetFloat("Ultra_width")),
            (int)(PlayerPrefs.GetFloat("Ultra_height")), true);

                foreach (Camera cam in FindObjectsOfType<Camera>())
                {
                    cam.GetComponent<UniversalAdditionalCameraData>()
                        .antialiasing = AntialiasingMode.TemporalAntiAliasing;
                    cam.GetComponent<UniversalAdditionalCameraData>()
                            .taaSettings.quality = TemporalAAQuality.VeryHigh;
                }

                if (FindFirstObjectByType<Terrain>())
                {
                    FindFirstObjectByType<Terrain>().heightmapPixelError = 1f;
                    FindFirstObjectByType<Terrain>().detailObjectDistance = 139f;
                }
            }
            if (level == "Max")
            {
                Screen.SetResolution((int)(PlayerPrefs.GetFloat("OriginalX")),
            (int)(PlayerPrefs.GetFloat("OriginalY")), true);

                foreach (Camera cam in FindObjectsOfType<Camera>())
                {
                    cam.GetComponent<UniversalAdditionalCameraData>()
                        .antialiasing = AntialiasingMode.TemporalAntiAliasing;
                    cam.GetComponent<UniversalAdditionalCameraData>()
                            .taaSettings.quality = TemporalAAQuality.VeryHigh;
                }

                if (FindFirstObjectByType<Terrain>())
                {
                    FindFirstObjectByType<Terrain>().heightmapPixelError = 1f;
                    FindFirstObjectByType<Terrain>().detailObjectDistance = 139f;
                }
            }
            Update_SSR();
        }

        public void Update_DisplaFPS()
        {
            if (FindFirstObjectByType<FPSCounter>())
                FindFirstObjectByType<FPSCounter>().Update_DisplayFPS_UI();
        }

        public void Update_SideUI()
        {
            if (FindFirstObjectByType<Race_Manager>())
                FindFirstObjectByType<Race_Manager>().Update_SideUI();
        }

        public void Update_DynamicCamera()
        {
            if (GameObject.FindGameObjectWithTag("Player")
                .GetComponent<EasyCarController>())
            {
                GameObject.FindGameObjectWithTag("Player")
                    .GetComponent<EasyCarController>().Update_DynamicCamera();
            }
        }

        public void Update_LocalPosition_UI()
        {

            if (FindFirstObjectByType<Race_Manager>())
            {
                if (PlayerPrefs.GetString("Local_Position") == "On")
                    FindFirstObjectByType<Race_Manager>().showLocalPosition = true;
                else
                    FindFirstObjectByType<Race_Manager>().showLocalPosition = false;

                if (PlayerPrefs.GetString("Side_UI") == "On")
                    FindFirstObjectByType<Race_Manager>().positionUI.SetActive(true);
                else
                    FindFirstObjectByType<Race_Manager>().positionUI.SetActive(false);
            }

            // Enable local position display on top of the cars
            foreach (Car_Position carPos in FindObjectsOfType<Car_Position>())
            {
                // Show or hide car position on the top of the car
                carPos.GetComponent<Car_Position>().displayPosition =
                    FindFirstObjectByType<Race_Manager>().showLocalPosition;
            }
        }

        public void Update_ControlMode()
        {
            if (FindFirstObjectByType<InputSystem>())
                FindFirstObjectByType<InputSystem>().Update_ControlMode();
        }

        public void Update_DifficultyLevel()
        {
            foreach (DifficultyLevel diff in FindObjectsOfType<DifficultyLevel>())
                diff.Update_DifficultyLevel();
        }
    }
}