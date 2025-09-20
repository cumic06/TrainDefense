using UnityEngine;

namespace Cumic.UI
{
    public class UIManager : Singleton<UIManager>
    {
        public void ShowPopup(string uiName)
        {
            GameObject popup = Resources.Load<GameObject>(uiName);
            Instantiate(popup, transform);
        }
    }
}