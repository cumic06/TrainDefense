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
      private Color insufficientColor = Color.red;
      [Tooltip("최대 레벨(MAX)일 때 가격 텍스트에 적용할 색상")]
      [SerializeField]
      private Color maxLevelColor = Color.white;
      #endregion

      private UpgradeData _upgradeData;
      private Color _priceOriginalColor;

      private void Start()
      {
         _upgradeData = DatabaseManager.Instance.GetUpgradeData(shopItemDataId);
         _priceOriginalColor = needMoneyText.color;

         // 꾹 누르면 연속 구매되도록 버튼에 부착된 홀드 반복 핸들러에 구매 콜백을 연결한다.
         // 컴포넌트는 ShopItemButton 프리팹의 Button과 같은 GameObject에 미리 부착되어 있다.
         // 단발 탭은 OnPointerDown에서 1회 구매로 처리되므로 onClick은 더 이상 사용하지 않는다.
         ButtonHoldRepeater holdRepeater = buyButton.GetComponent<ButtonHoldRepeater>();
         if (holdRepeater != null)
            holdRepeater.Init(_TryBuy);

         GameEventSystem.Subscribe<ChangeCoinUIEvent>(_OnChangeCoin);
         SetUp();
      }

      private void OnDestroy()
      {
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

      // 구매 1회 시도. 성공하면 true, 더 살 수 없으면(최대 레벨/코인 부족) false를 반환해
      // 홀드 반복이 즉시 멈추도록 한다.
      private bool _TryBuy()
      {
         if (_upgradeData == null)
            return false;

         if (UserDataManager.Instance.IsUpgradeMaxLevel(shopItemDataId))
            return false;

         // 코인이 부족하면 가격 텍스트가 이미 빨간색으로 표시되어 있으므로 구매만 막는다.
         if (UserDataManager.Instance.Coin < GetCurrentCost())
            return false;

         SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_ItemBuy, ignoreSuppress: true);

         GameEventSystem.Publish(new BuyShopItemEvent(GetCurrentCost(), shopItemDataId));

         return true;
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

         // MAX 레벨은 코인 보유량과 무관하게 항상 흰색으로 표시한다. (MAX 판정을 가장 먼저)
         if (UserDataManager.Instance != null && UserDataManager.Instance.IsUpgradeMaxLevel(shopItemDataId))
         {
            needMoneyText.color = maxLevelColor;
            return;
         }

         needMoneyText.color = coin >= GetCurrentCost() ? _priceOriginalColor : insufficientColor;
      }
   }
}