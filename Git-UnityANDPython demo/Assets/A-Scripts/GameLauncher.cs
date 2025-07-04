using UnityEngine;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;

public class GameLauncher : MonoBehaviour
{
    // ZMQ通信相关
    private RequestSocket clientSocket;
    private bool isRunning = true;

    // ZMQ配置
    private const string RequestAddress = "tcp://localhost:6666";

    void Start()
    {
        // 初始化ZMQ
        InitZMQ();
    }

    void OnDestroy()
    {
        // 关闭ZMQ和线程
        CleanupZMQ();
    }

    void OnApplicationQuit()
    {
        CleanupZMQ();
    }

    // 初始化ZMQ通信
    private void InitZMQ()
    {
        try
        {
            clientSocket = new RequestSocket();
            clientSocket.Connect(RequestAddress);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ZMQ初始化失败: {e.Message}");
        }
    }

    // 清理ZMQ资源
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
            Debug.LogError($"ZMQ清理失败: {e.Message}");
        }
    }

    // 发送命令到Python
    private void SendCommandToPython(string command)
    {
        try
        {
            Debug.Log($"[ZMQ发送] 命令: {command}");
            clientSocket.SendFrame(command);

            // 不需要等待回复，直接返回
        }
        catch (System.Exception e)
        {
            Debug.LogError($"发送命令错误: {e.Message}");
        }
    }

    // 按钮点击事件处理

    // 启动贪吃蛇游戏
    public void LaunchSnakeGame()
    {
        SendCommandToPython("start_game:snake");
    }

    // 启动打地鼠游戏
    public void LaunchMoleGame()
    {
        SendCommandToPython("start_game:mole");
    }
}