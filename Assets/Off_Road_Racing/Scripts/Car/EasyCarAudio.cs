//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using System;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections;
using ALIyerEdon;

namespace ALIyerEdon
{
	[RequireComponent(typeof(EasyCarController))]
	public class EasyCarAudio : MonoBehaviour
	{
		[Header("Audio Sources ____________________________________________________" +
			"____________________________________________________")]
		[Space(5)]
		// Audio Sources
		public AudioSource engineSource;
		public AudioSource collisionSource;
		public AudioSource gearSource;
		public AudioSource startSkidSource;
		public AudioSource skidSource;
		public AudioSource flameSource;

		[Header("Audio Clips ____________________________________________________" +
			"____________________________________________________")]
		[Space(5)]
		// Audio Clips
		public AudioClip engineSound;
		public AudioClip engineSoundOFF;
		public AudioClip gearShift;
		public AudioClip collisionClip;
		public AudioClip startSkidClip;
		public AudioClip skidClip;
		public AudioClip flameClip;

		[Header("Volume ____________________________________________________" +
			"____________________________________________________")]
		[Space(5)]
		public float engineVolume = 1f;
		public float gearVolume = 1f;
		public float collisionVolume = 1f;
		public float startSkidVolume = 1f;
		public float skidVolume = 1f;
		public float flameVolume = 1f;

		[Header("Pitch ____________________________________________________" +
			"____________________________________________________")]
		[Space(5)]
		public float pitchMultiplier = 1f;
		public float revMultiplier = 1f;

		public float PitchMin = 0.43f;

		public float PitchMax = 1.2f;

		public float PitchGearChanging = 0.8f;

		[Header("Settings ____________________________________________________" +
			"____________________________________________________")]
		[Space(5)]
		public float collisionVelocity = 5f;
		public float collisioBackForce = 300f;
		public float startSkidDuration = 2.3f;
		[Header("Effects____________________________________________________" +
			"____________________________________________________")]
		[Space(5)]
		public GameObject[] wheelSmokes;
		public GameObject[] exhaustFlame;
		public GameObject collisionSpark;
		public GameObject rainParticle;

		EasyCarController m_vehicleController;
		[HideInInspector] public bool raceIsStarted;
		[HideInInspector] public Shake_Utility shakeUtility;
		[HideInInspector] public float engineStartVolume;
		[HideInInspector] public bool releaseThrottle;

        float default_engineVolume = 0;
		float default_gearVolume = 0;
		float default_collisionVolume = 0;
		float default_startSkidVolume = 0;
		float default_skidVolume = 0;
		float default_flameVolume = 0;

		public void Update_VolumeSettings()
		{
            if (PlayerPrefs.GetString("CarSFX") == "Low")
            {
                engineVolume = default_engineVolume / 3;
				gearVolume = default_gearVolume / 3;
                collisionVolume = default_collisionVolume / 3;
                startSkidVolume = default_startSkidVolume / 3;
                skidVolume = default_skidVolume / 3;
                flameVolume = default_flameVolume / 3;
            }
            if (PlayerPrefs.GetString("CarSFX") == "Medium")
            {
                engineVolume = default_engineVolume / 2;
                gearVolume = default_gearVolume / 2;
                collisionVolume = default_collisionVolume / 2;
                startSkidVolume = default_startSkidVolume / 2;
                skidVolume = default_skidVolume / 2;
                flameVolume = default_flameVolume / 2;
            }
            if (PlayerPrefs.GetString("CarSFX") == "High")
            {
                engineVolume = default_engineVolume;
                gearVolume = default_gearVolume;
                collisionVolume = default_collisionVolume;
                startSkidVolume = default_startSkidVolume;
                skidVolume = default_skidVolume;
                flameVolume = default_flameVolume;
            }
        }

		void Start()
        {
             default_engineVolume = engineVolume;
             default_gearVolume = gearVolume;
             default_collisionVolume = collisionVolume;
             default_startSkidVolume = startSkidVolume;
             default_skidVolume = skidVolume;
             default_flameVolume = flameVolume;

            Update_VolumeSettings();

            Stop_Effects();

			m_vehicleController = GetComponent<EasyCarController>();

			shakeUtility = FindFirstObjectByType<Shake_Utility>();

			checkWheel = new bool[m_vehicleController.Wheel_Colliders.Length];

			engineStartVolume = engineVolume;

			engineVolume = 0;

			gearSource.loop = false;
			gearSource.playOnAwake = false;
			gearSource.clip = gearShift;

			engineSource.clip = engineSound;
			engineSource.loop = true;
			engineSource.volume = engineVolume;
			engineSource.Play();

			collisionSource.loop = false;
			collisionSource.playOnAwake = false;
			collisionSource.clip = collisionClip;
			collisionSource.volume = collisionVolume;

			if (startSkidSource)
			{
				startSkidSource.loop = false;
				startSkidSource.playOnAwake = false;
				startSkidSource.clip = startSkidClip;
				startSkidSource.volume = startSkidVolume;
			}
			if (skidSource)
			{
				skidSource.loop = true;
				skidSource.playOnAwake = false;
				skidSource.clip = skidClip;
				skidSource.volume = skidVolume;
			}
			if (flameSource)
			{
				flameSource.loop = false;
				flameSource.playOnAwake = false;
				flameSource.clip = flameClip;
				flameSource.volume = flameVolume;
			}
		}
		
        void Update()
		{
            // The pitch is interpolated between the min and max values, according to the vehicle's revs.
            float pitch = ULerp(PitchMin, PitchMax, m_vehicleController.Revs * revMultiplier);

            // clamp to minimum pitch (note, not clamped to max for high revs while burning out)
            pitch = Mathf.Min(PitchMax, pitch);

			if (!releaseThrottle)
			{
				engineSource.pitch =
					Mathf.Lerp(engineSource.pitch, pitch * pitchMultiplier,
					Time.deltaTime * 5);
			}
			else
			{
				if (m_vehicleController.currentSpeed > 5f)
				{
					engineSource.pitch =
						Mathf.Lerp(engineSource.pitch, PitchGearChanging,
						Time.deltaTime * 1f);
				}
				else
				{
                    engineSource.pitch =
                        Mathf.Lerp(engineSource.pitch, PitchMin,
                        Time.deltaTime * 1f);
                }
			}
		}


		private static float ULerp(float from, float to, float value)
		{
			return (1.0f - value) * from + value * to;
		}


		public void Stop_Effects()
		{
			for (int a = 0; a < wheelSmokes.Length; a++)
			{
				var emi = wheelSmokes[a].GetComponent<ParticleSystem>().emission;
				emi.enabled = false;
			}
		}
		public void Play_StartSkid_Sound()
		{
			StartCoroutine(StartSkid());
		}

		IEnumerator StartSkid()
		{
			startSkidSource.Play();

			for (int a = 0; a < 2; a++)
			{
				var emi = wheelSmokes[a].GetComponent<ParticleSystem>().emission;
				emi.enabled = true;
			}

			// Reduce mass of the car at start skidding
			float mass = 0;
			mass = GetComponent<Rigidbody>().mass;
			GetComponent<Rigidbody>().mass = mass / 2;

			yield return new WaitForSeconds(startSkidDuration);

			GetComponent<Rigidbody>().mass = mass;

			if (startSkidSource.isPlaying)
				startSkidSource.Stop();

			for (int a = 0; a < 2; a++)
			{
				var emi = wheelSmokes[a].GetComponent<ParticleSystem>().emission;
				emi.enabled = false;
			}

			raceIsStarted = true;
		}

		public void Play_ChangeGear_Sound()
		{
			gearSource.PlayOneShot(gearShift);
		}

		// Flame
		[HideInInspector] public bool isFlamePlaying;
		[HideInInspector] public bool stopRandom;

		public void Play_Flame_Sound()
		{
			flameSource.PlayOneShot(flameClip);
			for (int a = 0; a < exhaustFlame.Length; a++)
			{
				exhaustFlame[a].GetComponent<ParticleSystem>().Play();
			}
		}
		public void Play_RandomFlame_Sound()
		{
			if (!isFlamePlaying)
				StartCoroutine(RandomFlame());
		}
		public void Stop_RandomFlame_Sound()
		{
			StopCoroutine(RandomFlame());
		}
		IEnumerator RandomFlame()
		{
			isFlamePlaying = true;

			while (!stopRandom)
			{
				yield return new WaitForSeconds(Random.Range(0.3f, 1));

				flameSource.PlayOneShot(flameClip);

				for (int a = 0; a < exhaustFlame.Length; a++)
				{
					exhaustFlame[a].GetComponent<ParticleSystem>().Play();
				}

			}

			isFlamePlaying = false;
		}

		// Wheel skiddmark sound manager

		[HideInInspector] public bool inRoadCheck;

		[HideInInspector] public bool[] checkWheel;

		public void Check_InRoad()
		{
			for (int a = 0; a < checkWheel.Length; a++)
			{
				if (checkWheel[a] == true)
					inRoadCheck = true;
				else
					inRoadCheck = false;
			}
		}
		bool isBouncing;
		float bounce;

		void StopBounce()
		{
			isBouncing = false;
		}

		void OnCollisionEnter(Collision collision)
		{
			if (m_vehicleController.isPlayer)
			{
				if (!collision.transform.GetComponent<Rigidbody>())
				{
					bounce = collisioBackForce * m_vehicleController.currentSpeed; //amount of force to apply
					Vector3 direction = (transform.position - collision.contacts[0].point).normalized;
					GetComponent<Rigidbody>().AddForce(direction * bounce, ForceMode.Impulse);
					GetComponent<Rigidbody>().AddForce(-Vector3.up * (bounce / 2), ForceMode.Impulse);
					isBouncing = true;
					
					if(m_vehicleController.currentSpeed > 20f)
						shakeUtility.Shake_Now(0.5f, shakeUtility.shakeIntensity);

					Invoke("StopBounce", 0.3f);
				}
			}

			if (collision.relativeVelocity.magnitude > collisionVelocity)
			{
				if (m_vehicleController.isPlayer)
				{
					if (shakeUtility)
					{
						shakeUtility.collisionShaking = true;
						shakeUtility.shakeIntensity = (m_vehicleController.currentSpeed / 5)
							 * m_vehicleController.collisionShakeIntensity;
					}
				}

				collisionSource.gameObject.transform.position =
				collision.GetContact(0).point;

				if (collisionSpark && collision.relativeVelocity.magnitude > collisionVelocity + 3)
				{
					Quaternion rot = Quaternion.FromToRotation(Vector3.forward, -transform.forward);
					
					GameObject colSpark = Instantiate(collisionSpark, collision.GetContact(0).point, rot);
					
					GameObject.Destroy(colSpark, 5f);

                }

				if (!collisionSource.isPlaying)
				{
					collisionSource.PlayOneShot(collisionClip);
					if (m_vehicleController.isPlayer)
						FindFirstObjectByType<Screen_Mud>().ApplyMud();
				}
			}
		}
		void OnCollisionExit(Collision collision)
		{
			if (collisionSpark)
			{
				if (m_vehicleController.isPlayer)
				{
					if (shakeUtility)
						shakeUtility.collisionShaking = false;
				}
			}
		}
		void OnCollisionStay(Collision collision)
        {
			if (collisionSpark && collision.relativeVelocity.magnitude < collisionVelocity + 3)
            {
				if (m_vehicleController.isPlayer)
				{
					if (shakeUtility)
						shakeUtility.collisionShaking = false;
				}
			}
		}
	}
}