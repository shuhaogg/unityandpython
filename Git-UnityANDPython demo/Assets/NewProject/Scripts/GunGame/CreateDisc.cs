using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateDisc : MonoBehaviour
{

    public GameObject m_disc; //定义公共游戏对象m_disc
    private Transform m_transform; //定义变换类型对象m_transform
    private float m_time = 5; //定义浮点类型m_time，作用是2s后生成盘子
    private Transform m_collection; //定义变换类型对象m_collection，这是用于收集克隆盘子的父类，相当于一个文件夹

    // Use this for initialization
    void Start()
    {
        m_transform = gameObject.GetComponent<Transform>(); //获取Transform组件
        m_collection = GameObject.Find("DiscCollection").gameObject.GetComponent<Transform>(); //获取名为DiscCollection的Transform组件
    }

    // Update is called once per frame
    void Update()
    {
        m_time -= Time.deltaTime;
        if (m_time < 0)
        {
            Batchdisc(); //调用批量生成函数
            m_time = 5; //计时用的时间重置为2
        }
    }

    private void Batchdisc()
    {
        for (int i = 0; i < 5; i++)
        { //批量生成5个
            Vector3 pos = new Vector3(Random.Range(0.0f, 8.0f), Random.Range(51.0f, 60.0f), Random.Range(3.0f, 20.0f)); //在特定区域内随机位置，定义为pos
            GameObject clonedisc = GameObject.Instantiate(m_disc, pos, Quaternion.identity); //在pos位置无旋转克隆m_disc，并赋给clonedisc对象
            clonedisc.GetComponent<Transform>().SetParent(m_collection); //把克隆体装入一个父类里，作用是美观
            GameObject.Destroy(clonedisc, 5.0f);//在5s后销毁克隆体
        }
    }
}