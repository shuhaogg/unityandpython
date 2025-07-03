using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using System;

public class PythonCommandSender : MonoBehaviour
{
    // Python �����ĵ�ַ�Ͷ˿�
    private string pythonAddress = "tcp://127.0.0.1:6666"; // ���ز�����

    public void SendStartTeaching()
    {
        SendCommandToPython("start_teaching");
    }

    public void SendRecordStart()
    {
        SendCommandToPython("record_start");
    }

    public void SendRecordEnd()
    {
        SendCommandToPython("record_end");
    }

    public void SendZeroPos()
    {
        SendCommandToPython("zero_pos");
    }

    public void EnableMotor()
    {
        SendCommandToPython("enable_motor");
    }

    public void DisableMotor()
    {
        SendCommandToPython("disable_motor");
    }
    public void SendSelectAction(string actionName)
    {
        if (!string.IsNullOrEmpty(actionName))
        {
            SendCommandToPython($"select_action:{actionName}");
        }
        else
        {
            Debug.LogWarning("[ZMQ] ��������Ϊ�գ��޷����� select_action");
        }
    }
    public void SendStartPasstrain()
    {
        SendCommandToPython("start_passive_training");
    }
    public void ConnectSelectedSensors(bool sEMG, bool imu, bool pressure)
    {
        SensorCommand cmd = new SensorCommand()
        {
            connect_sEMG = sEMG,
            connect_IMU = imu,
            connect_Pressure = pressure
        };

        string json = JsonUtility.ToJson(cmd);
        SendCommandToPython(json); // �������������߼�
    }

    // ���ݽṹ
    [System.Serializable]
    public class SensorCommand
    {
        public bool connect_sEMG;
        public bool connect_IMU;
        public bool connect_Pressure;
    }

    private void SendCommandToPython(string command)
    {
        try
        {
            using (var requestSocket = new RequestSocket())
            {
                requestSocket.Connect(pythonAddress);
                requestSocket.SendFrame(command);

                if (requestSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(500), out string response))
                {
                    Debug.Log($"[ZMQ] Python �ظ���{response}");
                }
                else
                {
                    Debug.LogWarning("[ZMQ] �޷��յ� Python ��Ӧ��");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ZMQ] ��������ʧ�ܣ�{e.Message}");
        }
    }
}
