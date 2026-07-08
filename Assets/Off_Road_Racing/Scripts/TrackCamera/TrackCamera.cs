using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
namespace ALIyerEdon
{
    public class TrackCamera : MonoBehaviour
    {
        
        GameObject target;

        IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();

            target = GameObject.FindGameObjectWithTag("Player");

        }

        // Update is called once per frame
        void Update()
        {
            if (target)
                transform.LookAt(target.transform.position);
        }
    }
}