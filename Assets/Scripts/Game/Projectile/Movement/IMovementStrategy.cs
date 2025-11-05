namespace TrainDefense.Game
{
    /// <summary>
    /// 투사체 이동 전략 인터페이스
    /// </summary>
    public interface IMovementStrategy
    {
        void Initialize(Projectile projectile, ProjectileConfig config, IProjectileTarget target);
        void UpdateMovement(Projectile projectile, float deltaTime);
        bool ShouldImpact(Projectile projectile);
    }
}