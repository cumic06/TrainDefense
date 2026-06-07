using UnityEngine;

namespace TrainDefense.Game.Datas
{
   [System.Serializable]
   public class EliteData
   {
      [Header("스폰 타이밍")]
      public float spawnInterval = 60f;
      [Range(0f, 100f)]
      public float spawnMaxChance = 30f;
      [Range(0f, 100f)]
      public float chanceGrowthPerInterval = 5f;

      [Header("엘리트 배율 램프 (한 판 내내 누적)")]
      [Tooltip("이 시간(초)마다 엘리트 배율 진행도가 증가. 0 이하면 램프 비활성(진행도가 시작값에 고정).")]
      public float multiplierRampInterval = 60f;
      [Tooltip("interval마다 증가하는 진행도(0~1). 진행도가 1이면 각 몬스터의 EliteChanceMultiplier가 100% 적용된다.")]
      public float multiplierGrowthPerInterval = 0.1f;
      [Range(0f, 1f)]
      [Tooltip("게임 시작 시 진행도. 0이면 초반에는 엘리트 배율이 0(엘리트 거의 없음)에서 시작해 점점 증가한다.")]
      public float multiplierStartProgress = 0f;

      [Header("엘리트 스탯 배율")]
      public float hpMultiplier = 3f;
      public float damageMultiplier = 2f;
      public float moveSpeedMultiplier = 1f;
      public float sizeScale = 1.5f;
      public float dropExpMultiplier = 2f;
      public float dropMoneyMultiplier = 2f;
   }
}
