using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager2 : MonoBehaviour {

	enum State{
		START,
		PLAY,
		GAMEOVER,
		PAUSE,
	}

	public GameObject CrossHair;
	public static float time;
	public float timeLimit = 30;
	const float waitTime = 5;

	Animator anim;
	MoleManager moleManager;
	Text remainingTIme;
	AudioSource audio;

	State state;
	float timer;

	public bool isStop = false;
	public GameObject menu;
	void Start () 
	{
		Application.targetFrameRate = 30;

		this.state = State.START;
		this.timer = 0;
		this.anim = GameObject.Find ("Canvas").GetComponent<Animator> ();
		this.moleManager = GameObject.Find ("GameManager").GetComponent<MoleManager> ();
		this.remainingTIme = GameObject.Find ("RemainingTime").GetComponent<Text>();
		this.audio = GetComponent<AudioSource> ();
	}
	
	void Update () 
	{
		if (this.state == State.START) 
		{
			isStop = true;
			if (Input.GetMouseButtonDown (0)) 
			{
				this.state = State.PLAY;

				// hide start label
				this.anim.SetTrigger ("StartTrigger");

				// start to generate moles
				this.moleManager.StartGenerate ();

				this.audio.Play ();

				CrossHair.SetActive(true);

			}

			if (Input.GetKeyDown(KeyCode.Escape))
			{
				menu.SetActive(true);
				Time.timeScale = (0);
				CrossHair.SetActive(false);
				this.state = State.PAUSE;
			}
			
			/*
			else
			{
				if (Input.GetKeyDown(KeyCode.Escape))
				{
					menu.SetActive(false);
					isStop = true;
					Time.timeScale = (1);
					CrossHair.SetActive(true);
					this.state = State.START;
				}
			}
			*/
		}

		else if (this.state == State.PLAY) 
		{
			isStop = false;
			this.timer += Time.deltaTime;
			time = this.timer / timeLimit;
				
			if (this.timer > timeLimit) 
			{				
				this.state = State.GAMEOVER;

				// show gameover label
				this.anim.SetTrigger ("GameOverTrigger");

				// stop to generate moles
				this.moleManager.StopGenerate ();

				this.timer = 0;

				// stop audio
				this.audio.loop = false;
			}


			if (Input.GetKeyDown(KeyCode.Escape))
			{
				menu.SetActive(true);
				isStop = false;
				Time.timeScale = (0);
				CrossHair.SetActive(false);
				this.state = State.PAUSE;
				this.audio.Pause();
			}
			

			this.remainingTIme.text = "Time: " + ((int)(timeLimit-timer)).ToString ("D2");
		}
		else if (this.state == State.GAMEOVER) 
		{
			this.timer += Time.deltaTime;

			if (this.timer > waitTime) 
			{
				SceneManager.LoadScene ( SceneManager.GetActiveScene().name );
			}

			this.remainingTIme.text = "";
			}

		else if (this.state == State.PAUSE)
		{
			
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				menu.SetActive(false);
				Time.timeScale = (1);
				if (isStop)
				{
					CrossHair.SetActive(false);
					this.state = State.START;
				}

				else
				{
					CrossHair.SetActive(true);
					this.audio.Play();
					this.state = State.PLAY;
				}
			}
			
		}
	}

	public void Resume()
	{
		menu.SetActive(false);
		Time.timeScale = (1);
		
		if (isStop)
        {
			CrossHair.SetActive(false);
			this.state = State.START;
		}

		else
		{
			CrossHair.SetActive(true);
			this.state = State.PLAY;
			this.audio.Play();
		}
	}
}
