using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game.UI
{
    public class ShopItemUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private string shopItemDataId;
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
            UpgradeData upgradeData = DataBaseManager.Instance.GetDB().GetUpgradeData(shopItemDataId);

            if (upgradeData != null)
            {
                itemNameText.text = upgradeData.Name;
                string description = string.Format(upgradeData.Description, upgradeData.UpgradeValue);
                itemDescriptionText.text = description;
                needMoneyText.text = $"{upgradeData.NeedMoney}$";
            }
        }

        public void SetVaild(int currentMoney)
        {
            UpgradeData upgradeData = DataBaseManager.Instance.GetDB().GetUpgradeData(shopItemDataId);
            
            if (currentMoney >= upgradeData.NeedMoney)
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