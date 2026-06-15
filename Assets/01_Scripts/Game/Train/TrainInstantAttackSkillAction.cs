namespace TrainDefense.Game
{
    public class TrainInstantAttackSkillAction : TrainSkillAction
    {
        protected override bool OnUse()
        {
            if (owner is IForceAttacker forceAttacker)
                return forceAttacker.ForceAttack();

            return false;
        }
    }
}
