using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using UnityEngine.InputSystem;
namespace ALIyerEdon
{
    public class UI_Selection : MonoBehaviour
    {
        public bool updateSplash;

        public bool weatherSplash;

        public Image splashImage;

        public Sprite[] levelSplashes;

        public UI_Selection_Mode selectMode;

        public GameObject[] SelectionImages;

        public Button[] targetButtons;

        public Button backButtons;

        public float selectionScale = 0.2f;

        public int currentSelection;

        void Start()
        {
            Select_Button(currentSelection);
        }

        // Update is called once per frame
        void Update()
        {
            foreach (Button bbb in targetButtons)
            {
                bbb.transform.localScale =
                    new Vector3(1, 1, 1);
            }

            targetButtons[currentSelection].transform.localScale =
                new Vector3(1 + selectionScale, 1 + selectionScale, 1 + selectionScale);

            #region NextKey
            if (selectMode == UI_Selection_Mode.UpDown)
            {
                if (Gamepad.current != null)
                {
                    if (Gamepad.current.dpad.up.wasPressedThisFrame)
                        Next_Button();
                }
                else
                {
                    if (Keyboard.current != null)
                    {
                        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
                            Next_Button();
                    }
                }
            }
            if (selectMode == UI_Selection_Mode.LeftRight)
            {
                if (Gamepad.current != null)
                {
                    if (Gamepad.current.dpad.left.wasPressedThisFrame)
                        Next_Button();
                }
                else
                {
                    if (Keyboard.current != null)
                    {
                        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                            Next_Button();
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
                        Prev_Button();
                }
                else
                {
                    if (Keyboard.current != null)
                    {
                        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
                            Prev_Button();
                    }
                }
            }
            if (selectMode == UI_Selection_Mode.LeftRight)
            {
                if (Gamepad.current != null)
                {
                    if (Gamepad.current.dpad.right.wasPressedThisFrame)
                        Prev_Button();
                }
                else
                {
                    if (Keyboard.current != null)
                    {
                        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                            Prev_Button();
                    }
                }
            }
            #endregion

            #region EnterKey
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                    Enter_Button();
            }
            else
            {
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.enterKey.wasPressedThisFrame)
                        Enter_Button();
                }
            }
            #endregion

            #region BackKey
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonEast.wasPressedThisFrame)
                {
                    if (backButtons)
                        backButtons.onClick.Invoke();
                }
            }
            else
            {
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.escapeKey.wasPressedThisFrame
                    || Keyboard.current.backspaceKey.wasPressedThisFrame)
                    {
                        if (backButtons)
                            backButtons.onClick.Invoke();
                    }
                }
            }
            #endregion

        }

        public void Prev_Button()
        {
            if (currentSelection < targetButtons.Length - 1)
                currentSelection++;
            else
                currentSelection = 0;

            Select_Button(currentSelection);

            if (FindFirstObjectByType<MainUtility>())
                FindFirstObjectByType<MainUtility>().Click_Sound();
            else
            {
                if (FindFirstObjectByType<Pause_Menu>())
                    FindFirstObjectByType<Pause_Menu>().Click_Sound();
            }
        }

        public void Next_Button()
        {
            if (currentSelection > 0)
                currentSelection--;
            else
                currentSelection = targetButtons.Length - 1;

            Select_Button(currentSelection);

            if (FindFirstObjectByType<MainUtility>())
                FindFirstObjectByType<MainUtility>().Click_Sound();
            else
            {
                if (FindFirstObjectByType<Pause_Menu>())
                    FindFirstObjectByType<Pause_Menu>().Click_Sound();
            }
        }

        public void Select_Button(int id)
        {
            if (targetButtons.Length == SelectionImages.Length)
            {
                for (int a = 0; a < SelectionImages.Length; a++)
                {
                    SelectionImages[a].SetActive(false);
                }

                SelectionImages[id].SetActive(true);
            }

            if (updateSplash)
            {
                splashImage.sprite = levelSplashes[currentSelection];
            }

            if (weatherSplash)
            {
                if (PlayerPrefs.GetInt("LevelID") == 0)
                {
                    splashImage.sprite = GetComponent<LevelSelect>().weatherSplashes_1[currentSelection];

                    for (int a = 0; a < targetButtons.Length; a++)
                        targetButtons[a].GetComponent<Image>().sprite = GetComponent<LevelSelect>().weatherSplashes_1[a];
                }
                if (PlayerPrefs.GetInt("LevelID") == 1)
                {
                    splashImage.sprite = GetComponent<LevelSelect>().weatherSplashes_2[currentSelection];
                    for (int a = 0; a < targetButtons.Length; a++)
                        targetButtons[a].GetComponent<Image>().sprite = GetComponent<LevelSelect>().weatherSplashes_2[a];
                }
                if (PlayerPrefs.GetInt("LevelID") == 2)
                {
                    splashImage.sprite = GetComponent<LevelSelect>().weatherSplashes_3[currentSelection];
                    for (int a = 0; a < targetButtons.Length; a++)
                        targetButtons[a].GetComponent<Image>().sprite = GetComponent<LevelSelect>().weatherSplashes_3[a];
                }
                if (PlayerPrefs.GetInt("LevelID") == 3)
                {
                    splashImage.sprite = GetComponent<LevelSelect>().weatherSplashes_4[currentSelection];
                    for (int a = 0; a < targetButtons.Length; a++)
                        targetButtons[a].GetComponent<Image>().sprite = GetComponent<LevelSelect>().weatherSplashes_4[a];
                }
                if (PlayerPrefs.GetInt("LevelID") == 4)
                {
                    splashImage.sprite = GetComponent<LevelSelect>().weatherSplashes_5[currentSelection];
                    for (int a = 0; a < targetButtons.Length; a++)
                        targetButtons[a].GetComponent<Image>().sprite = GetComponent<LevelSelect>().weatherSplashes_5[a];
                }
            }
        }

        public void Set_CurrentSelection(int ID)
        {
            currentSelection = ID;
        }

        public void Enter_Button()
        {
            targetButtons[currentSelection].onClick.Invoke();
        }

        public void UpdateSplash()
        {
            if (updateSplash)
            {
                splashImage.sprite = levelSplashes[currentSelection];
            }

            if (weatherSplash)
            {
                if (PlayerPrefs.GetInt("LevelID") == 0)
                {
                    splashImage.sprite = GetComponent<LevelSelect>().weatherSplashes_1[currentSelection];
                    
                    for(int a = 0; a < targetButtons.Length ; a++)
                        targetButtons[a].GetComponent<Image>().sprite = GetComponent<LevelSelect>().weatherSplashes_1[a];
                }
                if (PlayerPrefs.GetInt("LevelID") == 1)
                { 
                    splashImage.sprite = GetComponent<LevelSelect>().weatherSplashes_2[currentSelection];
                    for (int a = 0; a < targetButtons.Length; a++)
                        targetButtons[a].GetComponent<Image>().sprite = GetComponent<LevelSelect>().weatherSplashes_2[a];
                }
                if (PlayerPrefs.GetInt("LevelID") == 2)
                { 
                    splashImage.sprite = GetComponent<LevelSelect>().weatherSplashes_3[currentSelection];
                    for (int a = 0; a < targetButtons.Length; a++)
                        targetButtons[a].GetComponent<Image>().sprite = GetComponent<LevelSelect>().weatherSplashes_3[a];
                }
                if (PlayerPrefs.GetInt("LevelID") == 3)
                { 
                    splashImage.sprite = GetComponent<LevelSelect>().weatherSplashes_4[currentSelection];
                    for (int a = 0; a < targetButtons.Length; a++)
                        targetButtons[a].GetComponent<Image>().sprite = GetComponent<LevelSelect>().weatherSplashes_4[a];
                }
                if (PlayerPrefs.GetInt("LevelID") == 4)
                {  
                    splashImage.sprite = GetComponent<LevelSelect>().weatherSplashes_5[currentSelection];
                    for (int a = 0; a < targetButtons.Length; a++)
                        targetButtons[a].GetComponent<Image>().sprite = GetComponent<LevelSelect>().weatherSplashes_5[a];
                }
            }
        }
    }
}