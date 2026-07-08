using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using ALIyerEdon;
#if UNITY_EDITOR || UNITY_WEBGL || UNITY_STANDALONE || UNITY_WSA || UNITY_64 || UNITY_PS4 || UNITY_XBOXONE || UNITY_WEBGL

public class Controller_Update : MonoBehaviour
{

    public float updateInterval = 1f;
    public GameObject gamepadHelp;
    public GameObject keyboardHelp;

    [HideInInspector] public bool isGamepad;

    MainUtility mainUtility;
    ALIyerEdon.InputSystem inputSystem;


    IEnumerator Start()
    {
        if (FindFirstObjectByType<MainUtility>())
            mainUtility = FindFirstObjectByType<MainUtility>();

        if (FindFirstObjectByType<ALIyerEdon.InputSystem>())
            inputSystem = FindFirstObjectByType<ALIyerEdon.InputSystem>();

        if (mainUtility)
            mainUtility.Change_ControlType(Control_Type.Keyboard);

        if (inputSystem)
            inputSystem.Change_ControlType(Control_Type.Keyboard);

        while (true)
        {
            yield return new WaitForSeconds(updateInterval);

            if (Gamepad.all.Count == 0)
            {
                if (isGamepad)
                {
                    isGamepad = false;

                    if (mainUtility)
                       mainUtility.Change_ControlType(Control_Type.Keyboard);
                    
                    if(inputSystem)
                       inputSystem.Change_ControlType(Control_Type.Keyboard);
                }
            }
            else
            {
                if (!isGamepad)
                {
                    isGamepad = true;

                    if (mainUtility)
                        mainUtility.Change_ControlType(Control_Type.Gamepad);
                   
                    if (inputSystem)
                        inputSystem.Change_ControlType(Control_Type.Gamepad);
                }
            }
        }
    }

    public void ShowControl_Help()
    {
        if (isGamepad)
        {
            gamepadHelp.SetActive(true);
            keyboardHelp.SetActive(false);
        }
        else
        {
            gamepadHelp.SetActive(false);
            keyboardHelp.SetActive(true);
        }
      
    }
}
#endif