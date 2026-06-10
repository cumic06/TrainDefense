using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrainDefense.Game.Datas;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Events;
using System.Text.RegularExpressions;

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

      [Header("Price Color")]
      [Tooltip("보유 코인이 부족할 때 가격 텍스트에 적용할 색상")]
      [SerializeField]
      private Color insufficientColor = Color.gray;
      #endregion

      private UpgradeData _upgradeData;
      private Color _priceOriginalColor;

      private void Start()
      {
         buyButton.onClick.AddListener(OnBuyButtonClick);
         _upgradeData = DatabaseManager.Instance.GetUpgradeData(shopItemDataId);
         _priceOriginalColor = needMoneyText.color;
         GameEventSystem.Subscribe<ChangeCoinUIEvent>(_OnChangeCoin);
         SetUp();
      }

      private void OnDestroy()
      {
         buyButton.onClick.RemoveListener(OnBuyButtonClick);
         GameEventSystem.Unsubscribe<ChangeCoinUIEvent>(_OnChangeCoin);
      }

      public void SetUp()
      {
         if (_upgradeData != null)
         {
            float currentTotalValue = GetCurrentTotalValue();
            itemNameText.text = string.Format(_upgradeData.Name, currentTotalValue);

            itemDescriptionText.text = GetLevelDescription();
            if (UserDataManager.Instance.IsUpgradeMaxLevel(shopItemDataId))
               needMoneyText.text = "MAX";
            else
               needMoneyText.text = $"<sprite name=\"Coin\"> {GetCurrentCost().ToCommaString()}$";

            _UpdatePriceColor();
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
         float increaseAmount = 0;

         if (_upgradeData.UpgradeDataType == UpgradeDataType.NonTrainUpgrade)
         {
            increaseAmount = _upgradeData.UpgradeValue;
         }
         else if (_upgradeData.UpgradeDataType == UpgradeDataType.TrainUpgrade)
         {
            foreach (var stat in _upgradeData.Stats)
            {
               if (stat.Value == 0)
                  continue;

               increaseAmount += stat.Value;
            }
         }

         // 증가/감소 방향은 설명 문구로 표현하고, 값은 크기(양수)만 표시. (예: 공속 -1 → "+1")
         float perLevelAmount = Mathf.Abs(increaseAmount);
         if (increaseAmount != 0)
         {
            increaseAmountText = $"+{perLevelAmount}";
         }

         // 현재/최대 누적값 = 레벨 × 레벨당 증가량
         float currentValue = currentLevel * perLevelAmount;
         float maxValue = _upgradeData.MaxUpgradeCount * perLevelAmount;

         string desc = _upgradeData.Description;
         // {0}(다음 누적 총값)은 더 이상 표시하지 않음. 포맷 {1}=레벨당 증가량, {2}=현재 누적값, {3}=최대 누적값.
         try
         {
            if (UserDataManager.Instance.IsUpgradeMaxLevel(shopItemDataId))
               return string.Format(desc, string.Empty, increaseAmountText, maxValue, maxValue);

            return string.Format(desc, string.Empty, increaseAmountText, currentValue, maxValue);
         }
         catch (System.FormatException)
         {
            return Regex.Replace(desc, @"\{[0-9]+\}", "-");
         }
      }

      private void OnBuyButtonClick()
      {
         if (_upgradeData == null) return;

         if (UserDataManager.Instance.IsUpgradeMaxLevel(shopItemDataId))
            return;

         // 코인이 부족하면 가격 텍스트가 이미 회색으로 표시되어 있으므로 구매만 막는다.
         if (UserDataManager.Instance.Coin < GetCurrentCost())
            return;

         SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_ItemBuy, ignoreSuppress: true);

         GameEventSystem.Publish(new BuyShopItemEvent(GetCurrentCost(), shopItemDataId));
      }

      private void _OnChangeCoin(ChangeCoinUIEvent changeCoinEvent)
      {
         // 구독 순서에 의존하지 않도록 이벤트의 AfterCoin을 직접 기준으로 사용한다.
         _ApplyPriceColor(changeCoinEvent.AfterCoin);
      }

      // 현재 보유 코인이 구매 비용보다 적으면 가격 텍스트를 회색으로, 충분하면 원래 색으로 표시한다.
      private void _UpdatePriceColor()
      {
         int coin = UserDataManager.Instance != null ? UserDataManager.Instance.Coin : 0;
         _ApplyPriceColor(coin);
      }

      private void _ApplyPriceColor(int coin)
      {
         if (_upgradeData == null)
            return;

         needMoneyText.color = coin >= GetCurrentCost() ? _priceOriginalColor : insufficientColor;
      }
   }
}