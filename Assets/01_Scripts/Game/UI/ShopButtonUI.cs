using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    public class ShopButtonUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private Button shopButton;
        [SerializeField]
        private ShopUI shopUI;
        #endregion

        private void Start()
        {
            shopButton.onClick.AddListener(OnShopButtonClick);
        }

        private void OnShopButtonClick()
        {
            if (shopUI.IsShopOpen)
            {
                shopUI.CloseShop();
                return;
            }
            shopUI.OpenShop();
        }

        public void OnShopOpen()
        {
            shopUI.OpenShop();
        }
    }
}