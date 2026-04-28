using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
    public static class TrainSkillActionFactory
    {
        public static TrainSkillAction Create(Train owner, TrainSkillData trainSkillData)
        {
            if (owner == null || trainSkillData == null || !trainSkillData.HasActiveSkill)
                return null;

            TrainSkillAction action = trainSkillData.SkillType switch
            {
                TrainSkillType.Projectile => new TrainProjectileSkillAction(),
                TrainSkillType.SelfBuff => new TrainSelfBuffSkillAction(),
                TrainSkillType.InstantAttack => new TrainInstantAttackSkillAction(),
                _ => null
            };

            action?.Initialize(owner, trainSkillData);
            return action;
        }
    }
}
