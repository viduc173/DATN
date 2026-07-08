using UnityEngine;
using System.Collections;
namespace ALIyerEdon
{
	public class RainLights : MonoBehaviour
	{

		// Use this for initialization
		public float minTime = 5f;

		public float maxTime = 20f;
		
		float delayTime;

		public Light rLight;
		public MeshRenderer ThunderMat;

		public float maxLightIntensity = 1f;
		public float maxThunderIntensity = 5f;

		IEnumerator Start()
		{
			yield return new WaitForEndOfFrame();
			if (GameObject.FindGameObjectWithTag("Player").
				GetComponent<EasyCarController>().rainParticle)
			{
				GameObject.FindGameObjectWithTag("Player").
				GetComponent<EasyCarController>().rainParticle.SetActive(true);
			}

			yield return new WaitForEndOfFrame();

			foreach (EasyCarController car in FindObjectsOfType<EasyCarController>())
				car.enableWheelEffects = false;

			ThunderMat.sharedMaterial.SetColor
					("_EmissionColor", Color.white * 0);

			while (true)
			{
				delayTime = Random.Range(minTime, maxTime);

				yield return new WaitForSeconds(delayTime);

				rLight.intensity = maxLightIntensity;

;				ThunderMat.sharedMaterial.SetColor
					("_EmissionColor", Color.white * maxThunderIntensity);

				yield return new WaitForSeconds(
					Random.Range(Time.timeScale * 0.1f, Time.timeScale * 0.5f));

				rLight.intensity = 0;

				ThunderMat.sharedMaterial.SetColor
					("_EmissionColor", Color.white * 0);

				yield return new WaitForSeconds(Time.timeScale * 0.1f);

				rLight.intensity = maxLightIntensity;
				ThunderMat.sharedMaterial.SetColor
					("_EmissionColor", Color.white * maxThunderIntensity);


				yield return new WaitForSeconds(Time.timeScale * 0.1f);

				rLight.intensity = 0;
				ThunderMat.sharedMaterial.SetColor
					("_EmissionColor", Color.white * 0);

			}
		}

	}
}