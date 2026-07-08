using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackCamera_Manager : MonoBehaviour
{
    public float updateDelay = 0.3f;

    public Transform[] CameraViewList;

    Transform target;
    [HideInInspector] public Transform currentCamera;
    [HideInInspector] public int currentID;

    IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        target = GameObject.FindGameObjectWithTag("Player").transform;

        while (true)
        {

            yield return new WaitForSeconds(updateDelay);

            FindTarget();

        }
    }

    void FindTarget()
    {
        for (int i = 0; i < CameraViewList.Length; i++)
        {
            CameraViewList[i].gameObject.SetActive(false);
        }

        currentCamera= GetClosestEnemy(CameraViewList);
    }

    Transform GetClosestEnemy(Transform[] enemies)
    {
        Transform bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = target.position;
        foreach (Transform potentialTarget in enemies)
        {
            Vector3 directionToTarget = potentialTarget.position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if (dSqrToTarget < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToTarget;
                bestTarget = potentialTarget;
            }
        }

        return bestTarget;
    }

    public Color color = Color.blue;
    public float size = 1f;

    void OnDrawGizmos()
    {
        Gizmos.color = color;

        for(int a = 0;a< CameraViewList.Length;a++)
            Gizmos.DrawSphere(CameraViewList[a].position, size);
    }
}
