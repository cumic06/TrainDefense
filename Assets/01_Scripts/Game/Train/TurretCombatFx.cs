using DG.Tweening;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 포탑 발사 시 공통으로 쓰이는 비주얼/투사체 헬퍼.
    /// TurretTrain(Train 상속: 자동 탐지 포탑)과 Turret(MainTrain 주무기, 경량)이
    /// 동일한 발사 연출·투사체 초기화를 각자 복제하던 것을 한 곳으로 모은다.
    /// 상태(스탯)는 인스턴스마다 다르므로 호출자가 넘긴다.
    /// </summary>
    public static class TurretCombatFx
    {
        /// <summary>
        /// 발사 펀치 애니메이션: 모델을 baseScale*0.9로 줄였다가 원복.
        /// SetUpdate(true): timeScale=0(상점/일시정지/삼중택일) 중에도 트윈이 진행돼 원복된다.
        /// 누락 시 펀치 트윈이 0.9배 축소 상태에서 멈춰 모델이 작게 고정되는 외형 버그가 난다.
        /// </summary>
        public static void PlayAttackPunch(Transform model, Vector3 baseScale)
        {
            if (model == null)
                return;

            model.DOKill();
            model.DOScale(baseScale * 0.9f, 0.1f).SetEase(Ease.OutBack).SetUpdate(true).OnComplete(() =>
            {
                model.DOScale(baseScale, 0.1f).SetEase(Ease.InBack).SetUpdate(true);
            });
        }

        /// <summary>
        /// 다발 발사 시 index번째 투사체에 부채꼴 spread 각도를 적용한다. (LookAt2D 이후 호출)
        /// </summary>
        public static void ApplySpread(Transform projectile, int index, int count, float spreadAngle)
        {
            if (projectile == null || spreadAngle <= 0f || count <= 1)
                return;

            float offset = (index - (count - 1) * 0.5f) * spreadAngle;
            projectile.Rotate(0f, 0f, offset);
        }

        /// <summary>
        /// 스탯 기반으로 투사체를 초기화한다. target이 없으면 데미지만 초기화하되 Init은 호출한다.
        /// target==null이어도 Init을 호출해야 _movementStrategy가 생성돼 발사 방향으로 비행한다
        /// (Init 누락 시 투사체가 제자리에 멈추고 데미지도 0이 된다).
        /// </summary>
        public static void InitProjectile(Projectile projectile, TurretTrainStatus status, IProjectileTarget owner, IProjectileTarget target)
        {
            if (projectile == null)
                return;

            bool scaleByArea = projectile.IsScaleByArea();
            projectile.Init(
                status.AttackDamage,
                owner,
                target,
                scaleByArea ? status.AttackArea : 0f,
                status.CriticalChance,
                status.CriticalDamage,
                scaleByArea ? status.AttackRange : 0f);
        }
    }
}
