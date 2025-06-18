using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController2 : MonoBehaviour {

	Vector3 defaultPos;
	private const float MAGNITUDE = 0.4f;

	public void Shake()
	{		
		StartCoroutine (_Shake ());
	}

	IEnumerator _Shake()
	{
		for (int i = 0; i <= 360; i += 60) 
		{
			transform.position = 
				new Vector3 (this.defaultPos.x, this.defaultPos.y + MAGNITUDE*Mathf.Sin (i * Mathf.Deg2Rad), this.defaultPos.z);

			yield return null;
		}
	}

	void Start () 
	{
		this.defaultPos = transform.position;
	}
}
