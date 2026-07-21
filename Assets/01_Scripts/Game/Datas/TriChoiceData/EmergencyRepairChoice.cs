using System;
using Cumic.Events;
using TrainDefense.Game.Events;
using TrainDefense.Game.Manager;
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

        // 레벨업 카드 풀에 상시 후보로 들어가므로, 수리 대상(죽은 기차·손상 기차)이 있을 때만 노출한다.
        public override bool IsValid()
        {
            var main = TrainManager.Instance != null ? TrainManager.Instance.MainTrain : null;

            return main != null && main.HasRepairTarget;
        }

        public override void Execute()
        {
            var main = TrainManager.Instance != null ? TrainManager.Instance.MainTrain : null;

            if (main == null)
            {
                Debug.LogError($"EmergencyRepairChoice [{Id}]: MainTrain is null");
                return;
            }

            main.EmergencyRepair(aliveHealRatio, revivedHpRatio);

            // 수리 분석(train_repair) 연속성 유지 — 상점 수리 슬롯 제거 후 유일한 수리 경로. 보상 카드라 비용 0.
            int stationCount = StageManager.Instance != null ? StageManager.Instance.TotalStationPassedCount : 0;
            GameEventSystem.Publish(new TrainRepairedEvent(0, stationCount));
        }
    }
}
