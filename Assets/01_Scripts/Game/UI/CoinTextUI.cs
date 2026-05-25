using UnityEngine;
using TMPro;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Events;
using DG.Tweening;

namespace TrainDefense.Game.UI
{
   public class CoinTextUI : MonoBehaviour
   {
      #region Field
      [SerializeField]
      private TextMeshProUGUI coinText;
      [SerializeField]
      private float tweenDuration = 1f;
      #endregion

      private void OnEnable()
      {
         if (coinText != null && UserDataManager.Instance != null)
         {
            coinText.text = UserDataManager.Instance.Coin.ToCommaString();
         }
      }

      private void Start()
      {
         GameEventSystem.Subscribe<ChangeCoinUIEvent>(OnChangeCoin);
      }

      private void OnDestroy()
      {
         GameEventSystem.Unsubscribe<ChangeCoinUIEvent>(OnChangeCoin);
      }

      private void OnChangeCoin(ChangeCoinUIEvent changeCoinEvent)
      {
         DOTween.To(() => changeCoinEvent.BeforeCoin, x => coinText.text = x.ToCommaString(), changeCoinEvent.AfterCoin, tweenDuration)
         .SetEase(Ease.InOutSine)
         .SetUpdate(true);
      }
   }
}