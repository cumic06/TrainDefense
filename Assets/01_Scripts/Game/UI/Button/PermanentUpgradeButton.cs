using Cumic.UI;
using UnityEngine;

namespace TrainDefense
{
    /// <summary>
    /// 영구 업그레이드 팝업을 여는 버튼. 버튼 OnClick에 OnPermanentUpgradeButtonClick을 연결한다.
    /// (QuitButton과 동일하게 버튼 GameObject에 붙여 사용)
    /// </summary>
    public class PermanentUpgradeButton : MonoBehaviour
    {
        private const string PopupName = "Popup_PermanentUpgrade";

        public void OnPermanentUpgradeButtonClick()
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowPopup(PopupName);
        }
    }
}
