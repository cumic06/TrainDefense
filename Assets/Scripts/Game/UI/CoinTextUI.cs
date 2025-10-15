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
            GameEventSystem.Subscribe<MonsterDeadEvent>(OnAddCoin);
            Setup();
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<MonsterDeadEvent>(OnAddCoin);
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

        private void OnAddCoin(MonsterDeadEvent monsterDeadEvent)
        {
            if (UserDataManager.Instance == null) return;

            coinText.text = $"Coin : {UserDataManager.Instance.Coin}";
        }
    }
}