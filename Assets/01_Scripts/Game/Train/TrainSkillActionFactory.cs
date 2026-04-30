using System;
using System.Collections.Generic;
using TrainDefense.Game.Datas;
using UnityEngine;

namespace TrainDefense.Game
{
    public static class TrainSkillActionFactory
    {
        private static readonly Dictionary<TrainSkillType, Func<TrainSkillAction>> _creators = new()
        {
            { TrainSkillType.Projectile,    () => new TrainProjectileSkillAction() },
            { TrainSkillType.SelfBuff,      () => new TrainSelfBuffSkillAction() },
            { TrainSkillType.InstantAttack, () => new TrainInstantAttackSkillAction() },
        };

        public static void Register(TrainSkillType type, Func<TrainSkillAction> creator)
            => _creators[type] = creator;

        public static TrainSkillAction Create(Train owner, TrainSkillData trainSkillData)
        {
            if (owner == null || trainSkillData == null || !trainSkillData.HasActiveSkill)
                return null;

            if (!_creators.TryGetValue(trainSkillData.SkillType, out var creator))
            {
                Debug.LogWarning($"TrainSkillActionFactory: unregistered SkillType '{trainSkillData.SkillType}'");
                return null;
            }

            var action = creator();
            action?.Initialize(owner, trainSkillData);
            return action;
        }
    }
}
