using Cumic.Events;
using TMPro;
using TrainDefense.Game.Events;
using UnityEngine;

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

            if (UserDataManager.Instance != null)
            {
                coinText.text = $"Coin : {UserDataManager.Instance.Money}";
            }
            else
            {
                coinText.text = $"Coin : 0";
            }
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<MonsterDeadEvent>(OnAddCoin);
        }

        private void OnAddCoin(MonsterDeadEvent monsterDeadEvent)
        {
            if (UserDataManager.Instance == null) return;
            
            coinText.text = $"Coin : {UserDataManager.Instance.Money}";
        }
    }
}