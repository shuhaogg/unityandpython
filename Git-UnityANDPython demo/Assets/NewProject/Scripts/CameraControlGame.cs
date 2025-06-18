using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraControlGame : MonoBehaviour
{
    //����ͨ������Player����ת����������������ӽǵ������ƶ�������������Ҫһ��Player��Tranform
    public Transform player;
    //��������float����������ȡ����ƶ���ֵ
    private float mouseX, mouseY;
    //���ǿ��Ը��������һ��������
    public float mouseSensitivity;

    //mouseY�е�GetAxis�����᷵��-1��1֮��ĸ�������������ƶ���ʱ����ֵ�����ŷ���ı仯���仯������겻��ʱ����ֵ��ص���0���������Ǿͻ�������������ƶ�ʱ�ص�������
    public float xRotation;

    private void Update()
    {
        
            //��Update�����У�����ʹ������ϵͳ�е�GetAxis��������ȡ����ƶ���ֵ����������������ٳ���Time.deltatime,����ƶ���ֵ�������õ���
            //Input.GetAxis:����������ƶ���Ӧ��Ӧ��Ĺ����з��� -1 �� 1 ��ֵ
            mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;

            //ʹ����ѧ����Clamp����
            xRotation = Mathf.Clamp(xRotation, -70f, 70f);

            //����ʹ��Transform��Rotate()��������תplayer
            //Vector3.up�����ϵ�һ����ά��������һ��0��1��0����ά������һ����
            //������Ҫ����player��y����ת��������������ת
            player.Rotate(Vector3.up * mouseX);
            //����������Ҫѡת����ˣ�����ʹ��tranform.localRotation�����������������ת��ʹ��localRotation�Ϳ��Բ�����������תӰ�죬���һЩ��ֵ�����
            //��ΪlocalRotation�����ԣ����ǻ�Ҫ������ֵ
            transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        

    }
}
