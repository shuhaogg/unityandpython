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
            subSocket.Connect("tcp://127.0.0.1:5555"); // �� Python ����һ��
            subSocket.Subscribe(""); // ����������Ϣ

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
                            motorStatusText.text = $"���״̬��<color=green>{status.motor_status}</color>";
                        }

                        if (!string.IsNullOrEmpty(status.current_action))
                        {
                            actionText.text = $"��ǰ������<color=green>{status.current_action}</color>";
                        }

                        if (!string.IsNullOrEmpty(status.feedback))
                        {
                            feedbackText.text = $"����������<color=#3B78FF>{status.feedback}</color>";
                        }
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ZMQ] ״̬��������: {e.Message}");
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
