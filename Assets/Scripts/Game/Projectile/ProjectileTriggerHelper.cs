using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 하위 객체의 Trigger 이벤트를 상위 Projectile로 전달하는 헬퍼 컴포넌트
    /// 하위 GameObject에 붙여서 사용
    /// </summary>
    public class ProjectileTriggerHelper : MonoBehaviour
    {
        private Projectile _parentProjectile;

        private void Awake()
        {
            // 상위에서 Projectile 찾기
            _parentProjectile = GetComponentInParent<Projectile>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_parentProjectile != null && other.TryGetComponent<IProjectileTarget>(out var target))
            {
                _parentProjectile.ProcessEnter(target);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (_parentProjectile != null && other.TryGetComponent<IProjectileTarget>(out var target))
            {
                _parentProjectile.ProcessStay(target);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (_parentProjectile != null && other.TryGetComponent<IProjectileTarget>(out var target))
            {
                _parentProjectile.ProcessExit(target);
            }
        }
    }
}