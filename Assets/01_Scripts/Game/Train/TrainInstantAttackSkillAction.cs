namespace TrainDefense.Game
{
    public class TrainInstantAttackSkillAction : TrainSkillAction
    {
        protected override bool OnUse()
        {
            if (owner is TurretTrain turret)
            {
                return turret.ForceAttack();
            }
            return false;
        }
    }
}
