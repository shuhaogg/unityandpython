using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoleManager : MonoBehaviour {

	List<MoleController> moles = new List<MoleController>();
	bool generate;
	public AnimationCurve maxMoles;

	// Use this for initialization
	void Start () 
	{
		GameObject[] gos = GameObject.FindGameObjectsWithTag ("Mole");
		foreach (GameObject go in gos) 
		{
			moles.Add (go.GetComponent<MoleController> ());
		}

		this.generate = false;
	}

	public void StartGenerate()
	{
		StartCoroutine ("Generate");
	}

	public void StopGenerate()
	{
		this.generate = false;
	}
		
	IEnumerator Generate()
	{
		this.generate = true;

		while (this.generate) 
		{
			// wait to generate next group
			yield return new WaitForSeconds (1.0f);

			int n = moles.Count;
			int maxNum = (int)this.maxMoles.Evaluate ( GameManager2.time );

			for (int i = 0; i < maxNum; i++) 
			{
				// select mole to up
				this.moles [Random.Range (0, n)].Up ();
								
				yield return new WaitForSeconds (5.0f); // 下一只鼠出现的等待时间
			}
		}
	}
}
