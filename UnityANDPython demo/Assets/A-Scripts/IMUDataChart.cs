using UnityEngine;
using XCharts.Runtime;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System;
using System.Text;

[DisallowMultipleComponent]
[RequireComponent(typeof(SimplifiedLineChart))]
public class IMUDataChart : MonoBehaviour
{
    [Header("ZMQ Settings")]
    [SerializeField] private string zmqAddress = "tcp://localhost:5556";
    [SerializeField] private int maxCache = 500;
    [SerializeField] private string displayField = "AccX";

    [Header("Chart Settings")]
    [SerializeField] private Vector2 yAxisRange = new Vector2(-20, 20);
    [SerializeField] private Color lineColor = Color.green;

    private SubscriberSocket subscriber;
    private CancellationTokenSource cts;
    private SimplifiedLineChart chart;
    private Thread zmqThread;
    private ConcurrentQueue<float> dataQueue = new ConcurrentQueue<float>();
    private volatile bool isRunning = false; // 新增运行状态标志

    void Awake()
    {
        AsyncIO.ForceDotNet.Force();
        chart = GetComponent<SimplifiedLineChart>();
        InitializeChart();
        StartZmqThread();
    }

    void InitializeChart()
    {
        chart.RemoveData();
        chart.SetMaxCache(maxCache);

        var serie = chart.AddSerie<Line>(displayField);
        serie.symbol.show = false;
        serie.lineStyle.color = lineColor;
        serie.animation.enable = false;

        chart.GetChartComponent<YAxis>().min = yAxisRange.x;
        chart.GetChartComponent<YAxis>().max = yAxisRange.y;
    }

    void StartZmqThread()
    {
        isRunning = true;
        cts = new CancellationTokenSource();
        zmqThread = new Thread(() => ReceiveData(cts.Token))
        {
            IsBackground = true,
            Priority = System.Threading.ThreadPriority.BelowNormal
        };
        zmqThread.Start();
    }

    void ReceiveData(CancellationToken token)
    {
        try
        {
            using (subscriber = new SubscriberSocket())
            {
                subscriber.Options.Linger = TimeSpan.FromMilliseconds(100);
                subscriber.Connect(zmqAddress);
                subscriber.Subscribe("imu");

                Debug.Log($"ZMQ线程启动: {zmqAddress}");

                while (isRunning && !token.IsCancellationRequested)
                {
                    try
                    {
                        if (subscriber.TryReceiveFrameBytes(TimeSpan.FromMilliseconds(100), out byte[] topicBytes) &&
                            subscriber.TryReceiveFrameBytes(TimeSpan.FromMilliseconds(100), out byte[] dataBytes))
                        {
                            string jsonData = Encoding.UTF8.GetString(dataBytes);
                            ProcessIMUData(jsonData);
                        }
                    }
                    catch (Exception e)
                    {
                        if (isRunning) // 仅记录未关闭时的异常
                            Debug.LogWarning($"接收异常: {e}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"ZMQ线程异常: {ex}");
        }
        finally
        {
            Debug.Log("ZMQ线程退出");
        }
    }

    void ProcessIMUData(string jsonData)
    {
        try
        {
            var data = JsonUtility.FromJson<IMUData>(jsonData);
            if (data != null)
            {
                float value = GetFieldValue(data);
                dataQueue.Enqueue(value);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"解析失败: {e}");
        }
    }

    float GetFieldValue(IMUData data)
    {
        // 修改10：添加空值检查
        if (data == null) return 0;

        switch (displayField)
        {
            case "AccX": return data.AccX;
            case "AccY": return data.AccY;
            case "AccZ": return data.AccZ;
            case "AsX": return data.AsX;
            case "AsY": return data.AsY;
            case "AsZ": return data.AsZ;
            case "AngX": return data.AngX;
            case "AngY": return data.AngY;
            case "AngZ": return data.AngZ;
            default: return 0;
        }
    }

    void Update()
    {
        try
        {
            int count = 0;
            while (dataQueue.TryDequeue(out float value) && count < 1000)
            {
                // 修改11：确保使用正确的系列索引
                chart.AddData(0, value);
                count++;
            }

            UpdateXAxisRange();

            // 修改12：强制刷新图表
            if (count > 0)
            {
                chart.RefreshChart();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"更新异常: {e}");
        }
    }

    void UpdateXAxisRange()
    {
        var serie = chart.GetSerie(0);
        if (serie == null) return;

        // 修改13：动态调整X轴范围
        int dataCount = serie.dataCount;
        var xAxis = chart.GetChartComponent<XAxis>();

        if (dataCount > maxCache)
        {
            xAxis.min = dataCount - maxCache;
            xAxis.max = dataCount;
        }
        else
        {
            xAxis.min = 0;
            xAxis.max = maxCache;
        }
    }

    void OnDestroy()
    {
        isRunning = false; // 先设置标志位

        // 取消任务
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }

        // 清理ZMQ资源
        if (subscriber != null)
        {
            subscriber.Dispose();
            subscriber = null;
        }

        // 等待线程退出
        if (zmqThread != null && zmqThread.IsAlive)
        {
            if (!zmqThread.Join(1500))
            {
                zmqThread.Abort(); // 强制终止
                Debug.LogWarning("强制终止ZMQ线程");
            }
            zmqThread = null;
        }

        // 清理NetMQ上下文
        NetMQConfig.Cleanup(block: false);
        Debug.Log("资源释放完成");
    }

    [System.Serializable]
    private class IMUData
    {
        public float AccX;
        public float AccY;
        public float AccZ;
        public float AsX;
        public float AsY;
        public float AsZ;
        public float AngX;
        public float AngY;
        public float AngZ;
        public double time;
    }
}
