using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.Manager;

namespace TrainDefense.Game.UI
{
   /// <summary>
   /// 상점의 '기차 수리' 항목. 부서진 기차를 모두 부활시키고 모든 기차를 일정 비율 회복한다(MainTrain.EmergencyRepair).
   /// 가격은 한 판 동안 지나온 누적 역 수(StageManager.TotalStationPassedCount)에 비례해 증가한다.
   /// </summary>
   public class ShopRepairItemUI : MonoBehaviour
   {
      #region Fields
      [SerializeField]
      private Button buyButton;
      [SerializeField]
      private TextMeshProUGUI itemNameText;
      [SerializeField]
      private TextMeshProUGUI itemDescriptionText;
      [SerializeField]
      private TextMeshProUGUI needMoneyText;

      [Header("Price")]
      [Tooltip("기본 수리 비용")]
      [SerializeField]
      private int baseCost = 100;
      [Tooltip("지나온 역 1개당 추가되는 수리 비용")]
      [SerializeField]
      private int costPerStation = 50;

      [Header("Repair Ratio")]
      [Tooltip("살아있던 기차의 회복 비율(0~1). 0.5 = 최대 체력의 50% 회복")]
      [SerializeField]
      private float aliveHealRatio = 0.5f;
      [Tooltip("부활시킨 기차의 부활 직후 HP 비율(0~1). 0.5 = 최대 체력의 50%")]
      [SerializeField]
      private float revivedHpRatio = 0.5f;

      [Header("Text")]
      [SerializeField]
      private string itemName = "기차 수리";
      [SerializeField]
      private string itemDescription = "부서진 기차 부활, 전체 체력 50% 회복";

      [Header("Price Color")]
      [Tooltip("보유 코인이 부족할 때 가격 텍스트에 적용할 색상")]
      [SerializeField]
      private Color insufficientColor = Color.red;
      #endregion

      private Color _priceOriginalColor;

      private void Start()
      {
         _priceOriginalColor = needMoneyText.color;

         buyButton.onClick.AddListener(_TryRepair);

         GameEventSystem.Subscribe<ChangeCoinUIEvent>(_OnChangeCoin);
         GameEventSystem.Subscribe<InspectionStartEvent>(_OnInspectionStart);
         SetUp();
      }

      private void OnDestroy()
      {
         buyButton.onClick.RemoveListener(_TryRepair);
         GameEventSystem.Unsubscribe<ChangeCoinUIEvent>(_OnChangeCoin);
         GameEventSystem.Unsubscribe<InspectionStartEvent>(_OnInspectionStart);
      }

      // 역을 통과할 때마다 가격이 오르므로 상점 진입 시점에 표시를 갱신한다.
      private void _OnInspectionStart(InspectionStartEvent inspectionStartEvent) => SetUp();

      public void SetUp()
      {
         itemNameText.text = itemName;
         itemDescriptionText.text = itemDescription;
         needMoneyText.text = $"<sprite name=\"Coin\"> {GetCurrentCost().ToCommaString()}$";

         _UpdatePriceColor();
      }

      private int GetCurrentCost()
      {
         int stationCount = StageManager.Instance != null ? StageManager.Instance.TotalStationPassedCount : 0;

         return baseCost + costPerStation * stationCount;
      }

      // 수리 1회 시도. 코인이 부족하거나 MainTrain이 없으면 아무 동작도 하지 않는다.
      private void _TryRepair()
      {
         int cost = GetCurrentCost();

         if (UserDataManager.Instance == null || UserDataManager.Instance.Coin < cost)
            return;

         MainTrain mainTrain = TrainManager.Instance != null ? TrainManager.Instance.MainTrain : null;

         if (mainTrain == null)
            return;

         int beforeCoin = UserDataManager.Instance.Coin;
         GameEventSystem.Publish(new ChangeCoinUIEvent(beforeCoin, beforeCoin - cost));

         SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_ItemBuy, ignoreSuppress: true);

         mainTrain.EmergencyRepair(aliveHealRatio, revivedHpRatio);

         SetUp();
      }

      private void _OnChangeCoin(ChangeCoinUIEvent changeCoinEvent)
      {
         // 구독 순서에 의존하지 않도록 이벤트의 AfterCoin을 직접 기준으로 사용한다.
         _ApplyPriceColor(changeCoinEvent.AfterCoin);
      }

      // 현재 보유 코인이 수리 비용보다 적으면 가격 텍스트를 빨간색으로, 충분하면 원래 색으로 표시한다.
      private void _UpdatePriceColor()
      {
         int coin = UserDataManager.Instance != null ? UserDataManager.Instance.Coin : 0;
         _ApplyPriceColor(coin);
      }

      private void _ApplyPriceColor(int coin)
      {
         needMoneyText.color = coin >= GetCurrentCost() ? _priceOriginalColor : insufficientColor;
      }
   }
}
