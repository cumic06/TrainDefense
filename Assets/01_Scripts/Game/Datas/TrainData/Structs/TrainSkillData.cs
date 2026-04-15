using System;
using UnityEngine;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class TrainSkillData : IData
    {
        [SerializeField]
        private string id;
        [SerializeField]
        private TrainSkillType skillType;
        [SerializeField]
        private float skillCooldown;
        [SerializeField]
        private string skillIconId;
        [SerializeField]
        private TrainSkillProjectileData projectileData;

        [SerializeField]
        private float buffDuration;
        [SerializeField]
        private StatType buffStatType;
        [SerializeField]
        private float buffPercent;

        public string Id => id;
        public bool HasSkill => skillType != TrainSkillType.None;
        public TrainSkillType SkillType => skillType;
        public float SkillCooldown => skillCooldown;
        public string SkillIconId => skillIconId;
        public TrainSkillProjectileData ProjectileData => projectileData ??= new TrainSkillProjectileData();
        public float BuffDuration => buffDuration;
        public StatType BuffStatType => buffStatType;
        public float BuffPercent => buffPercent;
    }
}
