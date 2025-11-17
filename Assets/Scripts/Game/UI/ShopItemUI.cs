using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrainDefense.Game.Datas;
using Cumic.Events;
using TrainDefense.Game.Events;

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

        private UpgradeData _upgradeData;

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
                string description = GetLevelDescription(upgradeData);

                itemDescriptionText.text = description;
                needMoneyText.text = $"{upgradeData.NeedMoney}$";
            }
        }

        private string GetLevelDescription(UpgradeData upgradeData)
        {
            _upgradeData = upgradeData;

            int currentLevel = UserDataManager.Instance.GetUpgradeLevel(shopItemDataId);
            float nextTotalValue = (currentLevel + 1) * upgradeData.UpgradeValue;
            float increaseAmount = upgradeData.UpgradeValue;
            string increaseAmountText = "";
            if (increaseAmount > 0)
            {
                increaseAmountText = $"+{increaseAmount}";
            }
            else if (increaseAmount < 0)
            {
                increaseAmountText = $"{increaseAmount}";
            }

            string description = string.Format(upgradeData.Description, nextTotalValue, increaseAmountText);
            return description;
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
            GameEventSystem.Publish(new BuyShopItemEvent(_upgradeData.NeedMoney));
            SetVaild(UserDataManager.Instance.Coin);
        }
    }
}