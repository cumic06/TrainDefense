using UnityEngine;

namespace TrainDefense.Game.Datas
{
   [System.Serializable]
   public class EliteData
   {
      [Header("스폰 타이밍")]
      public float spawnInterval = 60f;
      [Range(0f, 100f)]
      public float spawnChance = 30f;

      [Header("엘리트 스탯 배율")]
      public float hpMultiplier = 3f;
      public float damageMultiplier = 2f;
      public float moveSpeedMultiplier = 1f;
      public float sizeScale = 1.5f;
      public float dropExpMultiplier = 2f;
      public float dropMoneyMultiplier = 2f;
   }
}
