//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using ALIyerEdon;
using UnityEngine.InputSystem;

namespace ALIyerEdon
{
    public enum Control_Type
    {
        Keyboard, Gamepad, Touch
    }

    public class InputSystem : MonoBehaviour
    {

        [Tooltip("Automatically switch between keyboard and mobile controls based on the running platform")]
        public bool autoSwitchPlatform = true;
        // Select control type => Touch or keyboard
        [Tooltip("Keyboard/Gamepad for pc/console and Touch for mobile platforms")]
        public Control_Type controlMode;

        // Enable or diable buttons icon based on the control type
        [Header("UI Icons")]
        [Space(5)]
        public GameObject[] gamepadUI;
        public GameObject[] keyboardUI;

        EasyCarController controller;

        float motorInput;
        float steerInput;
        bool handBrake;

        [Header("Components")]
        ALIyerEdon.Joystick vJoystick;
        bool sWheelControl;

        public GameObject joystick;
        public GameObject arrowKeys;

        // Accelerometer controlling
        [Header("Accelerometer")]
        public float accelSensibility = 10f;
        public float accelSmooth = 0.5f;
        Vector3 curAc;
        bool accelInput;

        [HideInInspector] public bool canStartRace;
        [HideInInspector] public bool finishedRace;
        [HideInInspector] public bool raceIsStarted;
        [HideInInspector] public bool canControl = false;

        Start_Cutscene startCutscene;

        IEnumerator Start()
        {
            Update_ControlMode();

            if (FindFirstObjectByType<Start_Cutscene>())
                startCutscene = FindFirstObjectByType<Start_Cutscene>();

            yield return new WaitForEndOfFrame();

            controller = GameObject.FindGameObjectWithTag("Player")
                .GetComponent<EasyCarController>();

            GameObject.FindGameObjectWithTag("Player")
                .GetComponent<Car_AI>().enabled = false;
        }

        void Update()
        {

            if (accelInput)
            {
                // Controll steering (mobile)	
                // 		
                if (Input.acceleration.x > 0.2f || Input.acceleration.x < -0.2f)
                {
                    steerInput = Input.acceleration.x * Time.deltaTime * accelSensibility;
                }
                else
                {
                    steerInput = 0;
                }

            }

            if (sWheelControl)
                steerInput = vJoystick.GetHorizontal(0) * Time.deltaTime * 23;


            if (controlMode != Control_Type.Touch)
            {
                #region Start Race
                // Start race button
                if (canStartRace && !raceIsStarted)
                {
                    // Start race
                    if (Gamepad.current != null)
                    {
                        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                            FindFirstObjectByType<Race_Manager>().StartRace_Button();
                    }
                    else
                    {
                        if (Keyboard.current != null)
                        {
                            if (Keyboard.current.enterKey.wasPressedThisFrame)
                            {
                                FindFirstObjectByType<Race_Manager>().StartRace_Button();
                            }
                        }
                    }

                    // Exit Race
                    if (Gamepad.current != null)
                    {
                        if (Gamepad.current.buttonEast.wasPressedThisFrame)
                            FindFirstObjectByType<Pause_Menu>().Exit();
                    }
                    else
                    {
                        if (Keyboard.current != null)
                        {
                            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                                FindFirstObjectByType<Pause_Menu>().Exit();
                        }
                    }
                }

                #endregion

                #region Finish Race
                // Finish race button
                if (finishedRace)
                {
                    // Restart race
                    if (Gamepad.current != null)
                    {
                        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                            FindFirstObjectByType<Pause_Menu>().Restart();
                    }
                    else
                    {
                        if (Keyboard.current != null)
                        {
                            if (Keyboard.current.enterKey.wasPressedThisFrame)
                            {
                                FindFirstObjectByType<Pause_Menu>().Restart();
                            }
                        }
                    }

                    // Exit Race
                    if (Gamepad.current != null)
                    {
                        if (Gamepad.current.buttonEast.wasPressedThisFrame)
                            FindFirstObjectByType<Pause_Menu>().Exit();
                    }
                    else
                    {
                        if (Keyboard.current != null)
                        {
                            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                                FindFirstObjectByType<Pause_Menu>().Exit();
                        }
                    }
                }

                #endregion

                #region Skip Cutscene
                if (startCutscene && startCutscene.canSkip && !startCutscene.skipped
                    && FindFirstObjectByType<InputSystem>().canStartRace != true)
                {
                    // Gamepad select button to skip cutscene
                    if (Gamepad.current != null)
                    {
                        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                        {
                            startCutscene.skipped = true;
                            startCutscene.Skip_Cutscene();
                            canStartRace = true;
                        }
                    }
                    else
                    {
                        // Keyboard tab key to skip cutscene
                        if (Keyboard.current != null)
                        {
                            if (Keyboard.current.enterKey.wasPressedThisFrame)
                            {
                                startCutscene.skipped = true;
                                startCutscene.Skip_Cutscene();
                                canStartRace = true;
                            }
                        }
                    }
                }
                #endregion
            }

            if (controller && canControl)
            {
                if (controlMode != Control_Type.Touch)
                {
                    #region Throttle
                    // Throttle input
                    if (Gamepad.current != null)
                    {
                        motorInput =
                                     Gamepad.current.rightTrigger.ReadValue() +
                                     (-Gamepad.current.leftTrigger.ReadValue());
                    }
                    else
                    {
                        if (Keyboard.current != null)
                        {
                            motorInput = Keyboard.current.wKey.ReadValue()
                                     + (-Keyboard.current.sKey.ReadValue());
                        }
                    }
                    #endregion

                    #region Steer
                    // Steer input
                    if (Gamepad.current != null)
                    {
                        steerInput = Gamepad.current.leftStick.ReadValue().x;
                    }
                    else
                    {
                        if (Keyboard.current != null)
                        {
                            steerInput = (-Keyboard.current.aKey.ReadValue()) +
                                        Keyboard.current.dKey.ReadValue();
                        }
                    }
                    #endregion

                    #region Handbrake
                    // Hand brake
                    if (Gamepad.current != null)
                    {
                        if (Gamepad.current.buttonEast.ReadValue() > 0)
                            handBrake = true;
                        else
                            handBrake = false;
                    }
                    else
                    {
                        if (Keyboard.current != null)
                        {
                            if (Keyboard.current.spaceKey.ReadValue() > 0)
                                handBrake = true;
                            else
                                handBrake = false;
                        }
                    }
                    #endregion

                    #region Camera Switch
                    // Camera switch
                    if (Gamepad.current != null)
                    {
                        if (Gamepad.current.buttonNorth.wasPressedThisFrame)
                            FindFirstObjectByType<CameraSwitch>().NextCamera();
                    }
                    else
                    {
                        if (Keyboard.current != null)
                        {
                            if (Keyboard.current.cKey.wasPressedThisFrame)
                                FindFirstObjectByType<CameraSwitch>().NextCamera();
                        }
                    }
                    #endregion

                    #region Pause
                    // Pause
                    if (Gamepad.current != null)
                    {
                        if (Gamepad.current.startButton.wasPressedThisFrame)
                            FindFirstObjectByType<Pause_Menu>().Pause();
                    }
                    else
                    {
                        if (Keyboard.current != null)
                        {
                            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                                FindFirstObjectByType<Pause_Menu>().Pause();
                        }
                    }
                    #endregion
                }
                controller.Move(motorInput, steerInput, handBrake);
            }
        }

        public void LoadLevel(string name)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(name);
        }

        public void Pause_Game()
        {
            FindFirstObjectByType<Pause_Menu>().Pause();
        }

        public void Switch_Camera()
        {
            FindFirstObjectByType<CameraSwitch>().NextCamera();
        }

        public void Throttle()
        {
            if (controlMode == Control_Type.Touch)
                motorInput = 1f;
        }

        public void ThrottleRelease()
        {
            if (controlMode == Control_Type.Touch)
                motorInput = 0;
        }

        public void Steer(bool state)
        {
            if (controlMode == Control_Type.Touch)
            {
                if (state)
                    steerInput = Mathf.Lerp(steerInput, 1f, Time.deltaTime * 25);
                else
                    steerInput = Mathf.Lerp(steerInput, -1f, Time.deltaTime * 25);
            }
        }

        public void SteerRelease()
        {
            if (controlMode == Control_Type.Touch)
                steerInput = 0;

        }

        public void Brake(bool state)
        {
            if (controlMode == Control_Type.Touch)
            {
                if (state)
                    motorInput = -1f;
                else
                    motorInput = 0;
            }
        }

        public void Hand_Brake(bool state)
        {
            handBrake = state;
        }

        public void Update_ControlMode()
        {
            if (PlayerPrefs.GetFloat("accelSensibility") == 0)
                PlayerPrefs.SetFloat("accelSensibility", 10f);


            vJoystick = joystick.GetComponent<ALIyerEdon.Joystick>();

            if (autoSwitchPlatform)
            {
#if UNITY_EDITOR || UNITY_WEBGL || UNITY_STANDALONE || UNITY_WSA || UNITY_64
                controlMode = Control_Type.Keyboard;
                Change_ControlType(Control_Type.Keyboard);
#endif
            }
            else
            {
                if (PlayerPrefs.GetString("Control_Mode") == "Low")
                {
                    controlMode = Control_Type.Touch;
                    Change_ControlType(Control_Type.Touch);
                }
                if (PlayerPrefs.GetString("Control_Mode") == "Medium")
                {
                    controlMode = Control_Type.Keyboard;
                    Change_ControlType(Control_Type.Keyboard);
                }
                if (PlayerPrefs.GetString("Control_Mode") == "High")
                {
                    controlMode = Control_Type.Gamepad;
                    Change_ControlType(Control_Type.Gamepad);
                }
            }

            if (PlayerPrefs.GetString("Steer_Mode") == "Low")
            {
                joystick.SetActive(false);
                arrowKeys.SetActive(true);
            }
            if (PlayerPrefs.GetString("Steer_Mode") == "Medium")
            {
                joystick.SetActive(true);
                arrowKeys.SetActive(false);
                sWheelControl = true;
            }
            if (PlayerPrefs.GetString("Steer_Mode") == "High")
            {
                joystick.SetActive(false);
                arrowKeys.SetActive(false);
                accelInput = true;
            }

            accelSensibility = PlayerPrefs.GetFloat("accelSensibility");

        }

        // Update controls icon tips (keyaboard keys or joystick nums (Xbox Controller))
        public void Change_ControlType(Control_Type controlTips)
        {
            if (controlTips == Control_Type.Keyboard)
            {
                FindFirstObjectByType<Race_Manager>().mobileControls.SetActive(false);
                FindFirstObjectByType<Nitro>().Update_UI();

            }
            if (controlTips == Control_Type.Gamepad)
            {
                FindFirstObjectByType<Race_Manager>().mobileControls.SetActive(false);
                FindFirstObjectByType<Nitro>().Update_UI();
            }
            if (controlTips == Control_Type.Touch)
            {
                FindFirstObjectByType<Race_Manager>().mobileControls.SetActive(true);
                FindFirstObjectByType<Nitro>().Update_UI();
            }

            // Select control icon tips
            if (controlTips == Control_Type.Keyboard)
            {
                foreach (GameObject gp in gamepadUI)
                    gp.SetActive(false);

                foreach (GameObject kb in keyboardUI)
                    kb.SetActive(true);
            }

            if (controlTips == Control_Type.Gamepad)
            {
                foreach (GameObject gp in gamepadUI)
                    gp.SetActive(true);

                foreach (GameObject kb in keyboardUI)
                    kb.SetActive(false);
            }
            if (controlTips == Control_Type.Touch)
            {
                foreach (GameObject gp in gamepadUI)
                    gp.SetActive(false);

                foreach (GameObject kb in keyboardUI)
                    kb.SetActive(false);
            }
        }

    }
}