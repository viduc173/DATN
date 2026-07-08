using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using UnityEngine.InputSystem;

namespace ALIyerEdon
{
    public enum UI_Selection_Mode
    {
        UpDown,
        LeftRight
    }

    public class Car_UI_Selection : MonoBehaviour
    {
        public UI_Selection_Mode selectMode;

        public Button selectButton;
        public Button backButton;
        public Button nextButton;
        public Button prevButton;

        // Gamepad 
        bool isAxis_7th;

        // Update is called once per frame
        void Update()
        {
            #region NextKey
            if (selectMode == UI_Selection_Mode.UpDown)
            {
                if (Gamepad.current != null)
                {
                    if (Gamepad.current.dpad.up.wasPressedThisFrame)
                        nextButton.onClick.Invoke();
                }
                else
                {
                    if (Keyboard.current != null)
                    {
                        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                            nextButton.onClick.Invoke();
                    }
                }
            }
            if (selectMode == UI_Selection_Mode.LeftRight)
            {
                if (Gamepad.current != null)
                {
                    if (Gamepad.current.dpad.right.wasPressedThisFrame)
                        nextButton.onClick.Invoke();
                }
                else
                {
                    if (Keyboard.current != null)
                    {
                        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                            nextButton.onClick.Invoke();
                    }
                }
            }
            #endregion

            #region PrevKey
            if (selectMode == UI_Selection_Mode.UpDown)
            {
                if (Gamepad.current != null)
                {
                    if (Gamepad.current.dpad.down.wasPressedThisFrame)
                        prevButton.onClick.Invoke();
                }
                else
                {
                    if (Keyboard.current != null)
                    {
                        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                            prevButton.onClick.Invoke();
                    }
                }
            }
            if (selectMode == UI_Selection_Mode.LeftRight)
            {
                if (Gamepad.current != null)
                {
                    if (Gamepad.current.dpad.left.wasPressedThisFrame)
                        prevButton.onClick.Invoke();
                }
                else
                {
                    if (Keyboard.current != null)
                    {
                        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                            prevButton.onClick.Invoke();
                    }
                }
            }
                #endregion

            #region EnterKey
                if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                    selectButton.onClick.Invoke();
            }
            else
            {
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.enterKey.wasPressedThisFrame)
                        selectButton.onClick.Invoke();
                }
            }
            #endregion

            #region BackKey
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonEast.wasPressedThisFrame)
                    backButton.onClick.Invoke();
            }
            else
            {
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.escapeKey.wasPressedThisFrame
                    || Keyboard.current.backspaceKey.wasPressedThisFrame)
                        backButton.onClick.Invoke();
                }
            }
            #endregion
        }
    }
}