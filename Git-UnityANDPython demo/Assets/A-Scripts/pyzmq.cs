using System.Collections.Generic;
using UnityEngine;
using XCharts.Runtime;
using NetMQ;
using NetMQ.Sockets;
using AsyncIO;


public class Pyzmq : MonoBehaviour
{
    
    private string zmqAddress = "tcp://localhost:5555"; // Python �� ZMQ ���͵ĵ�ַ
    private SubscriberSocket subscriberSocket;
    private bool isRunning = true;
    private BaseChart chart;
    private int dataCount = 0;

    void Start()
    {
        // ���Ի�ȡ Xchart ���
        chart = GetComponent<BaseChart>();
        if (chart == null)
        {
            Debug.LogError("BaseChart ���δ�ҵ������ڹ����� My2901NetMQReceiver �ű��� GameObject �����һ�� Xchart ͼ��������� SimplifiedLineChart����");
            enabled = false; // ���ýű�������������б���
            return;
        }

        InitChart();

        // ��ʼ�� NetMQ
        ForceDotNet.Force(); // ��ֹ Unity ����
        subscriberSocket = new SubscriberSocket();
        subscriberSocket.Connect(zmqAddress);
        subscriberSocket.Subscribe(""); // ������������
    }

    void Update()
    {
        if (!isRunning || subscriberSocket == null) return;

        try
        {
            
            // ���Խ���������
            string message;
            if (subscriberSocket.TryReceiveFrameString(out message))
            {
                // ���� JSON ���ݲ�����ͼ��
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
            Debug.LogError($"�������: {ex.Message}");
        }
    }

    void InitChart()
    {
        // ��ʼ��ͼ������
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
        // ������ݵ�ͼ��
        chart.AddXAxisData(data.Time.ToString()); // �� Time ��Ϊ X ������
        chart.AddData(0, data.sensor2); // sensor2 ���ݵ���һ��ϵ��
        //chart.AddData(1, data.sensor3); // sensor3 ���ݵ��ڶ���ϵ��
        //chart.AddData(2, data.sensor6); // sensor6 ���ݵ�������ϵ��
        //chart.AddData(3, data.sensor7); // sensor7 ���ݵ����ĸ�ϵ��
    }

    void OnApplicationQuit()
    {
        // ȷ���˳�ʱ�ر� ZMQ ����
        isRunning = false;
        if (subscriberSocket != null)
        {
            subscriberSocket.Close();
        }
        NetMQConfig.Cleanup(); // ���� NetMQ ����
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
