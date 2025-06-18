using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // ���� SceneManager ��
public class SceneLoader : MonoBehaviour
{
    // ��������Ҫ��ת���ĳ���������"Graph" 
    public string nextSceneName = "Graph";
    // ����һ�����������س���
    public void LoadNextScene()
    {
        // ʹ�� SceneManager �� LoadScene ���������س���
        // �����ǳ��������֣��ڶ��������Ǽ���ģʽ
        // LoadSceneMode.Single ��ʾ�����³�����ж�ص�ǰ����
        SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
    }
}