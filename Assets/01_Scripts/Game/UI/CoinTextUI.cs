using UnityEngine;
using TMPro;
using Cumic.Events;
using TrainDefense.Game.Events;

namespace TrainDefense.Game.UI
{
    public class CoinTextUI : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private TextMeshProUGUI coinText;
        #endregion

        private void Start()
        {
            GameEventSystem.Subscribe<IncreaseCoinEvent>(OnIncreaseCoin);
            GameEventSystem.Subscribe<DecreaseCoinEvent>(OnDecreaseCoin);
            Setup();
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<IncreaseCoinEvent>(OnIncreaseCoin);
            GameEventSystem.Unsubscribe<DecreaseCoinEvent>(OnDecreaseCoin);
        }

        private void Setup()
        {
            if (UserDataManager.Instance != null)
            {
                coinText.text = $"Coin : {UserDataManager.Instance.Coin}";
            }
            else
            {
                coinText.text = $"Coin : 0";
            }
        }

        private void OnIncreaseCoin(IncreaseCoinEvent increaseCoinEvent)
        {
            if (UserDataManager.Instance == null) return;

            coinText.text = $"Coin : {UserDataManager.Instance.Coin}";
        }

        private void OnDecreaseCoin(DecreaseCoinEvent decreaseCoinEvent)
        {
            if (UserDataManager.Instance == null) return;

            coinText.text = $"Coin : {UserDataManager.Instance.Coin}";
        }
    }
}