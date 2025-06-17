using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateDisc : MonoBehaviour
{

    public GameObject m_disc; //���幫����Ϸ����m_disc
    private Transform m_transform; //����任���Ͷ���m_transform
    private float m_time = 5; //���帡������m_time��������2s����������
    private Transform m_collection; //����任���Ͷ���m_collection�����������ռ���¡���ӵĸ��࣬�൱��һ���ļ���

    // Use this for initialization
    void Start()
    {
        m_transform = gameObject.GetComponent<Transform>(); //��ȡTransform���
        m_collection = GameObject.Find("DiscCollection").gameObject.GetComponent<Transform>(); //��ȡ��ΪDiscCollection��Transform���
    }

    // Update is called once per frame
    void Update()
    {
        m_time -= Time.deltaTime;
        if (m_time < 0)
        {
            Batchdisc(); //�����������ɺ���
            m_time = 5; //��ʱ�õ�ʱ������Ϊ2
        }
    }

    private void Batchdisc()
    {
        for (int i = 0; i < 5; i++)
        { //��������5��
            Vector3 pos = new Vector3(Random.Range(0.0f, 8.0f), Random.Range(51.0f, 60.0f), Random.Range(3.0f, 20.0f)); //���ض����������λ�ã�����Ϊpos
            GameObject clonedisc = GameObject.Instantiate(m_disc, pos, Quaternion.identity); //��posλ������ת��¡m_disc��������clonedisc����
            clonedisc.GetComponent<Transform>().SetParent(m_collection); //�ѿ�¡��װ��һ�����������������
            GameObject.Destroy(clonedisc, 5.0f);//��5s�����ٿ�¡��
        }
    }
}