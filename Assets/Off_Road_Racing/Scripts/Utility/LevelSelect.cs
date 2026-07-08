//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using ALIyerEdon;

namespace ALIyerEdon
{
	public enum LevelType
	{
		Stage,Weather
	}

	public class LevelSelect : MonoBehaviour
	{

		[Header("UI Elements ____________________________________________________" +
			"____________________________________________________")]
		[Space(5)]
		public LevelType levelType;
        // Enable car select menu
        public GameObject backgroundUI;
		public GameObject carSelectUI;
		public GameObject weatherSelectUI;
		public GameObject purchaseUI;

		// Display total scores
		public Text TotalScores;
		public Text purchaseInfo;

		[Header("Levels Info ____________________________________________________" +
			"____________________________________________________")]
		[Space(5)]
		// Each level value
		public int[] levelPrices;

		//TotalScore text, level value text
		public Text[] levelPriceText;

		public GameObject[] levelLocks;

        public Sprite[] weatherSplashes_1;
        public Sprite[] weatherSplashes_2;
        public Sprite[] weatherSplashes_3;
        public Sprite[] weatherSplashes_4;
        public Sprite[] weatherSplashes_5;

        [Header("Awards ____________________________________________________" +
			"____________________________________________________")]
		[Space(5)]
		public Sprite goldAward;
		public Sprite bronzeAward;
		public Sprite silverAward;
		public Sprite noneAward;

		[Space(5)]
		public Image[] awardIcons;

		// MainLevel name
		public string[] levelNames;

		void Start()
		{
			purchaseInfo.text = "";

			// Update total scores display
			TotalScores.text = "Total Coins : " + PlayerPrefs.GetInt("TotalScores").ToString();

			for (int l = 0; l < levelPriceText.Length; l++)
			{
				if (levelType == LevelType.Stage)
				{
					// Enable lock and level price displays
					if (PlayerPrefs.GetInt("Level" + l.ToString()) == 3)
						levelLocks[l].SetActive(false);
					else
						levelLocks[l].SetActive(true);
				}
                if (levelType == LevelType.Weather)
                {
                    // Enable lock and level price displays
                    if (PlayerPrefs.GetInt("Weather" + l.ToString()) == 3)
                        levelLocks[l].SetActive(false);
                    else
                        levelLocks[l].SetActive(true);
                }
            }


			// Update current level value text
			for (int a = 0; a < levelPriceText.Length; a++)
			{
				if (levelType == LevelType.Stage)
				{
					if (PlayerPrefs.GetInt("Level" + a.ToString()) == 3)
						levelPriceText[a].text = "";
					else
						levelPriceText[a].text = "Price : " + levelPrices[a].ToString();
				}
                if (levelType == LevelType.Weather)
                {
                    if (PlayerPrefs.GetInt("Weather" + a.ToString()) == 3)
                        levelPriceText[a].text = "";
                    else
                        levelPriceText[a].text = "Price : " + levelPrices[a].ToString();
                }
            }

			if (levelType == LevelType.Stage)
			{
				// Update award icons
				for (int aw = 0; aw < awardIcons.Length; aw++)
				{
					if (PlayerPrefs.GetInt("Award_Level_" + aw.ToString()) == 0)
						awardIcons[aw].sprite = goldAward;

					if (PlayerPrefs.GetInt("Award_Level_" + aw.ToString()) == 1)
						awardIcons[aw].sprite = bronzeAward;

					if (PlayerPrefs.GetInt("Award_Level_" + aw.ToString()) == 2)
						awardIcons[aw].sprite = silverAward;

					if (PlayerPrefs.GetInt("Award_Level_" + aw.ToString()) == 3)
						awardIcons[aw].sprite = noneAward;

				}
			}
		}

		// Buy current selected level
		public void BuyLevel()
		{
			// Check player have enough money
			if (levelPrices[currentSelectedLevel] <= PlayerPrefs.GetInt("TotalScores"))
			{
				if (levelType == LevelType.Stage)
				{
					PlayerPrefs.SetInt("Level" + currentSelectedLevel.ToString(), 3);
				}
				if (levelType == LevelType.Weather)
				{
                    PlayerPrefs.SetInt("Weather" + currentSelectedLevel.ToString(), 3);
                }

                // Reduce current level price from the total scores
                PlayerPrefs.SetInt("TotalScores",
					PlayerPrefs.GetInt("TotalScores") - levelPrices[currentSelectedLevel]);

				// Disable lock icon for current level
				levelLocks[currentSelectedLevel].SetActive(false);

				// Clear level price text
				levelPriceText[currentSelectedLevel].text = "";

				// Update total scores display
				TotalScores.text = "Total Coins : " + PlayerPrefs.GetInt("TotalScores").ToString();

				purchaseUI.SetActive(false);

				purchaseInfo.text = "Successfully Purchased";

				FindFirstObjectByType<MainUtility>().Enable_UI_Selection(GetComponent<UI_Selection>());
			}
			else
			{
				// Show the shop offer window
				purchaseInfo.text = "Not Enough Coins";
			}
		}

		int currentSelectedLevel = 0;

		// Select current level
		public void SelectLevel(int ID)
		{
			currentSelectedLevel = ID;
			if (levelType == LevelType.Stage)
			{
				if (PlayerPrefs.GetInt("Level" + ID.ToString()) == 3)
				{
					PlayerPrefs.SetInt("LevelID", ID);

					gameObject.SetActive(false);

					backgroundUI.SetActive(false);

                    weatherSelectUI.SetActive(true);

					purchaseUI.SetActive(false);

				}
				else
				{
					purchaseInfo.text = "";

					purchaseUI.GetComponent<UI_Selection>().currentSelection = 0;
                    purchaseUI.SetActive(true);
				}
			}
			if (levelType == LevelType.Weather)
			{
                if (PlayerPrefs.GetInt("Weather" + ID.ToString()) == 3)
                {
                    PlayerPrefs.SetInt("WeatherID", ID);

                    gameObject.SetActive(false);

                    backgroundUI.SetActive(false);

                    carSelectUI.SetActive(true);

                    purchaseUI.SetActive(false);

                }
                else
                {
                    purchaseInfo.text = "";

                    purchaseUI.SetActive(true);
                }
            }
        }
	}
}