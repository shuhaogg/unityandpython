using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{

	///<summary>
	///
	///</summary>
	public class IsSceneScript : MonoBehaviour
	{
        public static bool isSceneScript { get; private set; }

        private void Awake()
        {
            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        }

        private void SceneManager_activeSceneChanged(Scene currentScene, Scene nextScene)
        {
            if (nextScene.name == "MainScene")
            {
                isSceneScript = true;
            }
            else
            {
                isSceneScript = false;
            }
        }


    }
}