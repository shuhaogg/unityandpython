using System.Collections;
using System.Collections.Generic;
//using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using System;

public class SnakeHead : MonoBehaviour
{
    public List<Transform> bodyList = new List<Transform>();
    public float velocity = 0.35f;
    public int step;
    private int x;
    private int y;
    private Vector3 headPos;
    private Transform canvas;
    private bool isDie = false;

    public AudioClip eatClip;
    public AudioClip dieClip;
    public GameObject dieEffect;
    public GameObject bodyPrefab;
    public Sprite[] bodySprites = new Sprite[2];

    private Thread zmqThread;
    private string zmqDirection = "";

    private int currX, currY;                // 记录当前方向
    private bool directionChangedThisStep;    // 本步是否已换向

    private SubscriberSocket subSocket;
    private volatile bool isRunning = true;


    void Awake()
    {
        canvas = GameObject.Find("Canvas").transform;
        gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>(PlayerPrefs.GetString("sh", "sh02"));
        bodySprites[0] = Resources.Load<Sprite>(PlayerPrefs.GetString("sb01", "sb0201"));
        bodySprites[1] = Resources.Load<Sprite>(PlayerPrefs.GetString("sb02", "sb0202"));
    }

    void Start()
    {
        InvokeRepeating("Move", 0, velocity);
        currX = step; currY = 0;            // 默认向👉
        directionChangedThisStep = false;    // 本步还没换向

        // 启动接收线程
        zmqThread = new Thread(ReceiveZmq) { IsBackground = true };
        zmqThread.Start();
    }

    //void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space) && MainUIController.Instance.isPause == false && isDie == false)
    //    {
    //        CancelInvoke();
    //        InvokeRepeating("Move", 0, velocity - 0.2f);
    //    }
    //    if (Input.GetKeyUp(KeyCode.Space) && MainUIController.Instance.isPause == false && isDie == false)
    //    {
    //        CancelInvoke();
    //        InvokeRepeating("Move", 0, velocity);
    //    }
    //    if (Input.GetKey(KeyCode.W) && y != -step && MainUIController.Instance.isPause == false && isDie == false)
    //    {
    //        gameObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
    //        x = 0; y = step;
    //    }
    //    if (Input.GetKey(KeyCode.S) && y != step && MainUIController.Instance.isPause == false && isDie == false)
    //    {
    //        gameObject.transform.localRotation = Quaternion.Euler(0, 0, 180);
    //        x = 0; y = -step;
    //    }
    //    if (Input.GetKey(KeyCode.A) && x != step && MainUIController.Instance.isPause == false && isDie == false)
    //    {
    //        gameObject.transform.localRotation = Quaternion.Euler(0, 0, 90);
    //        x = -step; y = 0;
    //    }
    //    if (Input.GetKey(KeyCode.D) && x != -step && MainUIController.Instance.isPause == false && isDie == false)
    //    {
    //        gameObject.transform.localRotation = Quaternion.Euler(0, 0, -90);
    //        x = step; y = 0;
    //    }
    //}

    // —— ZeroMQ 接收 —— //
    void ReceiveZmq()
    {
        AsyncIO.ForceDotNet.Force();        // 初始化 NetMQ 库
        subSocket = new SubscriberSocket();
        subSocket.Connect("tcp://localhost:5555");
        subSocket.Subscribe("");            // 订阅所有消息

        while (isRunning)
        {
            string msg;
            // 100ms 超时后会返回，继续检查 isRunning
            if (subSocket.TryReceiveFrameString(TimeSpan.FromMilliseconds(100), out msg))
            {
                zmqDirection = msg;
            }
        }
        // 循环退出后——清理 Socket
        subSocket.Close();
    }

    void OnDestroy()
    {
        // —— 清理顺序 —— 
        isRunning = false;                  // 1. 让线程跳出循环
        if (zmqThread != null && zmqThread.IsAlive)
        {
            zmqThread.Join(200);            // 2. 等它最多 200ms 结束
        }
        // subSocket 会在线程结束后自动 Close，但多调用一遍也安全
        subSocket?.Close();                 // 3. 确保断开
        NetMQConfig.Cleanup();              // 4. 清理所有 NetMQ 后台线程
    }

    void Update()
    {
        if (MainUIController.Instance.isPause || isDie) return;

        // —— 只有当本步还没换向，才尝试处理新指令 —— //
        if (!directionChangedThisStep && !string.IsNullOrEmpty(zmqDirection))
        {
            // —— 防止直接反向 —— //
            // 当前方向向量(currX, currY)，新指令对应(expectedX, expectedY)
            int expectedX = 0, expectedY = 0;
            switch (zmqDirection)
            {
                case "W": expectedY = step; expectedX = 0; break;
                case "S": expectedY = -step; expectedX = 0; break;
                case "A": expectedX = -step; expectedY = 0; break;
                case "D": expectedX = step; expectedY = 0; break;
            }
            // 反向检测：new + old == 0 表示反向，例如 (0,step)+ (0,-step) == (0,0)
            if (expectedX + currX != 0 || expectedY + currY != 0)
            {
                // 执行换向
                transform.localRotation = Quaternion.Euler(0, 0,
                    zmqDirection == "W" ? 0 :
                    zmqDirection == "S" ? 180 :
                    zmqDirection == "A" ? 90 : -90);
                currX = expectedX;
                currY = expectedY;
                directionChangedThisStep = true;  // ←—— 标记本步已换向
            }
            zmqDirection = "";  // 清空已消费
        }
    }

    void Move()
    {
        headPos = gameObject.transform.localPosition;                                               //保存下来蛇头移动前的位置
        gameObject.transform.localPosition = new Vector3(headPos.x + currX, headPos.y + currY, headPos.z);  //蛇头向期望位置移动
        if (bodyList.Count > 0)
        {
            //由于我们是双色蛇身，此方法弃用
            //bodyList.Last().localPosition = headPos;                                              //将蛇尾移动到蛇头移动前的位置
            //bodyList.Insert(0, bodyList.Last());                                                  //将蛇尾在List中的位置更新到最前
            //bodyList.RemoveAt(bodyList.Count - 1);                                                //移除List最末尾的蛇尾引用

            //由于我们是双色蛇身，使用此方法达到显示目的
            for (int i = bodyList.Count - 2; i >= 0; i--)                                           //从后往前开始移动蛇身
            {
                bodyList[i + 1].localPosition = bodyList[i].localPosition;                          //每一个蛇身都移动到它前面一个节点的位置
            }
            bodyList[0].localPosition = headPos;                                                    //第一个蛇身移动到蛇头移动前的位置
        }
        directionChangedThisStep = false;
    }

    void Grow()
    {
        AudioSource.PlayClipAtPoint(eatClip, Vector3.zero);
        int index = (bodyList.Count % 2 == 0) ? 0 : 1;
        GameObject body = Instantiate(bodyPrefab, new Vector3(2000, 2000, 0), Quaternion.identity);
        body.GetComponent<Image>().sprite = bodySprites[index];
        body.transform.SetParent(canvas, false);
        bodyList.Add(body.transform);
    }

    void Die()
    {
        AudioSource.PlayClipAtPoint(dieClip, Vector3.zero);
        CancelInvoke();
        isDie = true;
        Instantiate(dieEffect);
        PlayerPrefs.SetInt("lastl", MainUIController.Instance.length);
        PlayerPrefs.SetInt("lasts", MainUIController.Instance.score);
        if (PlayerPrefs.GetInt("bests", 0) < MainUIController.Instance.score)
        {
            PlayerPrefs.SetInt("bestl", MainUIController.Instance.length);
            PlayerPrefs.SetInt("bests", MainUIController.Instance.score);
        }
        StartCoroutine(GameOver(1.5f));
    }
    void OnApplicationQuit()
    {
        // 某些平台 Editor 停止时不会走 OnDestroy，双保险
        OnDestroy();
    }


    IEnumerator GameOver(float t)
    {
        yield return new WaitForSeconds(t);
        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Food"))
        {
            Destroy(collision.gameObject);
            MainUIController.Instance.UpdateUI();
            Grow();
            FoodMaker.Instance.MakeFood((UnityEngine.Random.Range(0, 100) < 20) ? true : false);
        }
        else if (collision.gameObject.CompareTag("Reward"))
        {
            Destroy(collision.gameObject);
            MainUIController.Instance.UpdateUI(UnityEngine.Random.Range(5, 15) * 10);
            Grow();
        }
        else if (collision.gameObject.CompareTag("Body"))
        {
            Die();
        }
        else
        {
            if (MainUIController.Instance.hasBorder)
            {
                Die();
            }
            else
            {
                switch (collision.gameObject.name)
                {
                    case "Up":
                        transform.localPosition = new Vector3(transform.localPosition.x, -transform.localPosition.y + 30, transform.localPosition.z);
                        break;
                    case "Down":
                        transform.localPosition = new Vector3(transform.localPosition.x, -transform.localPosition.y - 30, transform.localPosition.z);
                        break;
                    case "Left":
                        transform.localPosition = new Vector3(-transform.localPosition.x + 180, transform.localPosition.y, transform.localPosition.z);
                        break;
                    case "Right":
                        transform.localPosition = new Vector3(-transform.localPosition.x + 240, transform.localPosition.y, transform.localPosition.z);
                        break;
                }
            }
        }
    }
}
