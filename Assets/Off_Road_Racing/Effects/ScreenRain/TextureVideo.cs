using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TextureVideo : MonoBehaviour 
{
	
	public Texture2D[] frames ;
	public float framesPerSecond = 10f;

	MeshRenderer meshRenderer;

	void Start()
	{
		meshRenderer = GetComponent<MeshRenderer> ();

	}


	void Update () 
	{

		int index  = (int)Mathf.Floor( Time.time * framesPerSecond); 

		index = index % frames.Length;

		meshRenderer.material.mainTexture = frames[index]; 
	
	}
}
