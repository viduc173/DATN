//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________
// Based on the unity car tutorial

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ALIyerEdon
{
	public class WheelSkidmarks : MonoBehaviour
	{

		[HideInInspector] public int wheelID = 0;

		public float startSlipValue = 0.4f;

		//To hold the skidmarks object
		Skidmarks_Manager skidmarksMananger = null;

		//To hold last skidmarks data
		int lastSkidmark = -1;

		//To hold self wheel collider
		WheelCollider wheel_col;

		// Road detection
		bool inRoad = false;

		//The parent oject having a rigidbody attached to it.
		GameObject carRigid;
		ALIyerEdon.EasyCarAudio carAudio;

		void Start()
		{
			//Get the Wheel Collider attached to self
			carRigid = transform.root.gameObject;

			carAudio = transform.root
				.GetComponent<ALIyerEdon.EasyCarAudio>();

			wheel_col = GetComponent<WheelCollider>();

			// Find object "Skidmarks" from the game
			if (FindFirstObjectByType<Skidmarks_Manager>())
			{
				skidmarksMananger = FindFirstObjectByType<Skidmarks_Manager>();
			}
		}

		// This has to be in fixed update or it wont get time to make skidmesh fully.
		void FixedUpdate()
		{
			if (!skidmarksMananger)
				return;

			WheelHit GroundHit;

			wheel_col.GetGroundHit(out GroundHit);

			if (GroundHit.collider != null)
			{
				if (GroundHit.collider.transform.tag == "Road")
					inRoad = true;
				else
					inRoad = false;
			}
			else
			{
				inRoad = false;
			}

			if(carAudio.checkWheel.Length != 0)
				carAudio.checkWheel[wheelID] = inRoad;

			var wheelSlipAmount = Mathf.Abs(GroundHit.sidewaysSlip);

			if (wheelSlipAmount > startSlipValue) //if sideways slip is more than desired value
			{
				/*Calculate skid point:
				Since the body moves very fast, the skidmarks would appear away from the wheels because by the time the
				skidmarks are made the body would have moved forward. So we multiply the rigidbody's velocity vector x 2 
				to get the correct position
				*/

				Vector3 skidPoint = GroundHit.point + 2 * (carRigid.GetComponent<Rigidbody>().linearVelocity) * Time.deltaTime;

				//Add skidmark at the point using AddSkidMark function of the Skidmarks object
				//Syntax: AddSkidMark(Point, Normal, Intensity(max value 1), Last Skidmark index);

				lastSkidmark = skidmarksMananger.AddSkidMark(skidPoint, GroundHit.normal, wheelSlipAmount / 2.0f, lastSkidmark);

				carAudio.Check_InRoad();

				Play_Skid_Sound();
			}
			else
			{
				// Stop making skidmarks
				lastSkidmark = -1;

				carAudio.Check_InRoad();

				Stop_Skid_Sound();
			}
		}

		public void Play_Skid_Sound()
		{
			if (!carAudio.skidSource.isPlaying)
				carAudio.skidSource.Play();
		}
		public void Stop_Skid_Sound()
		{
			if (carAudio.skidSource.isPlaying)
				carAudio.skidSource.Stop();
		}
	}
}