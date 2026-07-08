//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ALIyerEdon;

namespace ALIyerEdon
{
    public class Checkpoint_Trigger : MonoBehaviour
    {
        public string PlayerTag = "Player";
        public string RacerTag = "Racer";

        public int currentCheckpoint = 0;

        void OnTriggerEnter(Collider col)
        {
            if (col.transform.tag == PlayerTag || col.transform.tag == RacerTag)
            {
                // Car_Position có thể nằm ở root của xe, không phải trên collider trực tiếp
                Car_Position carPos = col.GetComponent<Car_Position>()
                    ?? col.GetComponentInParent<Car_Position>();
                if (carPos == null) return;

                carPos.currentCheckpoint = currentCheckpoint;

                var checkpoints = GetComponentInParent<Checkpoint_Manager>().checkpoints;

                // Pass to the next checkpoint
                if (currentCheckpoint < checkpoints.Count - 1)
                    carPos.nextCheckpoint = checkpoints[carPos.currentCheckpoint + 1];

                // Switch to the first checkpoint at the final checkpoint
                if (currentCheckpoint == checkpoints.Count - 1)
                    carPos.nextCheckpoint = checkpoints[0];

                // Only player can pass to the next lap on the center of the race track
                if (currentCheckpoint == checkpoints.Count / 2)
                    carPos.canPassLap = true;

                // Pass to the next Lap
                if (currentCheckpoint == 0)
                {
                    if (carPos.canPassLap)
                        carPos.currentLap++;

                    if (col.transform.tag == PlayerTag)
                    {
                        var raceManager = FindFirstObjectByType<Race_Manager>();
                        if (raceManager != null && carPos.currentLap > raceManager.totalLaps)
                            raceManager.Finish_Race();
                    }

                    carPos.canPassLap = false;
                }
            }
        }
    }
}