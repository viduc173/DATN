using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ALIyerEdon
{
    public class CustomCameraView : MonoBehaviour
    {
        public float distance;
        public float height;
        public float heightOffset;

        void Start()
        {
            if (FindFirstObjectByType<SmoothFollow>())
            {
                FindFirstObjectByType<SmoothFollow>().distance = distance;
                FindFirstObjectByType<SmoothFollow>().height = height;
                FindFirstObjectByType<SmoothFollow>().offset = 
                    new Vector3(0, heightOffset, 0);
            }
            if (FindFirstObjectByType<SmoothFollow2>())
            {
                FindFirstObjectByType<SmoothFollow2>().distance = distance;
                FindFirstObjectByType<SmoothFollow2>().height = height;
                FindFirstObjectByType<SmoothFollow2>().Angle = heightOffset;
            }
        }
    }
}