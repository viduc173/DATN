using System.Collections;
using UnityEngine;

    public class CameraPositionCorrector : MonoBehaviour
    {
        [SerializeField]
        private Transform cameraParent;

        [SerializeField]
        private Transform actualCamera;

        [SerializeField]
        private Transform desiredPosition;

        [SerializeField]
        private int correctionIterations = 5;

        [SerializeField]
        private float correctionSpeed = 10f;

        [SerializeField]
        private bool AutoCorrect = false;

        private void Update()
        {
            if (!AutoCorrect) 
            {
                AutoCorrect = true;
                StartCoroutine(CorrectCameraPositionRoutine());
            }
        }

        public void CorrectCameraPosition()
        {
            StartCoroutine(CorrectCameraPositionRoutine());
        }

        private IEnumerator CorrectCameraPositionRoutine()
        {
            if (cameraParent == null || actualCamera == null || desiredPosition == null)
            {
                Debug.LogWarning("Missing required references for camera correction");
                yield break;
            }

            for (int i = 0; i < correctionIterations; i++)
            {
                Vector3 cameraOffset = actualCamera.position - cameraParent.position;
                Vector3 targetParentPosition = desiredPosition.position - cameraOffset;

                cameraParent.position = Vector3.Lerp(
                    cameraParent.position,
                    targetParentPosition,
                    Time.deltaTime * correctionSpeed
                );

                yield return null;
            }

            Vector3 finalOffset = actualCamera.position - cameraParent.position;
            cameraParent.position = desiredPosition.position - finalOffset;
        }
    }