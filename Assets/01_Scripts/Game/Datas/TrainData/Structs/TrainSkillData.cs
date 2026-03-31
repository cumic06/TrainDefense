using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class TrainSkillData
    {
        [SerializeField]
        private TrainSkillType skillType;
        [SerializeField]
        private float skillCooldown;
        [SerializeField]
        private string skillIconId;
        [SerializeField]
        private TrainSkillProjectileData projectileData;

        public bool HasSkill => skillType != TrainSkillType.None;
        public TrainSkillType SkillType => skillType;
        public float SkillCooldown => skillCooldown;
        public string SkillIconId => skillIconId;
        public TrainSkillProjectileData ProjectileData => projectileData ??= new TrainSkillProjectileData();
    }
}
