using UnityEngine;
using TMPro;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using System;

public class PythonStatusListener : MonoBehaviour
{
    public TextMeshProUGUI motorStatusText;
    public TextMeshProUGUI actionText;
    public TextMeshProUGUI feedbackText;

    private Thread listenerThread;
    private bool running = true;

    void Start()
    {
        listenerThread = new Thread(ListenForStatus);
        listenerThread.IsBackground = true;
        listenerThread.Start();
    }

    void ListenForStatus()
    {
        AsyncIO.ForceDotNet.Force();

        using (var subSocket = new SubscriberSocket())
        {
            subSocket.Connect("tcp://127.0.0.1:5555"); // 和 Python 保持一致
            subSocket.Subscribe(""); // 订阅所有消息

            while (running)
            {
                try
                {
                    string msg = subSocket.ReceiveFrameString();
                    StatusMessage status = JsonUtility.FromJson<StatusMessage>(msg);

                    UnityMainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        if (!string.IsNullOrEmpty(status.motor_status))
                        {
                            motorStatusText.text = $"电机状态：<color=green>{status.motor_status}</color>";
                        }

                        if (!string.IsNullOrEmpty(status.current_action))
                        {
                            actionText.text = $"当前动作：<color=green>{status.current_action}</color>";
                        }

                        if (!string.IsNullOrEmpty(status.feedback))
                        {
                            feedbackText.text = $"操作反馈：<color=#3B78FF>{status.feedback}</color>";
                        }
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ZMQ] 状态解析错误: {e.Message}");
                }
            }
        }
    }

    void OnDestroy()
    {
        running = false;
    }

    [System.Serializable]
    public class StatusMessage
    {
        public string motor_status;
        public string current_action;
        public string feedback;
    }
}
