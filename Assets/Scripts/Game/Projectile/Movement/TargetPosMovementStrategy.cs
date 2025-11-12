using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 타겟 위치로 이동하는 전략
    /// </summary>
    public class TargetPosMovementStrategy : IMovementStrategy
    {
        private Vector3 _targetPosition;
        
        public void Initialize(Projectile projectile, ProjectileData data, IProjectileTarget target)
        {
            if (target != null && target.TargetTransform != null)
            {
                _targetPosition = target.TargetTransform.position;
            }
            else
            {
                _targetPosition = projectile.transform.position;
            }
        }
        
        public void UpdateMovement(Projectile projectile, float deltaTime)
        {
            projectile.transform.position = _targetPosition;
        }
        
        public bool ShouldImpact(Projectile projectile)
        {
            return false;
        }
    }
}