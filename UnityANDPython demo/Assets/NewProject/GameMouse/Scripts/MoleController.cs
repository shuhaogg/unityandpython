using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoleController : MonoBehaviour {

	public float moveSpeed = 0.1f;
	public float waitTime = 1.0f;

	private const float TOP = -0.1f;
	private const float BOTTOM = -1.65f;
	private float tmpTime = 0;

	enum State{
		UNDER_GROUND,
		UP,
		ON_GROUND,
		DOWN,
		HIT,
	}
	State state;


	public void Up()
	{
		if (this.state == State.UNDER_GROUND) 
		{
			this.state = State.UP;
		}
	}

	public bool Hit()
	{
		// if mole is under ground, never hit
		if (this.state == State.UNDER_GROUND) 
		{
			return false;
		}

		// hit to bottom
		transform.position = 
			new Vector3(transform.position.x, BOTTOM, transform.position.z);

		this.state = State.UNDER_GROUND;

		return true;
	}
		
	void Start () 
	{
		this.state = State.UNDER_GROUND;
	}
		
	void Update () 
	{
		if(Time.timeScale == (1))
        {
			if (this.state == State.UP)
			{
				transform.Translate(0, this.moveSpeed, 0);

				if (transform.position.y > TOP)
				{
					transform.position =
						new Vector3(transform.position.x, TOP, transform.position.z);

					this.state = State.ON_GROUND;

					this.tmpTime = 0;
				}
			}
			else if (this.state == State.ON_GROUND)
			{
				this.tmpTime += Time.deltaTime;

				if (this.tmpTime > this.waitTime)
				{
					this.state = State.DOWN;
				}
			}
			else if (this.state == State.DOWN)
			{
				transform.Translate(0, -this.moveSpeed, 0);

				if (transform.position.y < BOTTOM)
				{
					transform.position =
						new Vector3(transform.position.x, BOTTOM, transform.position.z);

					this.state = State.UNDER_GROUND;
				}
			}
		}

	}
}
