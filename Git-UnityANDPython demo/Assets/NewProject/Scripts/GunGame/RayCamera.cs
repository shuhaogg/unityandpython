using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayCamera : MonoBehaviour
{

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        //声明一个Ray结构体，用于存储该射线的发射点，方向
        RaycastHit hitInfo;
        //声明一个RaycastHit结构体，存储碰撞信息
        if (Physics.Raycast(ray, out hitInfo))
        {
            // Debug.Log(hitInfo.collider.gameObject.name);
            Debug.DrawLine(transform.position, hitInfo.point, Color.yellow);
            //这里使用了RaycastHit结构体中的collider属性
            //因为hitInfo是一个结构体类型，其collider属性用于存储射线检测到的碰撞器。
            //通过collider.gameObject.name，来获取该碰撞器的游戏对象的名字。
        }
    }


}
