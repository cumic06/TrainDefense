using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TrainDefense.Game.Datas
{
    [System.Serializable]
    public class RangeTrainData : TrainData, IAttackTrainData
    {
        #region Fields
        [SerializeField]
        private RangeTrainStatus rangeTrainStatus;
        [SerializeField]
        private string rangeProjectilePrefabId;
        private GameObject rangeProjectilePrefab;
        [SerializeField]
        private float attackDamageMultiplier = 1f;
        #endregion

        public RangeTrainStatus RangeTrainStatus => rangeTrainStatus;
        public float AttackDamageMultiplier => attackDamageMultiplier;

        // IAttackTrainData
        public GameObject ProjectilePrefab => RangeProjectilePrefab;

        private const string PROJECTILE_PREFAB_PATH = "Prefabs/Projectiles/TrainProjectile/";

        [ShowInInspector, ReadOnly]
        public GameObject RangeProjectilePrefab
        {
            get
            {
                if (rangeProjectilePrefab == null && !string.IsNullOrEmpty(rangeProjectilePrefabId))
                {
                    rangeProjectilePrefab = Resources.Load<GameObject>($"{PROJECTILE_PREFAB_PATH}{rangeProjectilePrefabId}");
                    if (rangeProjectilePrefab == null)
                    {
                        Debug.LogWarning($"RangeTrainData [{Id}]: Projectile Prefab not found at 'Prefabs/{rangeProjectilePrefabId}'");
                    }
                }
                return rangeProjectilePrefab;
            }
        }

        [Obsolete("Use RangeProjectilePrefab property instead")]
        public Projectile RangeProjectilePrefabComponent => RangeProjectilePrefab?.GetComponent<Projectile>();

        public override IEnumerable<TrainStatLine> GetStatLines()
        {
            var s = rangeTrainStatus;
            yield return new TrainStatLine("Stat_AttackDamage", s.AttackDamage);
            yield return new TrainStatLine("Stat_AttackSpeed", TrainStatLine.ToAttackSpeed(s.AttackInterval));
            yield return new TrainStatLine("Stat_AttackArea", s.AttackArea);
            if (s.SlowRate > 0f)
                yield return new TrainStatLine("Stat_Slow", s.SlowRate);
        }
    }
}