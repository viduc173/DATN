using ALIyerEdon;
using UnityEngine;

// Attach this script to the racers
public class DifficultyLevel : MonoBehaviour
{
    public float enginePower_Low = 1500f;
    public float enginePower_Medium = 1700f;
    public float enginePower_High = 2000f;

    void Awake()
    {
        Update_DifficultyLevel();
    }

    public void Update_DifficultyLevel()
    {
        GetComponent<EasyCarController>().enginePower = enginePower_Low;

        // if (PlayerPrefs.GetString("DifficultyLevel") == "Low") // Easy
        //     GetComponent<EasyCarController>().enginePower = enginePower_Low;
        // if (PlayerPrefs.GetString("DifficultyLevel") == "Medium") // Medium
        //     GetComponent<EasyCarController>().enginePower = enginePower_Medium;
        // if (PlayerPrefs.GetString("DifficultyLevel") == "High") // Hard
        //     GetComponent<EasyCarController>().enginePower = enginePower_High;
    }
}
