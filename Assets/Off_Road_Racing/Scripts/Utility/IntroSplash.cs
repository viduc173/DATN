using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroSplash : MonoBehaviour
{

    public float delay = 7f;

    public string levelName = " Garage";

    public GameObject loading;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        Cursor.visible = false;
        
        yield return new WaitForSeconds(delay);

        if (FindFirstObjectByType<FadeMode>())
            FindFirstObjectByType<FadeMode>().Do_Fade();

        loading.SetActive(true);

        yield return new WaitForSeconds(2f);

        SceneManager.LoadSceneAsync(levelName);

    }
}
