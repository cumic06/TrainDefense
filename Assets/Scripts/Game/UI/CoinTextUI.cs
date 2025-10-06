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
            coinText.text = $"Coin : {UserDataManager.Instance.Money}";
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<MonsterDeadEvent>(OnAddCoin);
        }

        private void OnAddCoin(MonsterDeadEvent monsterDeadEvent)
        {
            coinText.text = $"Coin : {UserDataManager.Instance.Money}";
        }
    }
}