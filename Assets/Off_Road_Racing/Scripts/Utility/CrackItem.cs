using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CrackItem : MonoBehaviour
{
    public Rigidbody[] rigids;
    public bool destroy;
    public bool disableRigidbody;
    public float destroyDelay = 5f;
    public float targetSpeed = 50f;
    public string targetTag = "Car";
    public AudioSource crashSound;

    void Start()
    {
        if (GetComponent<AudioSource>())
            crashSound = GetComponent<AudioSource>();

        rigids = GetComponentsInChildren<Rigidbody>();
    }

    // Update is called once per frame
    void OnTriggerEnter(Collider col)
    {
        if (col.transform.tag == targetTag)
        {
            
                for (int a = 0; a < rigids.Length; a++)
                {
                    if (rigids[a])
                        rigids[a].isKinematic = false;
                }
                StartCoroutine(Disable_Delay());
            
        }
    }

   /* void OnTriggerStay(Collider col)
    {
        if (col.transform.tag == "Player" || col.transform.tag == "Racer")
        {
            if (col.GetComponent<ALIyerEdon.EasyCarController>().currentSpeed >= targetSpeed)
            {
                if (rigids.Length > 0)
                {
                    for (int a = 0; a < rigids.Length; a++)
                    {
                        if (rigids[a].isKinematic == true)
                            rigids[a].isKinematic = false;
                    }
                }
            }
        }
    }*/


    IEnumerator Disable_Delay()
    {
        if (crashSound)
        {
            if (!crashSound.isPlaying)
                crashSound.Play();
        }

        yield return new WaitForSeconds(destroyDelay);

        for (int a = 0; a < rigids.Length; a++)
        {
            if (destroy)
                Destroy(gameObject);
            else
            {
                if (disableRigidbody)
                {
                    if (rigids[a])
                    {
                        // rigids[a].isKinematic = true;
                         Destroy(rigids[a].gameObject.GetComponent<BoxCollider>());
                         Destroy(rigids[a]);
                    }
                }
            }
        }
    }
}
