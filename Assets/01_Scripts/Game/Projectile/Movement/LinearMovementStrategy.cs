using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 직선 이동 전략
    /// </summary>
    public class LinearMovementStrategy : IMovementStrategy
    {
        private float _speed;
        // DetonateAtTargetDistance가 켜진 포탄만 쓰는 값. 발사 시점의 목표까지 거리를 재 두고,
        // 그만큼 날아가면 ShouldImpact로 그 자리에서 터뜨린다. (포격 포탑 = 쏘아 보내는 곡사 포탄)
        private bool _detonateAtDistance;
        private float _targetDistance;
        private float _traveled;
        // 곡사 표현용. 비행 진행도에 따라 model만 화면 위로 띄웠다 내린다(충돌 판정은 직선 그대로).
        private float _lobPeakHeight;

        public void Initialize(Projectile projectile, ProjectileData data, IProjectileTarget target)
        {
            _speed = data.Speed;
            _traveled = 0f;
            _targetDistance = 0f;
            _detonateAtDistance = false;

            // 풀에서 재사용되므로 지난 발사의 높이를 지우고 시작한다.
            if (_lobPeakHeight > 0f) projectile.SetLobHeight(0f);
            _lobPeakHeight = 0f;

            if (!data.DetonateAtTargetDistance || target == null || target.TargetTransform == null)
                return;

            _targetDistance = Vector2.Distance(projectile.transform.position, target.TargetTransform.position);
            // 발사 지점과 목표가 사실상 겹치면 즉시 터져 버리므로 거리 조건을 걸지 않는다.
            _detonateAtDistance = _targetDistance > 0.01f;

            // 비행 시간을 고정하면 거리에 따라 속도를 맞춘다. 가까운 적이든 먼 적이든 같은 시간에 떨어진다.
            if (_detonateAtDistance && data.FixedFlightDuration > 0f)
                _speed = _targetDistance / data.FixedFlightDuration;

            if (_detonateAtDistance)
                _lobPeakHeight = data.LobPeakHeight;
        }

        public void UpdateMovement(Projectile projectile, float deltaTime)
        {
            if (_speed <= 0) return;

            float step = deltaTime * _speed;
            projectile.transform.Translate(Vector3.right * step);

            if (!_detonateAtDistance) return;

            _traveled += step;

            if (_lobPeakHeight <= 0f) return;

            // 발사 지점 0 → 중간에서 최고 → 착탄 지점 0인 포물선. 4·h·t·(1-t)의 정점이 정확히 h다.
            float progress = Mathf.Clamp01(_traveled / _targetDistance);
            projectile.SetLobHeight(4f * _lobPeakHeight * progress * (1f - progress));
        }

        public bool ShouldImpact(Projectile projectile)
        {
            // 기본 직선탄은 충돌로만 처리한다.
            return _detonateAtDistance && _traveled >= _targetDistance;
        }
    }
}
