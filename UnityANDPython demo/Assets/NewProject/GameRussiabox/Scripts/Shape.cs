
using UnityEngine;


public class Shape : MonoBehaviour 
{  
    //用于旋转的那个点
    public Transform pivot;
    private void Awake()
    {
        pivot = transform.Find("Pivot");
    }
}