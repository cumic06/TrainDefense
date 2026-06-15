namespace TrainDefense.Game
{
    /// <summary>
    /// 투사체 1발이 발사되는 시점의 컨텍스트. 총알 수식자(IProjectileModifier)에 전달된다.
    /// </summary>
    public struct ProjectileSpawnContext
    {
        /// <summary>기차의 누적 공격 횟수. "N발마다" 같은 주기 판정에 사용.</summary>
        public int AttackIndex;

        /// <summary>한 번의 공격에서 멀티 투사체가 나갈 때의 인덱스(0부터).</summary>
        public int ProjectileIndex;
    }

    /// <summary>
    /// 합성 가능한 총알 수식자. 한 기차에 여러 개가 붙으면 Order 순으로 모두 적용된다.
    /// 기존 "프리팹 통짜 교체(첫 non-null) + 스칼라 덮어쓰기" 모델을 대체하여
    /// 관통 + 광역 + 크기 + 넉백 같은 능력을 자유롭게 섞을 수 있게 한다.
    /// </summary>
    public interface IProjectileModifier
    {
        /// <summary>적용 순서. 낮을수록 먼저 적용된다. 프리팹 선택은 낮게, 비주얼/스탯 수식은 높게 두는 것을 권장.</summary>
        int Order { get; }

        /// <summary>
        /// 발사할 베이스 프리팹을 교체하고 싶을 때만 non-null을 반환한다. null이면 current를 그대로 유지.
        /// 여러 모디파이어가 교체를 시도하면 Order가 높은(나중) 쪽이 최종 프리팹이 된다.
        /// </summary>
        Projectile OverridePrefab(ProjectileSpawnContext ctx, Projectile current);

        /// <summary>스폰된 투사체 인스턴스에 누적 수식을 적용한다(크기/넉백 등). 여러 모디파이어가 함께 쌓인다.</summary>
        void Apply(ProjectileSpawnContext ctx, Projectile projectile);
    }

    /// <summary>
    /// 총알을 발사하는 기차가 구현. 패시브가 TurretTrain 같은 구체 타입에 직접 캐스팅하는 대신
    /// 이 인터페이스로 모디파이어를 등록하여 기차 종류에 독립적으로 동작한다.
    /// </summary>
    public interface IProjectileEmitter
    {
        void AddProjectileModifier(IProjectileModifier modifier);
        void RemoveProjectileModifier(IProjectileModifier modifier);
    }
}
