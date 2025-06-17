using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 引入 SceneManager 类
public class SceneLoader : MonoBehaviour
{
    // 假设我们要跳转到的场景名字是"Graph" 
    public string nextSceneName = "Graph";
    // 定义一个方法来加载场景
    public void LoadNextScene()
    {
        // 使用 SceneManager 的 LoadScene 方法来加载场景
        // 参数是场景的名字，第二个参数是加载模式
        // LoadSceneMode.Single 表示加载新场景并卸载当前场景
        SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
    }
}