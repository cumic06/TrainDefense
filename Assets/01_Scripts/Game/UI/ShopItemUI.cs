using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrainDefense.Game.Datas;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Events;
using System.Text.RegularExpressions;
using System.Collections;

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

      [Header("Flash Settings")]
      [SerializeField]
      private float flashDuration = 0.1f;
      [SerializeField]
      private int flashLoopCount = 3;
      #endregion

      private UpgradeData _upgradeData;
      private Color _priceOriginalColor;

      private void Start()
      {
         buyButton.onClick.AddListener(OnBuyButtonClick);
         _upgradeData = DatabaseManager.Instance.GetUpgradeData(shopItemDataId);
         _priceOriginalColor = needMoneyText.color;
         SetUp();
      }

      public void SetUp()
      {
         if (_upgradeData != null)
         {
            float currentTotalValue = GetCurrentTotalValue();
            itemNameText.text = string.Format(_upgradeData.Name, currentTotalValue);

            itemDescriptionText.text = GetLevelDescription();
            needMoneyText.text = $"<sprite name=\"Coin\"> {GetCurrentCost().ToCommaString()}$";

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

         string desc = _upgradeData.Description;
         try
         {
            if (UserDataManager.Instance.IsUpgradeMaxLevel(shopItemDataId))
               return string.Format(desc, nextTotalValue, increaseAmountText, "Max", "Max");

            return string.Format(desc, nextTotalValue, increaseAmountText, currentLevel, _upgradeData.MaxUpgradeCount);
         }
         catch (System.FormatException)
         {
            return Regex.Replace(desc, @"\{[0-9]+\}", "-");
         }
      }

      private void OnBuyButtonClick()
      {
         if (UserDataManager.Instance.IsUpgradeMaxLevel(shopItemDataId))
            return;

         if (UserDataManager.Instance.Coin < GetCurrentCost())
         {
            _FlashPriceRed();
            return;
         }

         GameEventSystem.Publish(new BuyShopItemEvent(GetCurrentCost(), shopItemDataId));
      }

      private Coroutine _flashCoroutine;

      private void _FlashPriceRed()
      {
         if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);
         needMoneyText.color = _priceOriginalColor;
         _flashCoroutine = StartCoroutine(_FlashPriceRedRoutine());
      }

      private IEnumerator _FlashPriceRedRoutine()
      {
         for (int i = 0; i < flashLoopCount; i++)
         {
            needMoneyText.color = Color.red;
            yield return new WaitForSecondsRealtime(flashDuration);
            needMoneyText.color = _priceOriginalColor;
            yield return new WaitForSecondsRealtime(flashDuration);
         }
         _flashCoroutine = null;
      }
   }
}