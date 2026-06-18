using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 엘리트 재화(영구 재화) 획득 선택지. PlayerPrefs에 영속 저장되는 메타 통화를 일정량 지급한다.
    /// </summary>
    [Serializable]
    public class EliteCurrencyRewardChoice : RewardChoiceBase
    {
        [SerializeField]
        [Tooltip("획득할 엘리트 재화량")]
        private int eliteCurrencyAmount = 3;

        public int EliteCurrencyAmount => eliteCurrencyAmount;

        public override void Execute()
        {
            var manager = PermanentUpgradeManager.Instance;

            if (manager == null)
            {
                Debug.LogError($"EliteCurrencyRewardChoice [{Id}]: PermanentUpgradeManager is null");
                return;
            }

            manager.AddCurrency(eliteCurrencyAmount);
        }
    }
}
