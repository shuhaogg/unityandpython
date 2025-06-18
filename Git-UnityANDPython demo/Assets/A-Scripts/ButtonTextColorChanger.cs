using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static System.Net.Mime.MediaTypeNames;
using TMPro;


public class ButtonTextColorChanger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public TMP_Text buttonText;          // �󶨰�ť��Text���
    public Color pressedColor;       // ����ʱ����ɫ
    private Color originalColor;     // ԭʼ��ɫ

    void Start()
    {
        if (buttonText != null)
            originalColor = buttonText.color;
    }

    // ���°�ťʱ����
    public void OnPointerDown(PointerEventData eventData)
    {
        if (buttonText != null)
            buttonText.color = pressedColor;
    }

    // �ɿ���ťʱ����
    public void OnPointerUp(PointerEventData eventData)
    {
        if (buttonText != null)
            buttonText.color = originalColor;
    }
}