using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MainMenuManager : MonoBehaviour
{
    // 步骤4中拖拽赋值
    public Button mainActionButton;

    void Start()
    {
        // 确保已导入DoTween插件
        DOTween.Init();

        // 绑定点击事件
        mainActionButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("ActiveTraining");
        });

        // 添加悬停动画
        EventTrigger trigger = mainActionButton.gameObject.AddComponent<EventTrigger>();

        // 鼠标进入时放大
        EventTrigger.Entry entryEnter = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        entryEnter.callback.AddListener((data) =>
        {
            ScaleButton(mainActionButton.transform, 1.1f);
        });

        // 鼠标离开时恢复
        EventTrigger.Entry entryExit = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        entryExit.callback.AddListener((data) =>
        {
            ScaleButton(mainActionButton.transform, 1f);
        });

        trigger.triggers.Add(entryEnter);
        trigger.triggers.Add(entryExit);
    }

    void ScaleButton(Transform btn, float targetScale)
    {
        btn.DOScale(targetScale * Vector3.one, 0.3f)
           .SetEase(Ease.OutBack);
    }
}
