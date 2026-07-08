//______________________________________________
// ALIyerEdon
// https://assetstore.unity.com/publishers/23606
//______________________________________________

using UnityEngine;
using System.Collections;
using ALIyerEdon;

namespace ALIyerEdon
{
	public class ColorLoader : MonoBehaviour
	{

		// List of the car colors
		public Color[] Colors;

		// Car material for changing color
		public Material mat;

		// Car ID for read car color
		public string CarID;

		void Start()
		{

			// Load last selected color by ID
			if (PlayerPrefs.GetInt("TruckColor" + CarID) == 0)
				mat.color = Colors[0];
			if (PlayerPrefs.GetInt("TruckColor" + CarID) == 1)
				mat.color = Colors[1];
			if (PlayerPrefs.GetInt("TruckColor" + CarID) == 2)
				mat.color = Colors[2];
			if (PlayerPrefs.GetInt("TruckColor" + CarID) == 3)
				mat.color = Colors[3];
			if (PlayerPrefs.GetInt("TruckColor" + CarID) == 4)
				mat.color = Colors[4];
			if (PlayerPrefs.GetInt("TruckColor" + CarID) == 5)
				mat.color = Colors[5];
			if (PlayerPrefs.GetInt("TruckColor" + CarID) == 6)
				mat.color = Colors[6];
			if (PlayerPrefs.GetInt("TruckColor" + CarID) == 7)
				mat.color = Colors[7];

		}
	}
}