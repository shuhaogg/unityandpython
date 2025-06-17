using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DoorAni : MonoBehaviour
{
	public bool trigger;
	public Animator openandclose;
	public bool open;
	public Transform Player;
	public int dis;

	void Start()
	{
		open = false;
	}

    private void OnTriggerStay(Collider other)
    {
        float dist = Vector3.Distance(other.transform.position, transform.position);
        if (dist < dis)
        {
            if (open == false)
            {
                // Debug.Log(true);
                if (Input.GetKeyDown(KeyCode.F))
                {
                    StartCoroutine(Opening());
                }
            }
            else
            {
                if (open == true)
                {
                    if (Input.GetKeyDown(KeyCode.F))
                    {
                        StartCoroutine(Closing());
                    }
                }

            }
        }
    }


	IEnumerator Opening()
	{
		// print("you are opening the door");
		openandclose.Play("OpenDoor");
		open = true;
		yield return new WaitForSeconds(.5f);
	}

	IEnumerator Closing()
	{
		// print("you are closing the door");
		openandclose.Play("CloseDoor");
		open = false;
		yield return new WaitForSeconds(.5f);
	}


}