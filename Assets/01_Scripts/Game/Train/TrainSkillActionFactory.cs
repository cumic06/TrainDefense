using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
    public static class TrainSkillActionFactory
    {
        public static TrainSkillAction Create(Train owner, TrainSkillData trainSkillData)
        {
            if (owner == null || trainSkillData == null || !trainSkillData.HasSkill)
                return null;

            TrainSkillAction action = trainSkillData.SkillType switch
            {
                TrainSkillType.Projectile => new TrainProjectileSkillAction(),
                _ => null
            };

            action?.Initialize(owner, trainSkillData);
            return action;
        }
    }
}
