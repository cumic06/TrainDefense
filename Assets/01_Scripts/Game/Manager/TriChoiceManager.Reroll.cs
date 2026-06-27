using UnityEngine;
using Cumic.Events;
using TrainDefense.Game.Datas;
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
        // 남은 무료 리롤 횟수(영구 업글). 트라이초이스가 열릴 때 충전된다.
        private int _freeRerollCount;
        #endregion

        // 무료 리롤이 남아있으면 0(무료)으로 표시한다.
        public int CurrentRerollCost => _freeRerollCount > 0 ? 0 : _currentRerollCost;

        /// <summary>
        /// 삼중택일이 새로 열릴 때 호출. 리롤 비용을 기본값으로 초기화하고
        /// 직전 엘리트 기억도 비운다(리롤이 아니므로 제약 없이 시작).
        /// (리롤 시에는 호출하지 않으므로 비용·엘리트 기억이 유지됨)
        /// </summary>
        public void ResetRerollCost()
        {
            _currentRerollCost = baseRerollCost;
            _lastEliteChoiceIds.Clear();

            // 영구 업그레이드: 레벨업(트라이초이스 오픈)마다 무료 리롤 횟수 충전
            var permanentUpgradeManager = PermanentUpgradeManager.Instance;
            _freeRerollCount = permanentUpgradeManager != null
                ? Mathf.RoundToInt(permanentUpgradeManager.GetValue(PermanentUpgradeType.FreeReroll))
                : 0;
        }

        /// <summary>
        /// 현재 리롤 비용만큼 코인이 충분한지 여부.
        /// </summary>
        public bool CanReroll()
        {
            if (_freeRerollCount > 0)
                return true;

            var userDataManager = UserDataManager.Instance;

            return userDataManager != null && userDataManager.Coin >= _currentRerollCost;
        }

        /// <summary>
        /// 리롤 비용을 차감하고 다음 비용을 배수만큼 증가시킨다. 성공하면 true.
        /// 코인이 부족하면 차감하지 않고 false를 반환한다.
        /// </summary>
        public bool TryReroll()
        {
            // 무료 리롤이 남아있으면 코인 차감 없이 소비
            if (_freeRerollCount > 0)
            {
                _freeRerollCount--;
                GameEventSystem.Publish(new RerollEvent(0, true));

                return true;
            }

            if (!CanReroll())
                return false;

            int beforeCoin = UserDataManager.Instance.Coin;
            int paidCost = _currentRerollCost;
            int afterCoin = beforeCoin - paidCost;
            GameEventSystem.Publish(new ChangeCoinUIEvent(beforeCoin, afterCoin));

            _currentRerollCost = Mathf.Max(1, Mathf.RoundToInt(_currentRerollCost * rerollCostMultiplier));

            GameEventSystem.Publish(new RerollEvent(paidCost, false));

            return true;
        }
    }
}
