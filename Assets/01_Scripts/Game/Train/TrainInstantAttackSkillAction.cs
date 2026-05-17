namespace TrainDefense.Game
{
    public class TrainInstantAttackSkillAction : TrainSkillAction
    {
        protected override bool OnUse()
        {
            if (owner is TurretTrain turret)
                return turret.ForceAttack();

            if (owner is RangeTrain rangeTrain)
            {
                var pd = trainSkillData?.ProjectileData;
                if (pd?.ProjectilePrefab == null || pd.Range <= 0f)
                    return false;
                rangeTrain.SpawnExternalProjectile(pd.ProjectilePrefab, pd.Range);
                return true;
            }

            return false;
        }
    }
}
