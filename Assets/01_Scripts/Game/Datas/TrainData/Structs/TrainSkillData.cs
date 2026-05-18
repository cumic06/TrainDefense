using System;
using System.Collections.Generic;
using UnityEngine;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public struct TrainSkillBuffEntry
    {
        [SerializeField]
        private StatType statType;
        [SerializeField]
        private float percent;

        public StatType StatType => statType;
        public float Percent => percent;

        public TrainSkillBuffEntry(StatType statType, float percent)
        {
            this.statType = statType;
            this.percent = percent;
        }
    }

    [Serializable]
    public class TrainSkillData : IData
    {
        [SerializeField]
        private string id;
        [SerializeField]
        private string name;
        [SerializeField]
        private string description;
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
        private List<TrainSkillBuffEntry> buffs = new();

        public string Id => id;
        public string Name => TrainDefense.Localize.LocalizeHelper.GetByKey(name, name);
        public string Description => TrainDefense.Localize.LocalizeHelper.GetByKey(description, description);
        public bool HasActiveSkill => skillType != TrainSkillType.None;
        public TrainSkillType SkillType => skillType;
        public float SkillCooldown => skillCooldown;
        public string SkillIconId => skillIconId;
        public TrainSkillProjectileData ProjectileData => projectileData ??= new TrainSkillProjectileData();
        public float BuffDuration => buffDuration;
        public IReadOnlyList<TrainSkillBuffEntry> Buffs => buffs ??= new List<TrainSkillBuffEntry>();
    }
}
