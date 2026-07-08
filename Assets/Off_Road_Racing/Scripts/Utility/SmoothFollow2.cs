//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ALIyerEdon
{
    public class SmoothFollow2 : MonoBehaviour
    {
        public Transform target;

        public float smooth = 0.33f;
        public float distance = 4.56f;
        public float height = 0.5f;
        public float Angle = 6.39f;
        public float YOffset = 0;
        [HideInInspector] public float daynamicCameraIntensity = 0.5f;
        [HideInInspector] public bool isReversing = false;
        [HideInInspector] public bool dashboardCameraMode;

        public LayerMask lineOfSightMask = 0;

        private Rigidbody myRigidbody;
        private float yVelocity = 0.0f;
        private float xVelocity = 0.0f;

        bool started;
        [HideInInspector] public bool trackCameraMode;
        [HideInInspector] public Camera currentCamera;
        Race_Manager raceManager;

        IEnumerator Start()
        {
            currentCamera = GetComponentInChildren<Camera>();

            yield return new WaitForEndOfFrame();

            raceManager = FindFirstObjectByType<Race_Manager>();

            if (!target)
                target = GameObject.FindGameObjectWithTag("Player").transform;

            myRigidbody = target.GetComponent<Rigidbody>();

            //Face the camera to the car forward at start
            transform.position = new Vector3(target.position.x,
                target.position.y + 300f, target.position.z);

            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                var playerAI = playerObj.GetComponent<Car_AI>();
                if (playerAI != null)
                    transform.LookAt(playerAI.rayPositionCenter);
                else
                    transform.LookAt(playerObj.transform);
            }

            started = true;
        }
        public void SwitchTarget(bool dashboardCamera)
        {
            dashboardCameraMode = dashboardCamera;
            if (dashboardCamera)
                target = GameObject.Find("Dashboard Camera").transform;
            else
                target = GameObject.FindGameObjectWithTag("Player").transform;
        }

        // Update is called once per frame
        void Update()
        {
            if (!started)
                return;
            if (trackCameraMode)
            {
                transform.position = raceManager.trackCamera.currentCamera.position;

                if (currentCamera.fieldOfView > 15f)
                    currentCamera.fieldOfView = 100f - Vector3.Distance(transform.position, target.position);
                else
                    currentCamera.fieldOfView = 15f;

                if (target)
                    transform.LookAt(target.transform.position);
            }
            else
            {
                if (isReversing)
                    YOffset = 180f;
                else
                {
                    if (Gamepad.current != null)
                    {
                        if (Gamepad.current.rightStick.ReadValue().x != 0)
                            YOffset = (90f * (Gamepad.current.rightStick.ReadValue().x));
                        else
                            YOffset = 0;
                    }
                    else
                    {
                        if (Keyboard.current != null)
                        {
                            if (Keyboard.current.rightArrowKey.ReadValue() > 0
                            || Keyboard.current.leftArrowKey.ReadValue() > 0)
                                YOffset = (90f * ((-Keyboard.current.leftArrowKey.ReadValue()) + Keyboard.current.rightArrowKey.ReadValue()));
                            else
                            {
                                if (Keyboard.current.downArrowKey.ReadValue() > 0)
                                    YOffset = (180f * Keyboard.current.downArrowKey.ReadValue());
                                else
                                    YOffset = 0;
                            }
                        }
                    }
                }

                // Damp angle from current y-angle towards target y-angle
                float xAngle = Mathf.SmoothDampAngle(transform.eulerAngles.x,
                target.eulerAngles.x + Angle, ref xVelocity, smooth);

                float yAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y,
                target.eulerAngles.y + YOffset, ref yVelocity, smooth);

                // Look at the target
                transform.eulerAngles = new Vector3(xAngle, yAngle, 0.0f);

                var direction = transform.rotation * -Vector3.forward;
                var targetDistance = AdjustLineOfSight(target.position + new Vector3(0, height, 0), direction);

                transform.position = target.position + new Vector3(0, height, 0) + direction * targetDistance;
            }
        }

        float AdjustLineOfSight(Vector3 target, Vector3 direction)
        {
            RaycastHit hit;

            if (Physics.Raycast(target, direction, out hit, distance, lineOfSightMask.value))
                return hit.distance;
            else
                return distance;
        }
    }
}