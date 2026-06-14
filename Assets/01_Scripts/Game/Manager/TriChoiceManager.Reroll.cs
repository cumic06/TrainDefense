using UnityEngine;
using Cumic.Events;
using TrainDefense.Game.Events;

namespace TrainDefense.Game
{
    // 삼중택일 리롤 비용 계산·차감 로직. UI(TriChoiceUI)는 표시만 담당하고
    // 비용 상태/계산/코인 차감은 모두 이 매니저가 책임진다.
    public partial class TriChoiceManager
    {
        #region Fields
        [Header("Reroll")]
        [SerializeField]
        [Tooltip("삼중택일이 새로 열릴 때 적용되는 첫 리롤 비용")]
        private int baseRerollCost = 50;
        [SerializeField]
        [Tooltip("리롤할 때마다 현재 비용에 곱해지는 배수")]
        private float rerollCostMultiplier = 2f;
        #endregion

        #region Variables
        private int _currentRerollCost;
        #endregion

        public int CurrentRerollCost => _currentRerollCost;

        /// <summary>
        /// 삼중택일이 새로 열릴 때 호출. 리롤 비용을 기본값으로 초기화하고
        /// 직전 엘리트 기억도 비운다(리롤이 아니므로 제약 없이 시작).
        /// (리롤 시에는 호출하지 않으므로 비용·엘리트 기억이 유지됨)
        /// </summary>
        public void ResetRerollCost()
        {
            _currentRerollCost = baseRerollCost;
            _lastEliteChoiceIds.Clear();
        }

        /// <summary>
        /// 현재 리롤 비용만큼 코인이 충분한지 여부.
        /// </summary>
        public bool CanReroll()
        {
            var userDataManager = UserDataManager.Instance;

            return userDataManager != null && userDataManager.Coin >= _currentRerollCost;
        }

        /// <summary>
        /// 리롤 비용을 차감하고 다음 비용을 배수만큼 증가시킨다. 성공하면 true.
        /// 코인이 부족하면 차감하지 않고 false를 반환한다.
        /// </summary>
        public bool TryReroll()
        {
            if (!CanReroll())
                return false;

            int beforeCoin = UserDataManager.Instance.Coin;
            int afterCoin = beforeCoin - _currentRerollCost;
            GameEventSystem.Publish(new ChangeCoinUIEvent(beforeCoin, afterCoin));

            _currentRerollCost = Mathf.Max(1, Mathf.RoundToInt(_currentRerollCost * rerollCostMultiplier));

            return true;
        }
    }
}
