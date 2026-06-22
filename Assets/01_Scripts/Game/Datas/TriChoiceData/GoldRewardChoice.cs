using System;
using UnityEngine;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Events;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 골드(코인) 획득 선택지. 세션 통화인 골드를 일정량 지급한다.
    /// </summary>
    [Serializable]
    public class GoldRewardChoice : RewardChoiceBase
    {
        [SerializeField]
        [Tooltip("획득할 골드(코인)량")]
        private int goldAmount = 100;

        public int GoldAmount => goldAmount;

        public override void Execute()
        {
            var userData = UserDataManager.Instance;

            if (userData == null)
            {
                Debug.LogError($"GoldRewardChoice [{Id}]: UserDataManager is null");
                return;
            }

            int before = userData.Coin;
            GameEventSystem.Publish(new ChangeCoinUIEvent(before, before + goldAmount));
        }
    }
}
