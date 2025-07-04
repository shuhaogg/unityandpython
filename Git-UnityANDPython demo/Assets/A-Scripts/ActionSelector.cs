using UnityEngine;
using TMPro;

public class ActionSelector : MonoBehaviour
{
    public string selectedActionName = "";
    public TextMeshProUGUI actionLabel;
    public PythonCommandSender commandSender;

    public void SelectAction(string actionName)
    {
        selectedActionName = actionName;
        if (actionLabel != null)
            actionLabel.text = $"已选择动作：{actionName}";
    }

    public void ConfirmSelection()
    {
        if (!string.IsNullOrEmpty(selectedActionName))
        {
            commandSender.SendSelectAction(selectedActionName);
        }
        else
        {
            Debug.LogWarning("未选择动作！");
        }
    }

    public void StartPasstrain()
    {
        commandSender.SendStartPasstrain();
    }
}