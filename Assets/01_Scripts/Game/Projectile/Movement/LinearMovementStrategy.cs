using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 직선 이동 전략
    /// </summary>
    public class LinearMovementStrategy : IMovementStrategy
    {
        private float _speed;
        
        public void Initialize(Projectile projectile, ProjectileData data, IProjectileTarget target)
        {
            _speed = data.Speed;
        }
        
        public void UpdateMovement(Projectile projectile, float deltaTime)
        {
            if (_speed <= 0) return;
            
            projectile.transform.Translate(Vector3.right * deltaTime * _speed);
        }
        
        public bool ShouldImpact(Projectile projectile)
        {
            return false; // 충돌 감지로 처리
        }
    }
}