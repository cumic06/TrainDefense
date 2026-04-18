using UnityEngine;
using TMPro;
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

      private void Start()
      {
         GameEventSystem.Subscribe<ChangeCoinUIEvent>(OnChangeCoin);
         Setup();
      }

      private void OnDestroy()
      {
         GameEventSystem.Unsubscribe<ChangeCoinUIEvent>(OnChangeCoin);
      }

      private void Setup()
      {
         if (UserDataManager.Instance != null)
         {
            coinText.text = $"0";
            //coinText.text = $"Coin : {UserDataManager.Instance.Coin}";
         }
         else
         {
            coinText.text = $"0";
         }
      }

      private void OnChangeCoin(ChangeCoinUIEvent changeCoinEvent)
      {
         DOTween.To(() => changeCoinEvent.BeforeCoin, x => coinText.text = $"{x}", changeCoinEvent.AfterCoin, tweenDuration)
         .SetEase(Ease.InOutSine)
         .SetUpdate(true);
      }
   }
}