using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 긴급 수리: 부서진(죽은) 기차를 모두 부활시키고, 살아있는 기차를 일정 비율만큼 회복한다.
    /// </summary>
    [Serializable]
    public class EmergencyRepairChoice : RewardChoiceBase
    {
        [SerializeField]
        [Tooltip("살아있던 기차의 회복 비율(0~1). 0.1 = 최대 체력의 10% 회복")]
        private float aliveHealRatio = 0.1f;

        [SerializeField]
        [Tooltip("부활시킨 기차의 부활 직후 HP 비율(0~1). 0.1 = 최대 체력의 10%")]
        private float revivedHpRatio = 0.1f;

        public float AliveHealRatio => aliveHealRatio;
        public float RevivedHpRatio => revivedHpRatio;

        public override void Execute()
        {
            var main = TrainManager.Instance != null ? TrainManager.Instance.MainTrain : null;

            if (main == null)
            {
                Debug.LogError($"EmergencyRepairChoice [{Id}]: MainTrain is null");
                return;
            }

            main.EmergencyRepair(aliveHealRatio, revivedHpRatio);
        }
    }
}
