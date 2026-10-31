using System;
using UnityEngine;

namespace TrainDefense.Game.Difficulty
{
    /// <summary>
    /// 난이도 단계별 수치 표. 1단계는 씬·프리팹의 기본값 그대로이고(배율 1·가산 0),
    /// 위 단계는 판 시작은 같게 두고 역이 지날수록 벌어지도록 성장률 쪽만 올린다(10-29 봇 18판 결정, 10-30 확정).
    ///
    /// 기본값은 씬 값 기준으로 체력 성장 0.04/0.048/0.056, 스폰 가속 3/4/5%(최대 60/70/80%), 수리 단가 1.0/1.15/1.3이 된다.
    /// Resources/Data/DifficultyStageConfig 에셋이 없으면 이 기본값으로 동작한다.
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyStageConfig", menuName = "Data/Difficulty/DifficultyStageConfig")]
    public class DifficultyStageConfig : ScriptableObject
    {
        [Serializable]
        public class StageValues
        {
            [Tooltip("StageManager.hpScale(역마다 몬스터 체력 증가율)에 곱하는 배율")]
            public float hpScaleMultiplier = 1f;

            [Tooltip("MonsterSpawner.stationSpawnAccelPercent(역마다 스폰 간격 단축 %)에 더하는 %p")]
            public float stationSpawnAccelPercentBonus;

            [Tooltip("MonsterSpawner.maxSpawnAccelPercent(스폰 간격 단축 상한 %)에 더하는 %p")]
            public float maxSpawnAccelPercentBonus;

            [Tooltip("ShopOfferPricing.repairCostPerHp(잃은 체력 1당 수리 코인)에 곱하는 배율")]
            public float repairCostMultiplier = 1f;
        }

        [Tooltip("인덱스 0 = 1단계. 단계 수가 이 배열 길이다")]
        [SerializeField]
        private StageValues[] stages =
        {
            new StageValues { hpScaleMultiplier = 1f, stationSpawnAccelPercentBonus = 0f, maxSpawnAccelPercentBonus = 0f, repairCostMultiplier = 1f },
            new StageValues { hpScaleMultiplier = 1.2f, stationSpawnAccelPercentBonus = 1f, maxSpawnAccelPercentBonus = 10f, repairCostMultiplier = 1.15f },
            new StageValues { hpScaleMultiplier = 1.4f, stationSpawnAccelPercentBonus = 2f, maxSpawnAccelPercentBonus = 20f, repairCostMultiplier = 1.3f },
        };

        private static readonly StageValues DefaultValues = new();

        public int StageCount => stages != null ? stages.Length : 0;

        /// <summary>단계(1부터)의 값. 표 밖이면 가장 가까운 단계로 고정한다.</summary>
        public StageValues GetValues(int stage)
        {
            if (stages == null || stages.Length == 0)
                return DefaultValues;

            int index = Mathf.Clamp(stage - DifficultyStageSelection.MIN_STAGE, 0, stages.Length - 1);

            return stages[index] ?? DefaultValues;
        }
    }
}
