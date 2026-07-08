//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using UnityEngine;
using System.Collections;
using ALIyerEdon;

namespace ALIyerEdon
{
    public class CameraSwitch : MonoBehaviour
    {
        [SerializeField]
        public CameraView[] cameraView;

        // Hold curent active camera id
        int currentCamera = 0;
        SmoothFollow2 smoothFollow;
        Race_Manager raceManager;

        void Start()
        {
            smoothFollow = FindFirstObjectByType<SmoothFollow2>();
            raceManager = FindFirstObjectByType<Race_Manager>();

            if (raceManager.trackCamera)
                raceManager.trackCamera.enabled = false;

            smoothFollow.smooth = cameraView[currentCamera].Smooth;
            smoothFollow.distance = cameraView[currentCamera].Distance;
            smoothFollow.height = cameraView[currentCamera].Height;
            smoothFollow.Angle = cameraView[currentCamera].Angle;
        }

#if UNITY_EDITOR
        void Update()
        {
            if (cameraView[currentCamera].captureCurrentView)
            {
                cameraView[currentCamera].captureCurrentView = false;

                cameraView[currentCamera].Smooth =
                    FindFirstObjectByType<SmoothFollow2>().smooth;
                cameraView[currentCamera].Distance =
                   FindFirstObjectByType<SmoothFollow2>().distance;
                cameraView[currentCamera].Height =
                   FindFirstObjectByType<SmoothFollow2>().height;
                cameraView[currentCamera].Angle =
                   FindFirstObjectByType<SmoothFollow2>().Angle;
            }

        }
#endif
        public void SelectCamera(int id)
        {
            currentCamera = id;
            NextCamera();
        }

        // Switch to next camera based total camera counts
        public void NextCamera()
        {
            if (currentCamera < cameraView.Length - 1)
            {
                currentCamera++;

                if (cameraView[currentCamera].trackCamera
                    && raceManager.trackCamera == null)
                {
                    if (currentCamera < cameraView.Length - 1)
                        currentCamera++;
                    else
                        currentCamera = 0;
                }
            }
            else
                currentCamera = 0;

            if (cameraView[currentCamera].trackCamera
                && raceManager.trackCamera != null)
            {
                smoothFollow.trackCameraMode = true;

                GameObject.FindGameObjectWithTag("Player").GetComponent
                    <EasyCarController>().trackCameraMode = true;
            }
            else
            {
                smoothFollow.trackCameraMode = false;

                GameObject.FindGameObjectWithTag("Player").GetComponent
                    <EasyCarController>().trackCameraMode = false;

                smoothFollow.smooth = cameraView[currentCamera].Smooth;
                smoothFollow.distance = cameraView[currentCamera].Distance;
                smoothFollow.height = cameraView[currentCamera].Height;
                smoothFollow.Angle = cameraView[currentCamera].Angle;
            }
        }



        [System.Serializable]
        public class CameraView
        {
            public float Smooth;
            public float Distance;
            public float Height;
            public float Angle;
            public bool captureCurrentView;
            public bool trackCamera;
        }
    }
}