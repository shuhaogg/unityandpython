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

    private Vector3 originalScale; // 存储原始缩放

    void Start()
    {
        DOTween.Init();
        originalScale = switchButton.transform.localScale; // 初始化原始缩放
        SetupButtonInteraction();
    }

    void SetupButtonInteraction()
    {
        switchButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(targetSceneName);
        });

        EventTrigger trigger = switchButton.gameObject.AddComponent<EventTrigger>();

        // 鼠标进入
        EventTrigger.Entry entryEnter = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        entryEnter.callback.AddListener((data) =>
        {
            AnimateButton(switchButton.transform, originalScale * 1.1f);
        });

        // 鼠标离开
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
        button.DOKill(); // 关键：终止进行中的动画
        button.DOScale(targetScale, 0.3f)
              .SetEase(Ease.OutBack);
    }
}