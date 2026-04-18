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
         _upgradeData = DatabaseManager.Instance.GetUpgradeData(shopItemDataId);
         SetUp();
      }

      public void SetUp()
      {
         if (_upgradeData != null)
         {
            float currentTotalValue = GetCurrentTotalValue();
            itemNameText.text = string.Format(_upgradeData.Name, currentTotalValue);

            itemDescriptionText.text = GetLevelDescription();
            needMoneyText.text = $"<sprite name=\"Coin\"> {GetCurrentCost()}$";

            if (UserDataManager.Instance.IsUpgradeMaxLevel(shopItemDataId))
            {
               buyButton.interactable = false;
            }
         }
      }

      private int GetCurrentCost()
      {
         int currentLevel = UserDataManager.Instance.GetUpgradeLevel(shopItemDataId);
         return _upgradeData.GetCostAtLevel(currentLevel);
      }

      private float GetCurrentTotalValue()
      {
         int currentLevel = UserDataManager.Instance.GetUpgradeLevel(shopItemDataId);
         float totalValue = 0;

         if (_upgradeData.UpgradeDataType == UpgradeDataType.NonTrainUpgrade)
         {
            totalValue = currentLevel * _upgradeData.UpgradeValue;
         }
         else if (_upgradeData.UpgradeDataType == UpgradeDataType.TrainUpgrade)
         {
            foreach (var stat in _upgradeData.Stats)
            {
               if (stat.Value == 0)
                  continue;
               totalValue += currentLevel * stat.Value;
            }
         }
         return totalValue;
      }

      private string GetLevelDescription()
      {
         int currentLevel = UserDataManager.Instance.GetUpgradeLevel(shopItemDataId);

         string increaseAmountText = "";
         float nextTotalValue = 0;
         float increaseAmount = 0;

         if (_upgradeData.UpgradeDataType == UpgradeDataType.NonTrainUpgrade)
         {
            nextTotalValue = (currentLevel + 1) * _upgradeData.UpgradeValue;
            increaseAmount = _upgradeData.UpgradeValue;
         }
         else if (_upgradeData.UpgradeDataType == UpgradeDataType.TrainUpgrade)
         {
            foreach (var stat in _upgradeData.Stats)
            {
               if (stat.Value == 0)
                  continue;

               nextTotalValue += (currentLevel + 1) * stat.Value;
               increaseAmount += stat.Value;
            }
         }

         if (increaseAmount > 0)
         {
            increaseAmountText = $"+{increaseAmount}";
         }
         else if (increaseAmount < 0)
         {
            increaseAmountText = $"{increaseAmount}";
         }

         if (UserDataManager.Instance.IsUpgradeMaxLevel(shopItemDataId))
         {
            return string.Format(_upgradeData.Description, nextTotalValue, increaseAmountText, "Max", "Max");
         }

         return string.Format(_upgradeData.Description, nextTotalValue, increaseAmountText, currentLevel, _upgradeData.MaxUpgradeCount);
      }

      public void SetVaild(int currentMoney)
      {
         if (UserDataManager.Instance.IsUpgradeMaxLevel(shopItemDataId))
         {
            buyButton.interactable = false;
            return;
         }

         if (currentMoney >= GetCurrentCost())
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
         GameEventSystem.Publish(new BuyShopItemEvent(GetCurrentCost(), shopItemDataId));
      }
   }
}