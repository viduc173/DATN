using UnityEngine;
using UnityEngine.UI;

namespace ALIyerEdon
{
    public class CheckBox : MonoBehaviour
    {
        public bool state;
        public Sprite isOn, isOff;
        public string keyName = "";

        void Start()
        {
            if (PlayerPrefs.GetString(keyName) == "On")
            {
                GetComponent<Image>().sprite = isOn;
                state = true;
            }
            else
            {
                GetComponent<Image>().sprite = isOff;
                state = false;
            }
        }

        public void Update_State()
        {
            state = !state;

            if(state)
            {
                GetComponent<Image>().sprite = isOn;

                PlayerPrefs.SetString(keyName, "On");
            }
            else
            {
                GetComponent<Image>().sprite = isOff;

                PlayerPrefs.SetString(keyName, "Off");
            }
        }
    }
}