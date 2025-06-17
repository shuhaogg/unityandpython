using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    public GameObject menu;
    public bool isStop = true;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isStop)
        {
           if(Input.GetKeyDown(KeyCode.Escape))
          {
                 menu.SetActive(true);
                 isStop = false ;
                 Time.timeScale = (0);
          }
        }
        else 
        {
            if(Input.GetKeyDown(KeyCode.Escape))
          {
                 menu.SetActive(false);
                 isStop = true ;
                 Time.timeScale = (1);
          }
        }
        
        
    }
    public void Resume()
        {
                 menu.SetActive(false);
                 isStop = true ;
                 Time.timeScale = (1);
        }
    public void Quit()
        {
           Application.Quit();
        }
}
