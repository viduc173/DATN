using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ALIyerEdon
{
    [ExecuteInEditMode]
    public class Billboard : MonoBehaviour
    {
        [HideInInspector] public Transform mainCamera;

        private void Start()
        {
            mainCamera = Camera.main.transform;
        }

        void Update()
        {
            transform.LookAt(mainCamera);
        }
    }
}