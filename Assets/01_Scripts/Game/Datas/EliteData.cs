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
               hpMultiplier = 120f,
               damageMultiplier = 4f,
               moveSpeedMultiplier = 1.4f,
               sizeScale = 1.6f,
               dropExpMultiplier = 10f,
               dropMoneyMultiplier = 10f,
               ccResistance = 0.5f,
               tintColor = new Color(1f, 0.3f, 0.3f, 1f),
            },
            // 🟢 초록 — 엄청 튼튼. 느려서 노출이 길고 CC 완전 면역이라 HP 로 탱크 정체성을 담당한다.
            new EliteVariant
            {
               type = EliteType.Green,
               weight = 1f,
               hpMultiplier = 250f,
               damageMultiplier = 1.5f,
               moveSpeedMultiplier = 0.85f,
               sizeScale = 1.8f,
               dropExpMultiplier = 10f,
               dropMoneyMultiplier = 10f,
               ccResistance = 1f,
               tintColor = new Color(0.3f, 1f, 0.4f, 1f),
            },
            // 🔵 파랑 — 주기적 원거리 공격. 접근 전부터 기차를 깎는 대신 몸이 가장 물렁하다. (weight 0 = 미등장)
            new EliteVariant
            {
               type = EliteType.Blue,
               weight = 0f,
               hpMultiplier = 70f,
               damageMultiplier = 2f,
               moveSpeedMultiplier = 1f,
               sizeScale = 1.5f,
               dropExpMultiplier = 10f,
               dropMoneyMultiplier = 10f,
               ccResistance = 0.25f,
               tintColor = new Color(0.3f, 0.6f, 1f, 1f),
               rangedAttackInterval = 3f,
            },
            // 🟡 노랑 — 주변 이속 증가 오라. 본체보다 주변 물량 가속이 위협인 세력 배가형. (weight 0 = 미등장)
            new EliteVariant
            {
               type = EliteType.Yellow,
               weight = 0f,
               hpMultiplier = 80f,
               damageMultiplier = 1.5f,
               moveSpeedMultiplier = 1.1f,
               sizeScale = 1.5f,
               dropExpMultiplier = 10f,
               dropMoneyMultiplier = 10f,
               ccResistance = 0.25f,
               tintColor = new Color(1f, 0.95f, 0.3f, 1f),
               auraRadius = 4f,
               auraSpeedMultiplier = 1.4f,
               auraInterval = 1f,
            },
         };
      }
   }
}
