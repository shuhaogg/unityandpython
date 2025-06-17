using UnityEngine;
using System.Collections;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine.UI;

[System.Serializable]
public class Coord
{
    public float x, y, z;
}

public class HummerController : MonoBehaviour
{
    [Header("Physical workspace bounds (mm)")]
    public float minX = 300f, maxX = 600f;
    public float minY = -200f, maxY = 200f;

    [Header("Effects")]
    public GameObject particle;
    public AudioClip hitSE;

    [Header("Camera & UI")]
    public Camera MouseCamera;
    public Vector3 bias = Vector3.zero;

    [Header("Crosshair UI")]
    public RectTransform crosshairUI;    // 新增：UI 上的准星

    [Header("Debug UI (optional)")]
    public Text debugText;

    [HideInInspector]
    public Vector3 calposition;

    private AudioSource audioSource;

    private SubscriberSocket subSocket;
    private Thread recvThread;
    private volatile Coord recvCoord = new Coord();

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        AsyncIO.ForceDotNet.Force();
        subSocket = new SubscriberSocket();
        subSocket.Connect("tcp://localhost:5555");
        subSocket.Subscribe("");

        recvThread = new Thread(ReceiveLoop) { IsBackground = true };
        recvThread.Start();
    }

    private void ReceiveLoop()
    {
        while (true)
        {
            try
            {
                string msg = subSocket.ReceiveFrameString();
                recvCoord = JsonUtility.FromJson<Coord>(msg);
                Debug.Log($"[ZeroMQ RX] x={recvCoord.x:F1}, y={recvCoord.y:F1}, z={recvCoord.z:F1}");
            }
            catch { }
        }
    }

    IEnumerator Hit(Vector3 target)
    {
        transform.position = new Vector3(target.x, 0, target.z);
        Instantiate(particle, transform.position, Quaternion.identity);
        MouseCamera.GetComponent<CameraController2>().Shake();
        audioSource.PlayOneShot(hitSE);
        yield return new WaitForSeconds(0.1f);
        for (int i = 0; i < 6; i++)
        {
            transform.Translate(0, 0, 1.0f);
            yield return null;
        }
    }

    void Update()
    {
        // 2. 把收到的物理坐标归一化到 [0,1]  
        float normX = Mathf.InverseLerp(minX, maxX, recvCoord.x);
        float normY = Mathf.InverseLerp(minY, maxY, recvCoord.y);

        // 3. 再映射到屏幕像素（假设 Canvas 是全屏 Overlay）  
        float screenX = normX * Screen.width;
        float screenY = normY * Screen.height;

        calposition = new Vector3(screenX, screenY, 0) + bias;

        // 可视化：移动 UI 准星
        if (crosshairUI != null)
            crosshairUI.position = calposition;

        // 可视化调试：Console 或屏幕文本
        Debug.Log($"[Frame] recvCoord=({recvCoord.x:F1},{recvCoord.y:F1},{recvCoord.z:F1}), calpos=({calposition.x:F1},{calposition.y:F1})");
        if (debugText != null)
            debugText.text = $"Crosshair: {calposition.x:F0},{calposition.y:F0}";

        // 射线检测、打击逻辑
        if (Time.timeScale == 1)
        {
            Ray ray = MouseCamera.ScreenPointToRay(calposition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var mole = hit.collider.GetComponent<MoleController>();
                if (mole != null && mole.Hit())
                {
                    StartCoroutine(Hit(hit.collider.transform.position));
                    ScoreManager.score += 10;
                }
            }
        }
    }

    void OnDestroy()
    {
        if (subSocket != null)
        {
            subSocket.Close();
            subSocket.Dispose();
        }
        NetMQConfig.Cleanup();
        if (recvThread != null && recvThread.IsAlive)
            recvThread.Abort();
    }
}
