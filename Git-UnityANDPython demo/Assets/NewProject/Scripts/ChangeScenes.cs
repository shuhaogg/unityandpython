using UnityEngine;
using UnityEngine.SceneManagement;
public class ChangeScenes : MonoBehaviour
{

    //��Ҫ�л��ĳ���Ӣ������
    public string sceneName;
    public void GoToNextScene()
    {
        //�л������ķ���
        SceneManager.LoadScene(sceneName);
        Time.timeScale = (1);
        if (sceneName == "Main Menu")
        {
            var cube = GameObject.Find("Player");
            DestroyImmediate(cube);
        }
    }
}
