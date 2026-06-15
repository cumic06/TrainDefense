using System;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 공격 시 발행되는 이벤트. 포탑/범위 모두 구현한다(범위는 monster=null로 발행).
    /// 패시브가 구체 타입(TurretTrain/RangeTrain) 대신 이 인터페이스로 구독하여 결합을 끊는다.
    /// </summary>
    public interface IAttackEvents
    {
        event Action<Monster> OnAttacked;
    }

    /// <summary>
    /// 외부(패시브/스킬)가 추가 투사체를 owner 위치/타겟에 발사. 포탑/범위 공통.
    /// </summary>
    public interface IExternalProjectileSpawner
    {
        void SpawnExternalProjectile(Projectile prefab, float radius,
            IProjectileTarget target = null, float damageMul = 1f, float shoveScale = 1f);
    }

    /// <summary>
    /// 쿨다운을 무시하고 즉시 1회 공격을 강제. 포탑/범위 공통.
    /// </summary>
    public interface IForceAttacker
    {
        bool ForceAttack();
    }

    /// <summary>
    /// 기본 공격의 넉백을 억제. (현재 범위 전용)
    /// </summary>
    public interface IShoveSuppressible
    {
        void SetSuppressMainProjectileShove(bool suppress);
    }

    /// <summary>
    /// 개별 투사체 발사형(포탑) 공격기의 확장 훅 모음. 연속발사 / 위치 공격 / 타겟 위치 오버라이드 등
    /// 포탑 고유 동작에 의존하는 패시브가 이 인터페이스로 접근한다.
    /// </summary>
    public interface IProjectileAttacker
    {
        float CurrentAttackDamage { get; }
        event Action OnTargetPosAttacked;
        Func<Vector3?> TargetPosOverride { get; set; }
        void RepeatNormalAttack(Vector2? aimPosition = null);
        void SpawnProjectileAtWorldPositionPublic(Monster target, Vector3 worldPosition, bool playSound = false);
    }
}
