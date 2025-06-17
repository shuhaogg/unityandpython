using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class SceneSwitcher : MonoBehaviour
{
    [Header("UI Settings")]
    public Button switchButton;
    public string targetSceneName = "IMU";

    private Vector3 originalScale; // �洢ԭʼ����

    void Start()
    {
        DOTween.Init();
        originalScale = switchButton.transform.localScale; // ��ʼ��ԭʼ����
        SetupButtonInteraction();
    }

    void SetupButtonInteraction()
    {
        switchButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(targetSceneName);
        });

        EventTrigger trigger = switchButton.gameObject.AddComponent<EventTrigger>();

        // ������
        EventTrigger.Entry entryEnter = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        entryEnter.callback.AddListener((data) =>
        {
            AnimateButton(switchButton.transform, originalScale * 1.1f);
        });

        // ����뿪
        EventTrigger.Entry entryExit = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        entryExit.callback.AddListener((data) =>
        {
            AnimateButton(switchButton.transform, originalScale);
        });

        trigger.triggers.Add(entryEnter);
        trigger.triggers.Add(entryExit);
    }

    void AnimateButton(Transform button, Vector3 targetScale)
    {
        button.DOKill(); // �ؼ�����ֹ�����еĶ���
        button.DOScale(targetScale, 0.3f)
              .SetEase(Ease.OutBack);
    }
}