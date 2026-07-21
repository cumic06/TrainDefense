using UnityEngine;
using System;
using Sirenix.OdinInspector;

namespace TrainDefense.Game.Datas
{
   [Serializable]
   public class RangeTrainUpgradeData : ITrainUpgradeData, IDescribableData, IIconData
   {
      #region Fields
      [SerializeField]
      private string id;
      [SerializeField]
      private string iconId;
      [Header("UI Info")]
      [SerializeField]
      private Sprite icon;

      [SerializeField]
      private string name;

      [SerializeField]
      [TextArea(2, 4)]
      private string description;

      [SerializeField]
      [Tooltip("업그레이드 스탯 배열 (인덱스 = 레벨, 예: [0] = 레벨 0, [1] = 레벨 1)")]
      private RangeTrainUpgradeStats[] upgradeStats;
      #endregion

      #region IData
      public string Id => id;
      #endregion

      #region IDescribableData
      public string Name => TrainDefense.Localize.LocalizeHelper.GetByKey(name, name);
      public string Description => TrainDefense.Localize.LocalizeHelper.GetByKey(description, description);
      #endregion

      #region IIconData
      public string IconId => iconId;
      [ShowInInspector, ReadOnly]
      public Sprite Icon
      {
         get
         {
            if (icon == null && !string.IsNullOrEmpty(iconId))
            {
               icon = Resources.Load<Sprite>($"Sprite/{iconId}");
               if (icon == null)
               {
                  Debug.LogWarning($"RangeTrainUpgradeData [{id}]: Icon not found at 'Sprite/{iconId}'");
               }
            }
            return icon;
         }
      }
      #endregion

      #region ITrainUpgradeData
      public string UpgradeName => Name;
      public TrainStatusData GetStatusUpgrade(int level)
      {
         var stats = GetStatsForLevel(level);
         return stats?.StatusUpgrade ?? default;
      }
      [ShowInInspector, ReadOnly]
      public int MaxLevel => upgradeStats?.Length ?? 0;
      #endregion

      // 런타임 단일 스탯 강화(상점 개별 강화)용: 모든 레벨 인덱스에 동일 델타를 채워
      // 기존 Upgrade(레벨 인덱스) 경로를 그대로 재사용한다. DB 직렬화와 무관.
      public static RangeTrainUpgradeData CreateRuntimeSingleStat(string id, string name, RangeTrainStatus delta, int levelCount)
      {
         var data = new RangeTrainUpgradeData
         {
            id = id,
            name = name,
            upgradeStats = new RangeTrainUpgradeStats[levelCount],
         };

         for (int i = 0; i < levelCount; i++)
            data.upgradeStats[i] = new RangeTrainUpgradeStats(delta);

         return data;
      }

      public RangeTrainStatus GetRangeStatusUpgrade(int level)
      {
         var stats = GetStatsForLevel(level);
         return stats?.RangeStatusUpgrade ?? default;
      }

      // 적용된 업그레이드 증가량의 합 (base 미포함). 레벨 = 받은 업그레이드 횟수 → 인덱스 0..level-1.
      public RangeTrainStatus GetAccumulatedRangeStatusUpgrade(int level)
      {
         var total = new RangeTrainStatus();
         for (int i = 0; i < level; i++)
         {
            var s = GetRangeStatusUpgrade(i);
            total.AttackDamage += s.AttackDamage;
            total.AttackRange += s.AttackRange;
            total.AttackArea += s.AttackArea;
            total.AttackCount += s.AttackCount;
            total.AttackInterval += s.AttackInterval;
            total.CriticalChance += s.CriticalChance;
            total.CriticalDamage += s.CriticalDamage;
            total.SlowRate += s.SlowRate;
            total.BurstDuration += s.BurstDuration;
         }
         return total;
      }

      private RangeTrainUpgradeStats GetStatsForLevel(int targetLevel)
      {
         if (upgradeStats == null || upgradeStats.Length == 0)
            return null;
         if (targetLevel < 0 || targetLevel >= upgradeStats.Length)
            return null;
         return upgradeStats[targetLevel];
      }
   }
}
