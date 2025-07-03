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
    private RequestSocket clientSocket;
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
        // 确保在场景销毁时停止所有线程和清理资源
        StopAllCoroutines();
        CleanupZMQ();
    }

    void OnApplicationQuit()
    {
        // 在应用程序退出时执行清理
        CleanupZMQ();
    }

    // 初始化ZMQ通信
    private void InitZMQ()
    {
        try
        {
            // 初始化NetMQ环境
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
            Debug.LogError($"ZMQ初始化失败: {e.Message}");
            UpdateStatusText(connectionStatusText, "通信初始化失败", Color.red);
        }
    }

    // 清理ZMQ资源
    private void CleanupZMQ()
    {
        try
        {
            // 停止接收线程
            isRunning = false;

            // 关闭套接字
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

            // 等待线程结束（设置超时避免永久等待）
            if (receiveThread != null && receiveThread.IsAlive)
            {
                if (!receiveThread.Join(2000))  // 等待2秒
                {
                    Debug.LogWarning("接收线程未能及时退出，强制终止");
                    receiveThread.Abort();
                }
                receiveThread = null;
            }

            // 清理NetMQ资源
            NetMQConfig.Cleanup(false);
            Debug.Log("ZMQ资源已清理");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ZMQ清理失败: {e.Message}");
        }
    }

    // 接收状态更新（改进异常处理）
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
                else
                {
                    // 短暂休眠避免CPU占用过高
                    Thread.Sleep(100);
                }
            }
        }
        catch (ThreadAbortException)
        {
            // 线程被中止，正常退出
            Debug.Log("接收线程已中止");
        }
        catch (System.Exception e)
        {
            if (isRunning)  // 只在运行时记录错误
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
        // 消息格式："sensor_type:status"（如"emg:已连接"）
        if (message.Contains(":"))
        {
            string[] parts = message.Split(':');
            if (parts.Length == 2)
            {
                string sensorType = parts[0].Trim();
                string status = parts[1].Trim();

                // 在主线程更新UI（修改为正确的属性调用方式）
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

    // 发送命令到Python（修改超时处理方式）
    private void SendCommandToPython(string command)
    {
        try
        {
            Debug.Log($"[ZMQ发送] 命令: {command}");
            clientSocket.SendFrame(command);

            // 等待Python回复（修改为无超时参数的重载）
            string response;
            if (clientSocket.TryReceiveFrameString(out response))
            {
                Debug.Log($"[ZMQ回复] {response}");
                if (response == "error")
                {
                    UpdateStatusText(connectionStatusText, "命令执行失败", Color.red);
                }
            }
            else
            {
                Debug.LogError("发送命令后未收到回复");
                UpdateStatusText(connectionStatusText, "无响应", Color.red);
            }
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
        // 可选：实时通知Python选择状态
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
        SendCommandToPython($"select_sensor:pressure:{isPressureSelected}");
    }

    // 连接设备按钮
    public void ConnectDevices()
    {
        // 构建命令：connect:emg,imu,pressure（仅包含已选择的传感器）
        string command = "connect:";
        if (isEMGSelected) command += "emg,";
        if (isIMUSelected) command += "imu,";
        if (isPressureSelected) command += "pressure,";

        // 移除末尾逗号
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