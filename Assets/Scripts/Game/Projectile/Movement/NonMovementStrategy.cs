namespace TrainDefense.Game
{
    /// <summary>
    /// 이동하지 않는 전략 (고정 위치)
    /// </summary>
    public class NonMovementStrategy : IMovementStrategy
    {
        public void Initialize(Projectile projectile, ProjectileConfig config, IProjectileTarget target)
        {
            // 이동하지 않으므로 초기화 작업 없음
        }
        
        public void UpdateMovement(Projectile projectile, float deltaTime)
        {
            // 이동하지 않음
        }
        
        public bool ShouldImpact(Projectile projectile)
        {
            return false; // 충돌 감지로 처리
        }
    }
}