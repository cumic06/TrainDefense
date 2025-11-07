using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 지연 후 타겟 위치에 낙하하는 전략
    /// </summary>
    public class DelayedDropMovementStrategy : IMovementStrategy
    {
        private float _delaySeconds;
        private float _elapsedTime;
        private Vector3 _targetPosition;
        private bool _hasImpacted;
        
        public void Initialize(Projectile projectile, ProjectileData data, IProjectileTarget target)
        {
            _delaySeconds = data.DelaySeconds;
            _elapsedTime = 0f;
            _hasImpacted = false;
            
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
            _elapsedTime += deltaTime;
            
            if (_elapsedTime >= _delaySeconds && !_hasImpacted)
            {
                projectile.transform.position = _targetPosition;
                _hasImpacted = true;
            }
        }
        
        public bool ShouldImpact(Projectile projectile)
        {
            return _elapsedTime >= _delaySeconds && !_hasImpacted;
        }
    }
}