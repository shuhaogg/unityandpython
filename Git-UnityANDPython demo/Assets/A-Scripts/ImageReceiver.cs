using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using System.Collections;
using System.Threading;

public class ImageReceiver : MonoBehaviour
{
    [Tooltip("����ͼ���RawImage���")]
    public UnityEngine.UI.RawImage targetImage;

    private Thread _receiveThread;
    private bool _running = true;
    private byte[] _latestFrame;
    private Texture2D _texture;

    void Start()
    {
        UnityEngine.Application.targetFrameRate = 60;
        _texture = new Texture2D(2, 2);
        targetImage.texture = _texture;
        
        _receiveThread = new Thread(ReceiveImageTask);
        _receiveThread.Start();
    }

    void ReceiveImageTask()
    {
        AsyncIO.ForceDotNet.Force();
        using (var subscriber = new SubscriberSocket())
        {
            subscriber.Connect("tcp://localhost:5555"); // ��Python��IPһ��
            subscriber.SubscribeToAnyTopic();

            while (_running)
            {
                if (subscriber.TryReceiveFrameBytes(out byte[] frame))
                {
                    lock (this) // �̰߳�ȫ��
                    {
                        _latestFrame = frame;
                    }
                }
            }
            subscriber.Close();
        }
        NetMQConfig.Cleanup();
    }

    void Update()
    {
        if (_latestFrame != null && _latestFrame.Length > 0)
        {
            lock (this)
            {
                _texture.LoadImage(_latestFrame);
                _texture.Apply();
            }
        }
    }

    void OnDestroy()
    {
        _running = false;
        if (_receiveThread != null && _receiveThread.IsAlive)
        {
            _receiveThread.Join();
        }
    }
}