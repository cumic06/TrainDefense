namespace TrainDefense.Game
{
    public class TrainInstantAttackSkillAction : TrainSkillAction
    {
        protected override bool OnUse()
        {
            if (owner is TurretTrain turret)
                return turret.ForceAttack();

            if (owner is RangeTrain rangeTrain)
                return rangeTrain.ForceAttack();

            return false;
        }
    }
}
