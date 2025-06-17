using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayCamera : MonoBehaviour
{

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        //����һ��Ray�ṹ�壬���ڴ洢�����ߵķ���㣬����
        RaycastHit hitInfo;
        //����һ��RaycastHit�ṹ�壬�洢��ײ��Ϣ
        if (Physics.Raycast(ray, out hitInfo))
        {
            // Debug.Log(hitInfo.collider.gameObject.name);
            Debug.DrawLine(transform.position, hitInfo.point, Color.yellow);
            //����ʹ����RaycastHit�ṹ���е�collider����
            //��ΪhitInfo��һ���ṹ�����ͣ���collider�������ڴ洢���߼�⵽����ײ����
            //ͨ��collider.gameObject.name������ȡ����ײ������Ϸ��������֡�
        }
    }


}
