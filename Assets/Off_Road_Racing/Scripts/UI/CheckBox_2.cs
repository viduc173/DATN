using UnityEngine;
using UnityEngine.UI;

namespace ALIyerEdon
{
    public class CheckBox_2 : MonoBehaviour
    {
        public int state;
        public Sprite isLow, isMedium,isHigh;
        public string keyName = "";

        void Start()
        {
            if (PlayerPrefs.GetString(keyName) == "Low")
            {
                GetComponent<Image>().sprite = isLow;
                state = 0;
            }
            if (PlayerPrefs.GetString(keyName) == "Medium")
            {
                GetComponent<Image>().sprite = isMedium;
                state = 1;
            }
            if (PlayerPrefs.GetString(keyName) == "High")
            {
                GetComponent<Image>().sprite = isHigh;
                state = 2;
            }
        }

        public void Update_State()
        {
            if (state < 2)
                state += 1;
            else
                state = 0;

            if (state == 0)
            {
                GetComponent<Image>().sprite = isLow;

                PlayerPrefs.SetString(keyName, "Low");
            }
            if (state == 1)
            {
                GetComponent<Image>().sprite = isMedium;

                PlayerPrefs.SetString(keyName, "Medium");
            }
            if (state == 2)
            {
                GetComponent<Image>().sprite = isHigh;

                PlayerPrefs.SetString(keyName, "High");
            }
        }
    }
}