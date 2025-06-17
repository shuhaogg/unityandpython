using UnityEngine;
using XCharts.Runtime;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using System.Collections.Generic;
using System;
using System.Collections;

[DisallowMultipleComponent]
[RequireComponent(typeof(SimplifiedLineChart))]
public class sEMGChart : MonoBehaviour
{
    // ��̬��������
    private static SubscriberSocket subscriber;
    private static CancellationTokenSource cts;
    private static Thread zmqThread;
    private static bool zmqInitialized = false;
    private static string zmqAddress = "tcp://localhost:5558";

    [Header("Channel Settings")]
    [SerializeField] private int channelIndex = 0;
    [SerializeField] private Color lineColor = Color.cyan;

    [Header("Filter Settings")]
    [SerializeField] private FilterType filterType = FilterType.MovingAverage;
    [SerializeField] private int filterWindowSize = 5;
    [SerializeField][Range(0.1f, 0.9f)] private float smoothFactor = 0.2f;

    [Header("Chart Settings")]
    [SerializeField] private int maxCache = 500;
    [SerializeField] private int pointsPerFrame = 50;
    [SerializeField] private bool simplifyLine = true;
    [SerializeField] private Vector2 yAxisRange = new Vector2(0, 1024);

    private enum FilterType { None, MovingAverage, ExponentialSmoothing }

    private SimplifiedLineChart chart;
    private static readonly ChannelData[] channelData = new ChannelData[4];
    private static readonly object dataLock = new object();

    [System.Serializable]
    public class sEMGData
    {
        public int ad1;
        public int ad2;
        public int ad3;
        public int ad4;
        public double time; // ��Python���ֶ�������ͬ��
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
            InitializeChannelData();
        }

        SetupChart();
        StartZmqThread();
    }

    void InitializeChannelData()
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

    void SetupChart()
    {
        chart.RemoveData();
        chart.SetMaxCache(maxCache);

        // ȷ��ϵ��������ͨ����Ӧ
        string[] serieNames = { "AD1", "AD2", "AD3", "AD4" };
        var serie = chart.AddSerie<Line>(serieNames[channelIndex]);
        serie.symbol.show = false;
        serie.lineStyle.color = lineColor;
        serie.animation.enable = false;
        serie.lineType = LineType.Smooth;

        // ����Y�᷶Χ
        chart.GetChartComponent<YAxis>().min = yAxisRange.x;
        chart.GetChartComponent<YAxis>().max = yAxisRange.y;

        // ����ʵʱ����
        chart.SetAllDirty();
    }

    static void StartZmqThread()
    {
        if (zmqInitialized) return;
        zmqInitialized = true;

        cts = new CancellationTokenSource();
        zmqThread = new Thread(() => ReceiveData(cts.Token));
        zmqThread.IsBackground = true; // ��Ϊ��̨�߳�
        zmqThread.Start();
    }

    static void ReceiveData(CancellationToken token)
    {
        using (subscriber = new SubscriberSocket())
        {
            subscriber.Connect(zmqAddress);
            subscriber.Subscribe("sEMG");
            subscriber.Options.ReceiveHighWatermark = 1000; // ���ӽ��ջ�����
            subscriber.Options.Linger = TimeSpan.Zero;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (subscriber.TryReceiveFrameString(TimeSpan.FromMilliseconds(10), out string topic) &&
                        subscriber.TryReceiveFrameString(TimeSpan.FromMilliseconds(10), out string jsonData))
                    {
                        // ��ӽ��յ���
                        UnityEngine.Debug.Log($"�յ�����: {jsonData}");

                        var data = JsonUtility.FromJson<sEMGData>(jsonData);

                        lock (dataLock)
                        {
                            channelData[0].rawQueue.Enqueue(data.ad1);
                            channelData[1].rawQueue.Enqueue(data.ad2);
                            channelData[2].rawQueue.Enqueue(data.ad3);
                            channelData[3].rawQueue.Enqueue(data.ad4);
                        }
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning($"ZMQ�쳣: {e}");
                }
            }
        }
    }

    void Update()
    {
        ProcessData();
        UpdateChartVisuals();
        chart.RefreshChart();
    }

    void ProcessData()
    {
        if (!chart) return;

        lock (dataLock)
        {
            int processed = 0;
            while (channelData[channelIndex].rawQueue.Count > 0 && processed < pointsPerFrame)
            {
                int rawValue = channelData[channelIndex].rawQueue.Dequeue();
                float filteredValue = ApplyFilter(ref channelData[channelIndex], rawValue);

                // ������ݵ����
                // UnityEngine.Debug.Log($"������ݵ�: {filteredValue}"); 

                chart.AddData(0, filteredValue);
                processed++;

                TrimExcessData();
            }
        }
    }

    void TrimExcessData()
    {
        if (chart.GetSerie(0).dataCount > maxCache * 1.2f)
        {
            chart.series[0].data.RemoveRange(0, maxCache / 10);
        }
    }

    void UpdateChartVisuals()
    {
        var serie = chart.GetSerie(0);
        if (serie.dataCount > maxCache)
        {
            var xAxis = chart.GetChartComponent<XAxis>();
            xAxis.min = serie.dataCount - maxCache;
            xAxis.max = serie.dataCount;
            xAxis.interval = Mathf.Max(1, maxCache / 10);
        }

        if (simplifyLine && chart.series.Count > 0)
        {
            var line = (Line)chart.series[0];
            line.ignoreLineBreak = true;
        }
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

        return data.filterBuffer.Count > 0 ? sum / data.filterBuffer.Count : 0;
    }

    float ExponentialSmoothing(ref ChannelData data, int newValue)
    {
        data.smoothedValue = smoothFactor * newValue + (1 - smoothFactor) * data.smoothedValue;
        return data.smoothedValue;
    }

    void OnDestroy()
    {
        if (!zmqInitialized) return;

        // 1. ��ӳ�ʱ����
        cts?.CancelAfter(500); // 500ms��ʱ

        // 2. �ֲ�������Դ
        if (zmqThread != null && zmqThread.IsAlive)
        {
            if (!zmqThread.Join(1000)) // ���ȴ�1��
            {
                zmqThread.Interrupt();
            }
        }

        // 3. ȷ��Socket�ر�˳��
        if (subscriber != null)
        {
            subscriber.Dispose();
            subscriber = null;
        }

        // 4. ���NetMQ�����ӳ�
        StartCoroutine(DelayedCleanup());
    }

    private IEnumerator DelayedCleanup()
    {
        yield return new WaitForSecondsRealtime(0.5f); // �ȴ�����
        NetMQConfig.Cleanup();
        zmqInitialized = false;

        // 5. ǿ����������
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
    }
}