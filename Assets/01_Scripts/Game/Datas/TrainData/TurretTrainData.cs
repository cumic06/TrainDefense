using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class TurretTrainData : TrainData, IAttackTrainData
    {
        #region Fields
        [SerializeField]
        private TurretTrainStatus turretTrainStatus;
        [SerializeField]
        private string turretProjectilePrefabId;
        private GameObject turretProjectilePrefab;
        [SerializeField]
        private float attackDamageMultiplier = 1f;
        #endregion

        public TurretTrainStatus TurretTrainStatus => turretTrainStatus;
        public float AttackDamageMultiplier => attackDamageMultiplier;

        // IAttackTrainData
        public GameObject ProjectilePrefab => TurretProjectilePrefab;

        private const string PROJECTILE_PREFAB_PATH = "Prefabs/Projectiles/TrainProjectile/";

        [ShowInInspector, ReadOnly]
        public GameObject TurretProjectilePrefab
        {
            get
            {
                if (turretProjectilePrefab == null && !string.IsNullOrEmpty(turretProjectilePrefabId))
                {
                    turretProjectilePrefab = Resources.Load<GameObject>($"{PROJECTILE_PREFAB_PATH}{turretProjectilePrefabId}");
                    if (turretProjectilePrefab == null)
                    {
                        Debug.LogWarning($"TurretTrainData [{Id}]: Projectile Prefab not found at '{PROJECTILE_PREFAB_PATH}{turretProjectilePrefabId}'");
                    }
                }
                return turretProjectilePrefab;
            }
        }

        [Obsolete("Use TurretProjectilePrefab property instead")]
        public Projectile TurretProjectilePrefabComponent => TurretProjectilePrefab?.GetComponent<Projectile>();

        // 이 포탑이 AttackArea 스탯을 실제 폭발 반경으로 쓰는지 판정 (빔 길이·파티클 비율은 제외).
        public bool UsesAttackArea
        {
            get
            {
                var prefab = TurretProjectilePrefab;
                if (prefab == null) return false;
                if (!prefab.TryGetComponent<Projectile>(out var projectile)) return false;
                var data = projectile.GetData();
                return data != null && data.ScaleByArea && data.ScaleRangeType == ScaleByRangeType.Area && data.IsSpawnTriggerHandle;
            }
        }

        public override IEnumerable<TrainStatLine> GetStatLines()
        {
            var s = turretTrainStatus;
            yield return new TrainStatLine("Stat_AttackDamage", s.AttackDamage);
            yield return new TrainStatLine("Stat_AttackSpeed", TrainStatLine.ToAttackSpeed(s.AttackInterval));
            yield return new TrainStatLine("Stat_AttackRange", s.AttackRange);
            if (s.AttackArea > 0f && UsesAttackArea)
                yield return new TrainStatLine("Stat_AttackArea", s.AttackArea);
            if (s.TargetCount > 1)
                yield return new TrainStatLine("Stat_TargetCount", s.TargetCount);
        }
    }
}