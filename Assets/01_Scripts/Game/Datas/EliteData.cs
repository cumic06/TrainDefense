using System.Collections.Generic;
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

      [Header("엘리트 스탯 배율 (variants 가 비었을 때 빨강 fallback 으로 사용)")]
      public float hpMultiplier = 3f;
      public float damageMultiplier = 2f;
      public float moveSpeedMultiplier = 1f;
      public float sizeScale = 1.5f;
      public float dropExpMultiplier = 2f;
      public float dropMoneyMultiplier = 2f;

      [Header("엘리트 타입별 변형 (빨강/초록/파랑/노랑)")]
      [Tooltip("비워두면 코드 기본 4종이 자동 사용된다. 인스펙터에서 채우면 그 목록을 쓴다.")]
      public List<EliteVariant> variants = new();

      private List<EliteVariant> _defaultVariants;

      // variants 가 비어있으면 코드 기본 4종을 생성해 돌려준다.
      public IReadOnlyList<EliteVariant> GetVariants()
      {
         if (variants != null && variants.Count > 0)
            return variants;

         return _defaultVariants ??= _CreateDefaultVariants();
      }

      // weight 가중치 기반으로 변형 1종을 선택한다. 후보가 없으면 null.
      public EliteVariant SelectVariant()
      {
         IReadOnlyList<EliteVariant> list = GetVariants();
         if (list == null || list.Count == 0)
            return null;

         float total = 0f;
         foreach (EliteVariant variant in list)
         {
            if (variant != null && variant.weight > 0f)
               total += variant.weight;
         }

         if (total <= 0f)
            return list[0];

         float point = Random.value * total;
         foreach (EliteVariant variant in list)
         {
            if (variant == null || variant.weight <= 0f)
               continue;

            if (point < variant.weight)
               return variant;

            point -= variant.weight;
         }

         return list[list.Count - 1];
      }

      private List<EliteVariant> _CreateDefaultVariants()
      {
         return new List<EliteVariant>
         {
            // 🔴 빨강 — 전체 스펙 증가
            new EliteVariant
            {
               type = EliteType.Red,
               weight = 1f,
               hpMultiplier = 3f,
               damageMultiplier = 2.5f,
               moveSpeedMultiplier = 1.2f,
               sizeScale = 1.5f,
               dropExpMultiplier = 2.5f,
               dropMoneyMultiplier = 2.5f,
               tintColor = new Color(1f, 0.3f, 0.3f, 1f),
            },
            // 🟢 초록 — 엄청 튼튼
            new EliteVariant
            {
               type = EliteType.Green,
               weight = 1f,
               hpMultiplier = 8f,
               damageMultiplier = 1.5f,
               moveSpeedMultiplier = 0.85f,
               sizeScale = 1.7f,
               dropExpMultiplier = 3f,
               dropMoneyMultiplier = 3f,
               tintColor = new Color(0.3f, 1f, 0.4f, 1f),
            },
            // 🔵 파랑 — 주기적 원거리 공격
            new EliteVariant
            {
               type = EliteType.Blue,
               weight = 1f,
               hpMultiplier = 3f,
               damageMultiplier = 2f,
               moveSpeedMultiplier = 1f,
               sizeScale = 1.5f,
               dropExpMultiplier = 2.5f,
               dropMoneyMultiplier = 2.5f,
               tintColor = new Color(0.3f, 0.6f, 1f, 1f),
               rangedAttackInterval = 3f,
            },
            // 🟡 노랑 — 주변 이속 증가 오라
            new EliteVariant
            {
               type = EliteType.Yellow,
               weight = 1f,
               hpMultiplier = 3f,
               damageMultiplier = 1.5f,
               moveSpeedMultiplier = 1.1f,
               sizeScale = 1.5f,
               dropExpMultiplier = 2.5f,
               dropMoneyMultiplier = 2.5f,
               tintColor = new Color(1f, 0.95f, 0.3f, 1f),
               auraRadius = 4f,
               auraSpeedMultiplier = 1.4f,
               auraInterval = 1f,
            },
         };
      }
   }
}
