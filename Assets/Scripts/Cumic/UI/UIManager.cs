using Cumic;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    public void ShowPopup(string uiName)
    {
        GameObject popup = Resources.Load<GameObject>(uiName);
        if (popup == null)
        {
            Debug.LogError($"Popup {uiName} not found");
            return;
        }

        Instantiate(popup, transform);
    }
}