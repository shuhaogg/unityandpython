using UnityEngine;
using XCharts.Runtime;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using System.Collections.Generic;
using System;

[DisallowMultipleComponent]
[RequireComponent(typeof(SimplifiedLineChart))]
public class SensorDataChart : MonoBehaviour
{
    // 将共享配置设为静态
    private static SubscriberSocket subscriber;
    private static CancellationTokenSource cts;
    private static Thread zmqThread;
    private static bool zmqInitialized = false;
    private static string zmqAddress = "tcp://localhost:5559"; // 改为静态

    [Header("Channel Settings")]
    [SerializeField] private int channelIndex = 0;
    [SerializeField] private Color lineColor = Color.white;

    [Header("Filter Settings")]
    [SerializeField] private FilterType filterType = FilterType.MovingAverage;
    [SerializeField] private int filterWindowSize = 5;
    [SerializeField][Range(0.1f, 0.9f)] private float smoothFactor = 0.2f;

    [Header("Chart Settings")]
    [SerializeField] private int maxCache = 300; // 从500降低到300
    [SerializeField] private int pointsPerFrame = 50; // 新增帧率控制
    [SerializeField] private bool simplifyLine = true; // 新增线条简化
    [SerializeField] private Vector2 yAxisRange = new Vector2(0, 10000);


    private enum FilterType { None, MovingAverage, ExponentialSmoothing }

    private SimplifiedLineChart chart;
    private static readonly ChannelData[] channelData = new ChannelData[4];
    private static readonly object dataLock = new object();

    [System.Serializable]
    public class SensorData
    {
        public int sensor2;
        public int sensor3;
        public int sensor6;
        public int sensor7;
        public double time;
    }

    private struct ChannelData
    {
        public Queue<int> rawQueue;
        public Queue<float> filterBuffer;
        public float smoothedValue;
    }

    void Awake()
    {
        AsyncIO.ForceDotNet.Force();
        chart = GetComponent<SimplifiedLineChart>();

        if (channelData[0].rawQueue == null)
        {
            for (int i = 0; i < 4; i++)
            {
                channelData[i] = new ChannelData
                {
                    rawQueue = new Queue<int>(),
                    filterBuffer = new Queue<float>(filterWindowSize),
                    smoothedValue = 0
                };
            }
        }

        InitializeChart();
        StartZmqThread();
    }

    void InitializeChart()
    {
        chart.RemoveData();
        chart.SetMaxCache(maxCache);

        string[] serieNames = { "Sensor2", "Sensor3", "Sensor6", "Sensor7" };
        var serie = chart.AddSerie<Line>(serieNames[channelIndex]);
        serie.symbol.show = false;
        serie.lineStyle.color = lineColor;
        serie.animation.enable = false;
        serie.lineType = LineType.Smooth;

        chart.GetChartComponent<YAxis>().min = yAxisRange.x;
        chart.GetChartComponent<YAxis>().max = yAxisRange.y;
    }

    void StartZmqThread()
    {
        if (zmqInitialized) return;
        zmqInitialized = true;

        cts = new CancellationTokenSource();
        zmqThread = new Thread(() => ReceiveData(cts.Token));
        zmqThread.Start();
    }

    static void ReceiveData(CancellationToken token)
    {
        using (subscriber = new SubscriberSocket())
        {
            subscriber.Connect(zmqAddress);
            subscriber.Subscribe("sensors");
            subscriber.Options.Linger = TimeSpan.Zero;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (subscriber.TryReceiveFrameString(TimeSpan.FromMilliseconds(10), out string topic) &&
                        subscriber.TryReceiveFrameString(TimeSpan.FromMilliseconds(10), out string jsonData))
                    {
                        var data = JsonUtility.FromJson<SensorData>(jsonData);

                        lock (dataLock)
                        {
                            channelData[0].rawQueue.Enqueue(data.sensor2);
                            channelData[1].rawQueue.Enqueue(data.sensor3);
                            channelData[2].rawQueue.Enqueue(data.sensor6);
                            channelData[3].rawQueue.Enqueue(data.sensor7);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"ZMQ异常: {e}");
                }
            }
        }
    }

    void Update()
    {
        lock (dataLock)
        {
            int processed = 0;
            while (channelData[channelIndex].rawQueue.Count > 0 && processed < pointsPerFrame)
            {
                int rawValue = channelData[channelIndex].rawQueue.Dequeue();
                float filteredValue = ApplyFilter(ref channelData[channelIndex], rawValue);
                chart.AddData(0, filteredValue);
                processed++;

                // 动态清理旧数据
                if (chart.GetSerie(0).dataCount > maxCache * 1.2f)
                {
                    chart.series[0].data.RemoveRange(0, maxCache / 10);
                }
            }
        }

        UpdateXAxisRange();

        // 启用线条简化
        if (simplifyLine && chart.series.Count > 0)
        {
            var line = (Line)chart.series[0];
            line.ignoreLineBreak = true;  // 忽略小线段
            //line.simplifyPoints = true;   // 启用点简化
            //line.simplifyTolerance = 0.2f;// 简化阈值
        }

        chart.RefreshChart();
    }

    float ApplyFilter(ref ChannelData data, int newValue)
    {
        switch (filterType)
        {
            case FilterType.MovingAverage:
                return MovingAverageFilter(ref data, newValue);
            case FilterType.ExponentialSmoothing:
                return ExponentialSmoothing(ref data, newValue);
            default:
                return newValue;
        }
    }

    float MovingAverageFilter(ref ChannelData data, int newValue)
    {
        if (data.filterBuffer.Count >= filterWindowSize)
            data.filterBuffer.Dequeue();

        data.filterBuffer.Enqueue(newValue);

        float sum = 0;
        foreach (var val in data.filterBuffer)
            sum += val;

        return sum / data.filterBuffer.Count;
    }

    float ExponentialSmoothing(ref ChannelData data, int newValue)
    {
        data.smoothedValue = smoothFactor * newValue + (1 - smoothFactor) * data.smoothedValue;
        return data.smoothedValue;
    }

    void UpdateXAxisRange()
    {
        var serie = chart.GetSerie(0);
        if (serie.dataCount > maxCache)
        {
            // 保持固定窗口滚动显示
            var xAxis = chart.GetChartComponent<XAxis>();
            xAxis.min = serie.dataCount - maxCache;
            xAxis.max = serie.dataCount;
            xAxis.interval = Mathf.Max(1, maxCache / 10); // 调整刻度密度
        }
    }

    void OnDestroy()
    {
        if (!zmqInitialized) return;
        cts?.Cancel();
        if (zmqThread != null && zmqThread.IsAlive)
            zmqThread.Join(1000);

        subscriber?.Dispose();
        NetMQConfig.Cleanup();
        zmqInitialized = false;
    }
}