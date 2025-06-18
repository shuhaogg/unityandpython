using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using System;

public class PythonCommandSender : MonoBehaviour
{
    // Python 监听的地址和端口
    private string pythonAddress = "tcp://127.0.0.1:6666"; // 本地测试用

    public void SendStartTeaching()
    {
        SendCommandToPython("start_teaching");
    }

    public void SendRecordStart()
    {
        SendCommandToPython("record_start");
    }

    public void SendRecordEnd()
    {
        SendCommandToPython("record_end");
    }

    public void SendZeroPos()
    {
        SendCommandToPython("zero_pos");
    }

    public void EnableMotor()
    {
        SendCommandToPython("enable_motor");
    }

    public void DisableMotor()
    {
        SendCommandToPython("disable_motor");
    }

    private void SendCommandToPython(string command)
    {
        try
        {
            using (var requestSocket = new RequestSocket())
            {
                requestSocket.Connect(pythonAddress);
                requestSocket.SendFrame(command);

                if (requestSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(500), out string response))
                {
                    Debug.Log($"[ZMQ] Python 回复：{response}");
                }
                else
                {
                    Debug.LogWarning("[ZMQ] 无法收到 Python 响应！");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ZMQ] 发送命令失败：{e.Message}");
        }
    }
}
