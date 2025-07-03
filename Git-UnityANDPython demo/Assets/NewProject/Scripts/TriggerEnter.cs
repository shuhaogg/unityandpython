
using Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// 游戏场景转换触发器
/// </summary>
public class TriggerEnter : MonoBehaviour
{
    public bool sceneScript = false;


    private void OnTriggerStay(Collider collider)
       {
        //Debug.Log("Enter" + collider.gameObject.name);
        //Debug.Log(action);
        string gameobj_name = collider.gameObject.name;


        if (IsSceneScript.isSceneScript)
        {
            if (gameobj_name == "GameObject")
            {
                if (Input.GetKeyDown(KeyCode.F))
                {
                    SceneManager.LoadScene("Resistance");
                }
            }

            if (gameobj_name == "TriggerResistant")
            {
                if (Input.GetKeyDown(KeyCode.F))
                {
                    SceneManager.LoadScene("Resistance");
                }
            }

            if (gameobj_name == "TriggerActive")
            {
                if (Input.GetKeyDown(KeyCode.F))
                {
                    SceneManager.LoadScene("Computer");
                }
            }
        }
           

    }
}


