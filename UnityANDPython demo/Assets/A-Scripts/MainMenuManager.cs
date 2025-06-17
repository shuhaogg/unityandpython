using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MainMenuManager : MonoBehaviour
{
    // ����4����ק��ֵ
    public Button mainActionButton;

    void Start()
    {
        // ȷ���ѵ���DoTween���
        DOTween.Init();

        // �󶨵���¼�
        mainActionButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("ActiveTraining");
        });

        // �����ͣ����
        EventTrigger trigger = mainActionButton.gameObject.AddComponent<EventTrigger>();

        // ������ʱ�Ŵ�
        EventTrigger.Entry entryEnter = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        entryEnter.callback.AddListener((data) =>
        {
            ScaleButton(mainActionButton.transform, 1.1f);
        });

        // ����뿪ʱ�ָ�
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
