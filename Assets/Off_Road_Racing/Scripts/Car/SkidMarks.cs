//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using UnityEngine;
using System.Collections;
namespace ALIyerEdon
{
	public class SkidMarks : MonoBehaviour
	{

		// For detect when wheel collid with ground
		public WheelCollider CorrespondingCollider;

		// Skidmark prefab fro instantiating
		public GameObject skidMarkPrefab;

		// Min and max slip value for instantiating skidmark
		public float onSlip = 0.07f,
		offSlip = 0.01f;

		public AudioSource skidSource;

		// Use this for Log wheel slip value
		public bool debug;

		bool inRoad;

		// Check the car is on the road
		ALIyerEdon.EasyCarController carController;

		void Start()
		{
			carController = GetComponentInParent<EasyCarController>();
		}

		void Update()
		{
			WheelHit hit;

			if (CorrespondingCollider.GetGroundHit(out hit)) Debug.DrawLine(hit.point, CorrespondingCollider.transform.position);
			{
				if (hit.collider.transform.tag == "Road")
					inRoad = true;
				else
					inRoad = false;
			}

			if (!inRoad)
				return;

			/*// now cast a ray out from the wheel collider's center the distance of the suspension, if it hit something, then use the "hit"
			// variable's data to find where the wheel hit, if it didn't, then se tthe wheel to be fully extended along the suspension.
				if ( Physics.Raycast( ColliderCenterPoint, -CorrespondingCollider.transform.up,out hit, CorrespondingCollider.suspensionDistance + CorrespondingCollider.radius ) ) {
						transform.position = hit.point + (CorrespondingCollider.transform.up * CorrespondingCollider.radius);
				}else{
						transform.position = ColliderCenterPoint - (CorrespondingCollider.transform.up * CorrespondingCollider.suspensionDistance);
				}*/

			// define a wheelhit object, this stores all of the data from the wheel collider and will allow us to determine
			// the slip of the tire.
			WheelHit CorrespondingGroundHit;
			CorrespondingCollider.GetGroundHit(out CorrespondingGroundHit);

			// if the slip of the tire is greater than 2.0, and the slip prefab exists, create an instance of it on the ground at
			// a zero rotation.
			if (debug)
				Debug.Log(CorrespondingGroundHit.sidewaysSlip.ToString());

			if (Mathf.Abs(CorrespondingGroundHit.sidewaysSlip) > onSlip)
			{

				if (!skiding)
				{
					if (!skidSource.isPlaying)
						skidSource.Play();

					skid = (GameObject)Instantiate(skidMarkPrefab, transform.position, transform.rotation);
					skid.name = "skid";
					skid.transform.parent = gameObject.transform;

					if (CorrespondingGroundHit.collider.tag == "Road")
					{
						if (roadFX)
						{
							road = (GameObject)Instantiate(roadFX, transform.position, transform.rotation);
							road.transform.parent = gameObject.transform;
						}
					}

					skiding = true;
				}
				else
				{
					if (!skidSource.isPlaying)
						skidSource.Play();

					skid.transform.parent = null;
					skid.transform.position = transform.position;


					if (road)
					{
						road.transform.parent = null;
						road.transform.position = transform.position;

					}
				}
			}
			else if (Mathf.Abs(CorrespondingGroundHit.sidewaysSlip) <= offSlip)
			{
				if (skiding)
				{
					skiding = false;
					if (skidSource.isPlaying)
						skidSource.Stop();
				}

			}
		}
		// Internal usage
		bool skiding;
		GameObject skid, road;

		public GameObject roadFX;
	}
}