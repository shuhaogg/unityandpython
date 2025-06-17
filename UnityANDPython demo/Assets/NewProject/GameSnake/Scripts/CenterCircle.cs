
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class CenterCircle : MonoBehaviour
{
    Vector3 mousePos;
    Vector3 MyPos;
    public Vector3 MoveDerectionPos;
    public float thlta;
    // Start is called before the first frame update
    void Start()
    {
        MyPos = transform.position;
        //  Debug.Log(MyPos.x + "," + MyPos.y + "," + MyPos.z);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            mousePos = Input.mousePosition;
            mousePos = Camera.main.ScreenToWorldPoint(mousePos);
            mousePos.z = MyPos.z;
            
            if (((mousePos.x - MyPos.x) * (mousePos.x - MyPos.x) + (mousePos.y - MyPos.y) * (mousePos.y - MyPos.y)) <= 2)
            {

                transform.position = mousePos;
                float thltaText = math.atan((mousePos.x - MyPos.x) / (mousePos.y - MyPos.y)) / math.PI * 180;

                //Debug.Log("thltaText:" + thltaText);
                MoveDerectionPos = mousePos - MyPos;
                if (MoveDerectionPos.y >= 0)
                    thlta = -thltaText;
                if (MoveDerectionPos.x >= 0 && MoveDerectionPos.y < 0)
                    thlta = -thltaText - 180;
                if (MoveDerectionPos.x <= 0 && MoveDerectionPos.y < 0)
                    thlta = 180 - thltaText;
                // Debug.Log("thlta:" + thlta);
                //Debug.Log("鼠标坐标：" + mousePos);
                //Debug.Log("物体坐标：" + MyPos);
                //Debug.Log("移动坐标：" +MoveDerectionPos);
                //  Debug.Log("在范围内");

            }

        }
    }
}





