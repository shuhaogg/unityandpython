using UnityEngine;
using UnityEngine.SceneManagement;
public class ChangeScenes : MonoBehaviour
{

    //需要切换的场景英文名称
    public string sceneName;
    public void GoToNextScene()
    {
        //切换场景的方法
        SceneManager.LoadScene(sceneName);
        Time.timeScale = (1);
        if (sceneName == "Main Menu")
        {
            var cube = GameObject.Find("Player");
            DestroyImmediate(cube);
        }
    }
}
