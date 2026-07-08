using UnityEngine;
using UnityEngine.UI;

public class FadeMode : MonoBehaviour
{
    public bool startFade;
    public Image targetImage;
    public float fadeSpeed = 1f;

    void Start()
    {
        if(startFade)
        {
            targetImage.color = Color.black;
        }
    }

    void Update()
    {
        if (startFade)
        {           
            targetImage.color =
                        new Color(targetImage.color.r, targetImage.color.g, targetImage.color.b,
                        Mathf.Lerp(targetImage.color.a, 0, Time.deltaTime * fadeSpeed));
        }
    }

    public void Do_Fade()
    {
        startFade = true;

        targetImage.color = Color.black;
    }
}