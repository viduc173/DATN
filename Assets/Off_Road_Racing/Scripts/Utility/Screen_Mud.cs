using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ALIyerEdon
{
    public class Screen_Mud : MonoBehaviour
    {
        public Image mudImage1, mudImage2, mudImage3;
        Vector3 mud1, mud2, mud3;

        public float randomTime = 1f;
        public float minAlpha;
        public float maxAlpha;

        [HideInInspector] public bool enableMud;

        int random;

        Color mudColor;

        IEnumerator Start()
        {
            mud1 = mudImage1.transform.GetComponent<RectTransform>().localScale;
            mud2 = mudImage2.transform.GetComponent<RectTransform>().localScale;
            mud3 = mudImage3.transform.GetComponent<RectTransform>().localScale;

            mudColor = mudImage1.color;
           
            mudImage1.color =
                    new Color(mudColor.r, mudColor.g, mudColor.b,
                    Mathf.Lerp(mudImage1.color.a, minAlpha, Time.deltaTime * 100));

            while (true)
            {
                yield return new WaitForSeconds(randomTime);

                if (random < 2)
                    random += 1;
                else
                    random = 0;
            }
        }

        void Update()
        {
            mudImage1.transform.GetComponent<RectTransform>().localScale =
               Vector3.Lerp(mudImage1.transform.GetComponent<RectTransform>().localScale, mud1, Time.deltaTime * 1);
            mudImage2.transform.GetComponent<RectTransform>().localScale =
                  Vector3.Lerp(mudImage2.transform.GetComponent<RectTransform>().localScale, mud2, Time.deltaTime * 1);
            mudImage3.transform.GetComponent<RectTransform>().localScale =
                Vector3.Lerp(mudImage3.transform.GetComponent<RectTransform>().localScale, mud3, Time.deltaTime * 1);

            mudImage1.color =
                    new Color(mudColor.r, mudColor.g, mudColor.b,
                    Mathf.Lerp(mudImage1.color.a, minAlpha, Time.deltaTime));
            
            mudImage2.color =
                    new Color(mudColor.r, mudColor.g, mudColor.b,
                    Mathf.Lerp(mudImage2.color.a, minAlpha, Time.deltaTime));

            mudImage3.color =
                    new Color(mudColor.r, mudColor.g, mudColor.b,
                    Mathf.Lerp(mudImage3.color.a, minAlpha, Time.deltaTime));

        }

        public void ApplyMud()
        {
            if (random == 0)
            {
                mudImage1.color =
                        new Color(mudColor.r, mudColor.g, mudColor.b,
                        Mathf.Lerp(mudImage1.color.a, maxAlpha, Time.deltaTime * 15));
                mudImage1.transform.GetComponent<RectTransform>().localScale =
                    Vector3.Lerp(mudImage1.transform.GetComponent<RectTransform>().localScale, mud1 * 1.5f, Time.deltaTime * 15f);
            }
            if (random == 1)
            {
                mudImage2.color =
                        new Color(mudColor.r, mudColor.g, mudColor.b,
                        Mathf.Lerp(mudImage2.color.a, maxAlpha, Time.deltaTime * 15));
                mudImage2.transform.GetComponent<RectTransform>().localScale =
                          Vector3.Lerp(mudImage2.transform.GetComponent<RectTransform>().localScale, mud2 * 1.5f, Time.deltaTime * 15f);
            }
            if (random == 2)
            {
                mudImage3.color =
                        new Color(mudColor.r, mudColor.g, mudColor.b,
                        Mathf.Lerp(mudImage3.color.a, maxAlpha, Time.deltaTime * 15));
                mudImage3.transform.GetComponent<RectTransform>().localScale =
                         Vector3.Lerp(mudImage3.transform.GetComponent<RectTransform>().localScale, mud3 * 1.5f, Time.deltaTime * 15f);
            }
        }
    }
}