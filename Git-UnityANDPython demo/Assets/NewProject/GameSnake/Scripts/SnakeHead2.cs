using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class SnakeHead2 : MonoBehaviour
{
    float interval = 0.5f;
    float count = 0f;
    float speed = 1.5f;

    public GameObject bodyPrefab;
    List<GameObject> snakeBody = new List<GameObject>();
    private Transform canvas;

    private bool isDie = false;
    public AudioClip eatClip;
    public AudioClip dieClip;
    public GameObject dieEffect;
    public Sprite[] bodySprites = new Sprite[2];

    void Awake()
    {
        canvas = GameObject.Find("Canvas").transform;
        //ͨ��Resources.Load(string path)����������Դ��path����д����Ҫ��Resources/�Լ��ļ���չ��
        gameObject.GetComponent<Image>().sprite = Resources.Load<Sprite>(PlayerPrefs.GetString("sh", "sh02"));
        bodySprites[0] = Resources.Load<Sprite>(PlayerPrefs.GetString("sb01", "sb0201"));
        bodySprites[1] = Resources.Load<Sprite>(PlayerPrefs.GetString("sb02", "sb0202"));
    }

    void Start()
    {
        GameObject canv = GameObject.Find("Canvas");
        for (int i = 0; i < 3; ++i)
        {
            Vector3 SnakeHeadPos = transform.position;
            SnakeHeadPos.z = 0;
            // Debug.Log("SnakeHeadPos:"+SnakeHeadPos);
            GameObject newbodynext = Instantiate<GameObject>(bodyPrefab,
            SnakeHeadPos - (i + 1) * new Vector3(0, 45f, 45f),/*40f����Ϊ��ͷ���ĵ����������ĵ�ľ���Ϊ45f*/ Quaternion.identity);

            // Debug.Log("����"+newbodynext.transform.position);
            newbodynext.transform.SetParent(canv.transform, false);//�ٽ�����Ϊcanvas��������
            int index = (i % 2 == 0) ? 0 : 1;
            //GameObject body = Instantiate(bodyPrefab, new Vector3(2000, 2000, 0), Quaternion.identity);
            newbodynext.GetComponent<Image>().sprite = bodySprites[index];
            snakeBody.Add(newbodynext);
        }
    }

    void Update()
    {
        count += speed * Time.deltaTime;
        if (count > interval)
        {
            count = 0;
            Vector3 tmpPosition = transform.position;    //��¼ͷ���仯ǰ��λ��
            List<Vector3> tmpList = new List<Vector3>(); //��¼����仯ǰ��λ�� 


            for (int i = 0; i < snakeBody.Count; ++i)
            {
                tmpList.Add(snakeBody[i].transform.position);
            }

            transform.Translate(0, 0.5f, 0);//��ʱ�ƶ�һ���ľ��루��ͷ���ĵ㵽�������ĵ�ľ��룩

            snakeBody[0].transform.position = tmpPosition;//��0�Ƶ�ͷ��֮ǰ��λ��

            for (int i = 1; i < snakeBody.Count; ++i)//����ǰ�������λ��
            {
                snakeBody[i].transform.position = tmpList[i - 1];
            }
        }

        //������Բ��ȡRotation��zֵ
        float thlta_z = GameObject.Find("Handle").GetComponent<CenterCircle>().thlta;
        //Debug.Log("ת�ǣ�"+thlta_z);
        transform.rotation = Quaternion.Euler(0, 0, thlta_z);

    }

    void Grow()
    {
        GameObject newbodynext = Instantiate<GameObject>(bodyPrefab, new Vector3(2000, 2000, 0), Quaternion.identity);
        newbodynext.transform.SetParent(canvas.transform, false);//�ٽ�����Ϊcanvas��������
        int index = (snakeBody.Count % 2 == 0) ? 0 : 1;
        //GameObject body = Instantiate(bodyPrefab, new Vector3(2000, 2000, 0), Quaternion.identity);
        newbodynext.GetComponent<Image>().sprite = bodySprites[index];
        snakeBody.Add(newbodynext);
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
            FoodMaker.Instance.MakeFood((Random.Range(0, 100) < 20) ? true : false);
        }
        else if (collision.gameObject.CompareTag("Reward"))
        {
            Destroy(collision.gameObject);
            MainUIController.Instance.UpdateUI(Random.Range(5, 15) * 10);
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













