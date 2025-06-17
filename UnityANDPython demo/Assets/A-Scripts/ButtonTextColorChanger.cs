using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static System.Net.Mime.MediaTypeNames;
using TMPro;


public class ButtonTextColorChanger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public TMP_Text buttonText;          // 绑定按钮的Text组件
    public Color pressedColor;       // 按下时的颜色
    private Color originalColor;     // 原始颜色

    void Start()
    {
        if (buttonText != null)
            originalColor = buttonText.color;
    }

    // 按下按钮时触发
    public void OnPointerDown(PointerEventData eventData)
    {
        if (buttonText != null)
            buttonText.color = pressedColor;
    }

    // 松开按钮时触发
    public void OnPointerUp(PointerEventData eventData)
    {
        if (buttonText != null)
            buttonText.color = originalColor;
    }
}