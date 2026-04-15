namespace TrainDefense.Game
{
    public class TrainSelfBuffSkillAction : TrainSkillAction
    {
        protected override bool OnUse()
        {
            if (owner == null || trainSkillData == null)
                return false;

            if (trainSkillData.BuffDuration <= 0f)
                return false;

            owner.ApplyTimedStat(
                trainSkillData.BuffStatType,
                trainSkillData.BuffPercent,
                trainSkillData.BuffDuration);
            return true;
        }
    }
}
