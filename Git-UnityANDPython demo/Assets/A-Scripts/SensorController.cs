using UnityEngine;
using TMPro;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;

public class SensorController : MonoBehaviour
{
    // UI�������
    public TMP_Text emgStatusText;
    public TMP_Text imuStatusText;
    public TMP_Text pressureStatusText;
    public TMP_Text connectionStatusText;

    // ������ѡ��״̬
    private bool isEMGSelected = false;
    private bool isIMUSelected = false;
    private bool isPressureSelected = false;

    // ZMQͨ�����
    private RequestSocket clientSocket;
    private SubscriberSocket subSocket;
    private Thread receiveThread;
    private bool isRunning = true;

    // ZMQ����
    private const string RequestAddress = "tcp://localhost:6666";
    private const string SubAddress = "tcp://localhost:5555";

    void Start()
    {
        UpdateStatusText(emgStatusText, "δ����", Color.red);
        UpdateStatusText(imuStatusText, "δ����", Color.red);
        UpdateStatusText(pressureStatusText, "δ����", Color.red);
        UpdateStatusText(connectionStatusText, "�����豸", Color.white);

        InitZMQ();
    }

    void OnDestroy()
    {
        // ȷ���ڳ�������ʱֹͣ�����̺߳�������Դ
        StopAllCoroutines();
        CleanupZMQ();
    }

    void OnApplicationQuit()
    {
        // ��Ӧ�ó����˳�ʱִ������
        CleanupZMQ();
    }

    // ��ʼ��ZMQͨ��
    private void InitZMQ()
    {
        try
        {
            // ��ʼ��NetMQ����
            clientSocket = new RequestSocket();
            clientSocket.Connect(RequestAddress);

            subSocket = new SubscriberSocket();
            subSocket.Connect(SubAddress);
            subSocket.SubscribeToAnyTopic();

            receiveThread = new Thread(ReceiveStatusUpdates);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ZMQ��ʼ��ʧ��: {e.Message}");
            UpdateStatusText(connectionStatusText, "ͨ�ų�ʼ��ʧ��", Color.red);
        }
    }

    // ����ZMQ��Դ
    private void CleanupZMQ()
    {
        try
        {
            // ֹͣ�����߳�
            isRunning = false;

            // �ر��׽���
            if (clientSocket != null)
            {
                clientSocket.Close();
                clientSocket = null;
            }

            if (subSocket != null)
            {
                subSocket.Close();
                subSocket = null;
            }

            // �ȴ��߳̽��������ó�ʱ�������õȴ���
            if (receiveThread != null && receiveThread.IsAlive)
            {
                if (!receiveThread.Join(2000))  // �ȴ�2��
                {
                    Debug.LogWarning("�����߳�δ�ܼ�ʱ�˳���ǿ����ֹ");
                    receiveThread.Abort();
                }
                receiveThread = null;
            }

            // ����NetMQ��Դ
            NetMQConfig.Cleanup(false);
            Debug.Log("ZMQ��Դ������");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ZMQ����ʧ��: {e.Message}");
        }
    }

    // ����״̬���£��Ľ��쳣����
    private void ReceiveStatusUpdates()
    {
        try
        {
            while (isRunning)
            {
                string topic, message;
                if (subSocket.TryReceiveFrameString(out topic) &&
                    subSocket.TryReceiveFrameString(out message))
                {
                    Debug.Log($"[ZMQ����] ����: {topic}, ��Ϣ: {message}");
                    ProcessSensorStatus(message);
                }
                else
                {
                    // �������߱���CPUռ�ù���
                    Thread.Sleep(100);
                }
            }
        }
        catch (ThreadAbortException)
        {
            // �̱߳���ֹ�������˳�
            Debug.Log("�����߳�����ֹ");
        }
        catch (System.Exception e)
        {
            if (isRunning)  // ֻ������ʱ��¼����
                Debug.LogError($"�����߳��쳣: {e.Message}");
        }
        finally
        {
            Debug.Log("�����߳����˳�");
        }
    }

    // ��������״̬��Ϣ
    private void ProcessSensorStatus(string message)
    {
        // ��Ϣ��ʽ��"sensor_type:status"����"emg:������"��
        if (message.Contains(":"))
        {
            string[] parts = message.Split(':');
            if (parts.Length == 2)
            {
                string sensorType = parts[0].Trim();
                string status = parts[1].Trim();

                // �����̸߳���UI���޸�Ϊ��ȷ�����Ե��÷�ʽ��
                if (UnityMainThreadDispatcher.Instance != null)
                {
                    UnityMainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        UpdateSensorStatus(sensorType, status);
                    });
                }
            }
        }
    }

    // ���´�����״̬��ʾ
    private void UpdateSensorStatus(string sensorType, string status)
    {
        Color statusColor = status switch
        {
            "δ����" => Color.red,
            "��ѡ��" => Color.yellow,
            "������" => Color.green,
            "������" => Color.blue,
            _ => Color.gray
        };

        switch (sensorType)
        {
            case "emg":
                UpdateStatusText(emgStatusText, status, statusColor);
                break;
            case "imu":
                UpdateStatusText(imuStatusText, status, statusColor);
                break;
            case "pressure":
                UpdateStatusText(pressureStatusText, status, statusColor);
                break;
            case "connection":
                UpdateStatusText(connectionStatusText, status, statusColor);
                break;
        }
    }

    // �����ı���ɫ������
    private void UpdateStatusText(TMP_Text text, string content, Color color)
    {
        if (text != null)
        {
            text.text = content;
            text.color = color;
        }
    }

    // �������Python���޸ĳ�ʱ����ʽ��
    private void SendCommandToPython(string command)
    {
        try
        {
            Debug.Log($"[ZMQ����] ����: {command}");
            clientSocket.SendFrame(command);

            // �ȴ�Python�ظ����޸�Ϊ�޳�ʱ���������أ�
            string response;
            if (clientSocket.TryReceiveFrameString(out response))
            {
                Debug.Log($"[ZMQ�ظ�] {response}");
                if (response == "error")
                {
                    UpdateStatusText(connectionStatusText, "����ִ��ʧ��", Color.red);
                }
            }
            else
            {
                Debug.LogError("���������δ�յ��ظ�");
                UpdateStatusText(connectionStatusText, "����Ӧ", Color.red);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"�����������: {e.Message}");
            UpdateStatusText(connectionStatusText, "ͨ��ʧ��", Color.red);
        }
    }

    // ��ť����¼���������ѡ��
    public void ToggleEMG()
    {
        isEMGSelected = !isEMGSelected;
        UpdateSensorStatus("emg", isEMGSelected ? "��ѡ��" : "δ����");
        // ��ѡ��ʵʱ֪ͨPythonѡ��״̬
        SendCommandToPython($"select_sensor:emg:{isEMGSelected}");
    }

    public void ToggleIMU()
    {
        isIMUSelected = !isIMUSelected;
        UpdateSensorStatus("imu", isIMUSelected ? "��ѡ��" : "δ����");
        SendCommandToPython($"select_sensor:imu:{isIMUSelected}");
    }

    public void TogglePressure()
    {
        isPressureSelected = !isPressureSelected;
        UpdateSensorStatus("pressure", isPressureSelected ? "��ѡ��" : "δ����");
        SendCommandToPython($"select_sensor:pressure:{isPressureSelected}");
    }

    // �����豸��ť
    public void ConnectDevices()
    {
        // �������connect:emg,imu,pressure����������ѡ��Ĵ�������
        string command = "connect:";
        if (isEMGSelected) command += "emg,";
        if (isIMUSelected) command += "imu,";
        if (isPressureSelected) command += "pressure,";

        // �Ƴ�ĩβ����
        if (command.EndsWith(","))
            command = command.Substring(0, command.Length - 1);

        if (command != "connect:")
        {
            SendCommandToPython(command);
            UpdateStatusText(connectionStatusText, "������...", Color.blue);
        }
        else
        {
            UpdateStatusText(connectionStatusText, "����ѡ�񴫸���", Color.red);
        }
    }
}