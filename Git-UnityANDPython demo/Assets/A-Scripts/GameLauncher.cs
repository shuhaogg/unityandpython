using UnityEngine;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;

public class GameLauncher : MonoBehaviour
{
    // ZMQͨ�����
    private RequestSocket clientSocket;
    private bool isRunning = true;

    // ZMQ����
    private const string RequestAddress = "tcp://localhost:6666";

    void Start()
    {
        // ��ʼ��ZMQ
        InitZMQ();
    }

    void OnDestroy()
    {
        // �ر�ZMQ���߳�
        CleanupZMQ();
    }

    void OnApplicationQuit()
    {
        CleanupZMQ();
    }

    // ��ʼ��ZMQͨ��
    private void InitZMQ()
    {
        try
        {
            clientSocket = new RequestSocket();
            clientSocket.Connect(RequestAddress);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ZMQ��ʼ��ʧ��: {e.Message}");
        }
    }

    // ����ZMQ��Դ
    private void CleanupZMQ()
    {
        try
        {
            isRunning = false;

            if (clientSocket != null)
            {
                clientSocket.Close();
                clientSocket = null;
            }

            NetMQConfig.Cleanup(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ZMQ����ʧ��: {e.Message}");
        }
    }

    // �������Python
    private void SendCommandToPython(string command)
    {
        try
        {
            Debug.Log($"[ZMQ����] ����: {command}");
            clientSocket.SendFrame(command);

            // ����Ҫ�ȴ��ظ���ֱ�ӷ���
        }
        catch (System.Exception e)
        {
            Debug.LogError($"�����������: {e.Message}");
        }
    }

    // ��ť����¼�����

    // ����̰������Ϸ
    public void LaunchSnakeGame()
    {
        SendCommandToPython("start_game:snake");
    }

    // �����������Ϸ
    public void LaunchMoleGame()
    {
        SendCommandToPython("start_game:mole");
    }
}