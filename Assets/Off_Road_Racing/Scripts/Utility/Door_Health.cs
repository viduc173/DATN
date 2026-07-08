using ALIyerEdon;
using UnityEngine;

public class Door_Health : MonoBehaviour
{
    [Space(5)]
    public bool isPlayer;

    public float targetSpeed = 50f;

    public GameObject[] frontFlares;

    public GameObject frontDoorsLStatic, frontLDoors;
    public GameObject frontDoorsRStatic, frontRDoors;
    public GameObject backDoorStatic, backDoor;
    public GameObject backDoorStatic2, backDoor2;

    int collisionCount;
    int totalHealth = 10;

    void OnCollisionEnter(Collision col)
    {
        if (isPlayer)
        {
            if (GetComponent<EasyCarController>().currentSpeed > targetSpeed)
            {

                totalHealth = totalHealth - 1;

                if (totalHealth == 7)
                {
                    if (frontDoorsLStatic && frontLDoors)
                    {
                        frontDoorsLStatic.SetActive(false);
                        frontLDoors.SetActive(true);
                    }

                    if (frontDoorsRStatic && frontRDoors)
                    {
                        frontDoorsRStatic.SetActive(false);
                        frontRDoors.SetActive(true);
                    }

                    for (int a = 0; a < frontFlares.Length; a++)
                        frontFlares[a].SetActive(false);
                }

                if (totalHealth == 0)
                {
                    if (backDoorStatic && backDoor)
                    {
                        backDoorStatic.SetActive(false);
                        backDoor.SetActive(true);
                    }
                    if (backDoorStatic2 && backDoor2)
                    {
                        backDoorStatic2.SetActive(false);
                        backDoor2.SetActive(true);
                    }
                }

            }
        }
        else
        {
            totalHealth = totalHealth - 1;

            if (totalHealth == 7)
            {
                if (frontDoorsLStatic && frontLDoors)
                {
                    frontDoorsLStatic.SetActive(false);
                    frontLDoors.SetActive(true);
                }

                if (frontDoorsRStatic && frontRDoors)
                {
                    frontDoorsRStatic.SetActive(false);
                    frontRDoors.SetActive(true);
                }

                for (int a = 0; a < frontFlares.Length; a++)
                    frontFlares[a].SetActive(false);
            }

            if (totalHealth == 0)
            {
                if (backDoorStatic && backDoor)
                {
                    backDoorStatic.SetActive(false);
                    backDoor.SetActive(true);
                }
                if (backDoorStatic2 && backDoor2)
                {
                    backDoorStatic2.SetActive(false);
                    backDoor2.SetActive(true);
                }
            }
        }
    }
}
