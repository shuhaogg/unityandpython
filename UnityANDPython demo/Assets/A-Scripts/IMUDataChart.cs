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
    private volatile bool isRunning = false; // ��������״̬��־

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

                Debug.Log($"ZMQ�߳�����: {zmqAddress}");

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
                        if (isRunning) // ����¼δ�ر�ʱ���쳣
                            Debug.LogWarning($"�����쳣: {e}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"ZMQ�߳��쳣: {ex}");
        }
        finally
        {
            Debug.Log("ZMQ�߳��˳�");
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
            Debug.LogWarning($"����ʧ��: {e}");
        }
    }

    float GetFieldValue(IMUData data)
    {
        // �޸�10����ӿ�ֵ���
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
                // �޸�11��ȷ��ʹ����ȷ��ϵ������
                chart.AddData(0, value);
                count++;
            }

            UpdateXAxisRange();

            // �޸�12��ǿ��ˢ��ͼ��
            if (count > 0)
            {
                chart.RefreshChart();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"�����쳣: {e}");
        }
    }

    void UpdateXAxisRange()
    {
        var serie = chart.GetSerie(0);
        if (serie == null) return;

        // �޸�13����̬����X�᷶Χ
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
        isRunning = false; // �����ñ�־λ

        // ȡ������
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }

        // ����ZMQ��Դ
        if (subscriber != null)
        {
            subscriber.Dispose();
            subscriber = null;
        }

        // �ȴ��߳��˳�
        if (zmqThread != null && zmqThread.IsAlive)
        {
            if (!zmqThread.Join(1500))
            {
                zmqThread.Abort(); // ǿ����ֹ
                Debug.LogWarning("ǿ����ֹZMQ�߳�");
            }
            zmqThread = null;
        }

        // ����NetMQ������
        NetMQConfig.Cleanup(block: false);
        Debug.Log("��Դ�ͷ����");
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
