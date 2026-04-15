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

            var buffs = trainSkillData.Buffs;
            if (buffs == null || buffs.Count == 0)
                return false;

            for (int i = 0; i < buffs.Count; i++)
            {
                var entry = buffs[i];
                owner.ApplyTimedStat(entry.StatType, entry.Percent, trainSkillData.BuffDuration);
            }
            return true;
        }
    }
}
