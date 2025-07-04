using UnityEngine;
using TMPro;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;

public class SensorController : MonoBehaviour
{
    // UI组件引用
    public TMP_Text emgStatusText;
    public TMP_Text imuStatusText;
    public TMP_Text pressureStatusText;
    public TMP_Text connectionStatusText;

    // 传感器选择状态
    private bool isEMGSelected = false;
    private bool isIMUSelected = false;
    private bool isPressureSelected = false;

    // ZMQ通信相关
    private DealerSocket clientSocket;  // 使用Dealer套接字替代Request
    private SubscriberSocket subSocket;
    private Thread receiveThread;
    private bool isRunning = true;

    // ZMQ配置
    private const string RequestAddress = "tcp://localhost:6666";
    private const string SubAddress = "tcp://localhost:5555";

    void Start()
    {
        UpdateStatusText(emgStatusText, "未连接", Color.red);
        UpdateStatusText(imuStatusText, "未连接", Color.red);
        UpdateStatusText(pressureStatusText, "未连接", Color.red);
        UpdateStatusText(connectionStatusText, "连接设备", Color.white);

        InitZMQ();
    }

    void OnDestroy()
    {
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
            // 使用Dealer套接字替代Request
            clientSocket = new DealerSocket();
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
            Debug.LogError($"ZMQ初始化失败: {e.Message}");
            UpdateStatusText(connectionStatusText, "通信初始化失败", Color.red);
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

            if (subSocket != null)
            {
                subSocket.Close();
                subSocket = null;
            }

            if (receiveThread != null && receiveThread.IsAlive)
            {
                if (!receiveThread.Join(2000))
                {
                    Debug.LogWarning("接收线程未能及时退出，强制终止");
                    receiveThread.Abort();
                }
                receiveThread = null;
            }

            NetMQConfig.Cleanup(false);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ZMQ清理失败: {e.Message}");
        }
    }

    // 接收状态更新
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
                    Debug.Log($"[ZMQ接收] 主题: {topic}, 消息: {message}");
                    ProcessSensorStatus(message);
                }

                // 检查是否有来自Python的回复
                string response;
                while (clientSocket.TryReceiveFrameString(out response))
                {
                    Debug.Log($"[ZMQ回复] {response}");
                    // 这里可以处理回复，但由于是非阻塞的，我们可能不需要立即处理
                }

                Thread.Sleep(100);
            }
        }
        catch (ThreadAbortException)
        {
            Debug.Log("接收线程已中止");
        }
        catch (System.Exception e)
        {
            if (isRunning)
                Debug.LogError($"接收线程异常: {e.Message}");
        }
        finally
        {
            Debug.Log("接收线程已退出");
        }
    }

    // 处理传感器状态消息
    private void ProcessSensorStatus(string message)
    {
        if (message.Contains(":"))
        {
            string[] parts = message.Split(':');
            if (parts.Length == 2)
            {
                string sensorType = parts[0].Trim();
                string status = parts[1].Trim();

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

    // 更新传感器状态显示
    private void UpdateSensorStatus(string sensorType, string status)
    {
        Color statusColor = status switch
        {
            "未连接" => Color.red,
            "已选择" => Color.yellow,
            "已连接" => Color.green,
            "连接中" => Color.blue,
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

    // 更新文本颜色和内容
    private void UpdateStatusText(TMP_Text text, string content, Color color)
    {
        if (text != null)
        {
            text.text = content;
            text.color = color;
        }
    }

    // 发送命令到Python（修改为非阻塞方式）
    private void SendCommandToPython(string command)
    {
        try
        {
            Debug.Log($"[ZMQ发送] 命令: {command}");
            clientSocket.SendFrame(command);

            // 不再立即接收响应，由ReceiveStatusUpdates线程处理
        }
        catch (System.Exception e)
        {
            Debug.LogError($"发送命令错误: {e.Message}");
            UpdateStatusText(connectionStatusText, "通信失败", Color.red);
        }
    }

    // 按钮点击事件（传感器选择）
    public void ToggleEMG()
    {
        isEMGSelected = !isEMGSelected;
        UpdateSensorStatus("emg", isEMGSelected ? "已选择" : "未连接");
        SendCommandToPython($"select_sensor:emg:{isEMGSelected}");
    }

    public void ToggleIMU()
    {
        isIMUSelected = !isIMUSelected;
        UpdateSensorStatus("imu", isIMUSelected ? "已选择" : "未连接");
        SendCommandToPython($"select_sensor:imu:{isIMUSelected}");
    }

    public void TogglePressure()
    {
        isPressureSelected = !isPressureSelected;
        UpdateSensorStatus("pressure", isPressureSelected ? "已选择" : "未连接");
        // 修改传感器名称为my2901，与Python端保持一致
        SendCommandToPython($"select_sensor:my2901:{isPressureSelected}");
    }

    // 连接设备按钮
    public void ConnectDevices()
    {
        string command = "connect:";
        if (isEMGSelected) command += "emg,";
        if (isIMUSelected) command += "imu,";
        if (isPressureSelected) command += "pressure,";

        if (command.EndsWith(","))
            command = command.Substring(0, command.Length - 1);

        if (command != "connect:")
        {
            SendCommandToPython(command);
            UpdateStatusText(connectionStatusText, "连接中...", Color.blue);
        }
        else
        {
            UpdateStatusText(connectionStatusText, "请先选择传感器", Color.red);
        }
    }
}