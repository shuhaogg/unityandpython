using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;
using NetMQ;
using NetMQ.Sockets;
using AsyncIO;


public class Pyzmq : MonoBehaviour
{
    
    private string zmqAddress = "tcp://localhost:5555"; // Python 端 ZMQ 发送的地址
    private SubscriberSocket subscriberSocket;
    private bool isRunning = true;
    private BaseChart chart;
    private int dataCount = 0;

    void Start()
    {
        // 尝试获取 Xchart 组件
        chart = GetComponent<BaseChart>();
        if (chart == null)
        {
            Debug.LogError("BaseChart 组件未找到！请在挂载了 My2901NetMQReceiver 脚本的 GameObject 上添加一个 Xchart 图表组件（如 SimplifiedLineChart）。");
            enabled = false; // 禁用脚本，避免后续运行报错
            return;
        }

        InitChart();

        // 初始化 NetMQ
        ForceDotNet.Force(); // 防止 Unity 冻结
        subscriberSocket = new SubscriberSocket();
        subscriberSocket.Connect(zmqAddress);
        subscriberSocket.Subscribe(""); // 订阅所有主题
    }

    void Update()
    {
        if (!isRunning || subscriberSocket == null) return;

        try
        {
            
            // 尝试接收数据流
            string message;
            if (subscriberSocket.TryReceiveFrameString(out message))
            {
                // 解析 JSON 数据并更新图表
                var data = JsonUtility.FromJson<My2901Data>(message);
                UpdateChart(data);
            }
        }
        catch (NetMQException ex)
        {
            Debug.LogError($"NetMQ Exception: {ex.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"意外错误: {ex.Message}");
        }
    }

    void InitChart()
    {
        // 初始化图表配置
        var serie2 = chart.GetSerie(0); // sensor2
        //var serie3 = chart.GetSerie(1); // sensor3
        //var serie6 = chart.GetSerie(2); // sensor6
        //var serie7 = chart.GetSerie(3); // sensor7

        serie2.symbol.show = false;
        //serie3.symbol.show = false;
        //serie6.symbol.show = false;
        //serie7.symbol.show = false;
    }

    void UpdateChart(My2901Data data)
    {
        // 添加数据到图表
        chart.AddXAxisData(data.Time.ToString()); // 将 Time 作为 X 轴数据
        chart.AddData(0, data.sensor2); // sensor2 数据到第一个系列
        //chart.AddData(1, data.sensor3); // sensor3 数据到第二个系列
        //chart.AddData(2, data.sensor6); // sensor6 数据到第三个系列
        //chart.AddData(3, data.sensor7); // sensor7 数据到第四个系列
    }

    void OnApplicationQuit()
    {
        // 确保退出时关闭 ZMQ 连接
        isRunning = false;
        if (subscriberSocket != null)
        {
            subscriberSocket.Close();
        }
        NetMQConfig.Cleanup(); // 清理 NetMQ 配置
    }

    [System.Serializable]
    public class My2901Data
    {
        public float Time;
        public float sensor2;
        //public float sensor3;
        //public float sensor6;
        //public float sensor7;
    }
}
