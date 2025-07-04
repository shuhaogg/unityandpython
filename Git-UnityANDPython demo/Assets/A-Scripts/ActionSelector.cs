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
            actionLabel.text = $"��ѡ������{actionName}";
    }

    public void ConfirmSelection()
    {
        if (!string.IsNullOrEmpty(selectedActionName))
        {
            commandSender.SendSelectAction(selectedActionName);
        }
        else
        {
            Debug.LogWarning("δѡ������");
        }
    }

    public void StartPasstrain()
    {
        commandSender.SendStartPasstrain();
    }
}