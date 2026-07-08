using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ALIyerEdon
{
    public class IgnoreCollision_Wheels : MonoBehaviour
    {
        public string targetTag = "IgnoreWheelCollision";
        // Start is called before the first frame update
        void Start()
        {
            // Ignore road blocker for racers , enable only for player
            GameObject[] ignores = GameObject.FindGameObjectsWithTag(targetTag);

            for (int a = 0; a < ignores.Length; a++)
            {
                Collider[] col = ignores[a].GetComponentsInChildren<Collider>();

                for (int c = 0; c < col.Length; c++)
                    Physics.IgnoreCollision(col[c], GetComponent<Collider>());
            }
        }
    }
}