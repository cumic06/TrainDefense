using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
   [System.Serializable]
   public class EliteData
   {
      [Header("엘리트 스폰 밀도")]
      [Tooltip("스폰 웨이브(틱) n회 분량의 시간당 엘리트 1마리. 웨이브당 마릿수(spawnCount)와 무관한 시간 기준 — 실제 몹 수로는 n×spawnCount마리당 1마리. 역(구간)마다 예산 = 구간시간 ÷ (n × 스폰간격)을 적립해 정수부만큼 구간에 균등 배치하고, 소수 잔여분은 다음 구간으로 이월한다. 절반으로 줄이면 엘리트가 2배 자주 나온다.")]
      [Min(1)]
      public int eliteSpawnCycle = 100;

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
               hpMultiplier = 100f,
               damageMultiplier = 4f,
               moveSpeedMultiplier = 1.4f,
               sizeScale = 1.6f,
               dropExpMultiplier = 10f,
               dropMoneyMultiplier = 10f,
               ccResistance = 0.5f,
               tintColor = new Color(1f, 0.3f, 0.3f, 1f),
            },
            // 🟢 초록 — 엄청 튼튼 (weight 0 = 미등장, 빨강만 사용)
            new EliteVariant
            {
               type = EliteType.Green,
               weight = 0f,
               hpMultiplier = 8f,
               damageMultiplier = 1.5f,
               moveSpeedMultiplier = 0.85f,
               sizeScale = 1.7f,
               dropExpMultiplier = 3f,
               dropMoneyMultiplier = 3f,
               ccResistance = 1f,
               tintColor = new Color(0.3f, 1f, 0.4f, 1f),
            },
            // 🔵 파랑 — 주기적 원거리 공격 (weight 0 = 미등장, 빨강만 사용)
            new EliteVariant
            {
               type = EliteType.Blue,
               weight = 0f,
               hpMultiplier = 3f,
               damageMultiplier = 2f,
               moveSpeedMultiplier = 1f,
               sizeScale = 1.5f,
               dropExpMultiplier = 2.5f,
               dropMoneyMultiplier = 2.5f,
               tintColor = new Color(0.3f, 0.6f, 1f, 1f),
               rangedAttackInterval = 3f,
            },
            // 🟡 노랑 — 주변 이속 증가 오라 (weight 0 = 미등장, 빨강만 사용)
            new EliteVariant
            {
               type = EliteType.Yellow,
               weight = 0f,
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
