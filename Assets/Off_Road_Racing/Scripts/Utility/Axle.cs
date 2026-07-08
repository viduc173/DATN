using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ALIyerEdon
{
    public class Axle : MonoBehaviour
    {

        public Transform targetWheel;
       
        // Update is called once per frame
        void Update()
        {
            transform.position = new Vector3(transform.position.x,
                targetWheel.position.y, transform.position.z);
        }
    }
}