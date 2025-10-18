using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrainDefense.Game.UI
{
    public class ShopItemUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private UpgradeData shopItemData;
        [SerializeField]
        private Button buyButton;
        [SerializeField]
        private TextMeshProUGUI itemNameText;
        [SerializeField]
        private TextMeshProUGUI itemDescriptionText;
        [SerializeField]
        private TextMeshProUGUI needMoneyText;
        #endregion

        private void Start()
        {
            buyButton.onClick.AddListener(OnBuyButtonClick);
            SetUp();
        }

        private void SetUp()
        {
            if (shopItemData != null)
            {
                itemNameText.text = shopItemData.UpgradeName;
                itemDescriptionText.text = shopItemData.Description;
                needMoneyText.text = $"{shopItemData.NeedMoney}$";
            }
        }

        public void SetVaild(int currentMoney)
        {
            if (currentMoney >= shopItemData.NeedMoney)
            {
                buyButton.interactable = true;
            }
            else
            {
                buyButton.interactable = false;
            }
        }

        private void OnBuyButtonClick()
        {
            // GameEventSystem.Publish(new BuyShopItemEvent(shopItemData.NeedMoney));
        }
    }
}