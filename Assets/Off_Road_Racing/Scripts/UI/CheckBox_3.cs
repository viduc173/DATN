using UnityEngine;
using UnityEngine.UI;

namespace ALIyerEdon
{
    public class CheckBox_3 : MonoBehaviour
    {
        public int state;
        public Sprite isVeryLow, isLow, isMedium, isHigh, isUltra, isMax;
        public string keyName = "";

        void Start()
        {
            if (PlayerPrefs.GetString(keyName) == "VeryLow")
            {
                GetComponent<Image>().sprite = isVeryLow;
                state = 0;
            }
            if (PlayerPrefs.GetString(keyName) == "Low")
            {
                GetComponent<Image>().sprite = isLow;
                state = 1;
            }
            if (PlayerPrefs.GetString(keyName) == "Medium")
            {
                GetComponent<Image>().sprite = isMedium;
                state = 2;
            }
            if (PlayerPrefs.GetString(keyName) == "High")
            {
                GetComponent<Image>().sprite = isHigh;
                state = 3;
            }
            if (PlayerPrefs.GetString(keyName) == "Ultra")
            {
                GetComponent<Image>().sprite = isUltra;
                state = 4;
            }
            if (PlayerPrefs.GetString(keyName) == "Max")
            {
                GetComponent<Image>().sprite = isMax;
                state = 5;
            }
        }

        public void Update_State()
        {
            if (state < 5)
                state += 1;
            else
                state = 0;

            if (state == 0)
            {
                GetComponent<Image>().sprite = isVeryLow;

                PlayerPrefs.SetString(keyName, "VeryLow");
            }
            if (state == 1)
            {
                GetComponent<Image>().sprite = isLow;

                PlayerPrefs.SetString(keyName, "Low");
            }
            if (state == 2)
            {
                GetComponent<Image>().sprite = isMedium;

                PlayerPrefs.SetString(keyName, "Medium");
            }
            if (state == 3)
            {
                GetComponent<Image>().sprite = isHigh;

                PlayerPrefs.SetString(keyName, "High");
            }
            if (state == 4)
            {
                GetComponent<Image>().sprite = isUltra;

                PlayerPrefs.SetString(keyName, "Ultra");
            }
            if (state == 5)
            {
                GetComponent<Image>().sprite = isMax;

                PlayerPrefs.SetString(keyName, "Max");
            }
        }
    }
}