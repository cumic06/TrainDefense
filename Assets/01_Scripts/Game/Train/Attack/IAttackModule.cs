using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game
{
    /// <summary>
    /// 기차의 공격 동작을 캡슐화하는 컴포넌트 계약. 상속(TurretTrain/RangeTrain) 대신
    /// 컴포지션으로 Train에 조립된다. Train(shell)이 라이프사이클/스탯 가상 메서드를 이 모듈로 위임한다.
    /// 구현체: RangeAttackModule, (예정) TurretAttackModule.
    /// </summary>
    public interface IAttackModule
    {
        /// <summary>owner(Train)와 데이터를 주입하고 공격 동작을 시작 가능 상태로 만든다. Train.Setup에서 호출.</summary>
        void InitializeModule(Train owner);

        /// <summary>부착된 잔류 투사체를 풀로 반환(상점 진입/전투 준비 등).</summary>
        void ClearAttachedProjectiles();

        /// <summary>현재(업그레이드 반영) 공격 사거리.</summary>
        float CurrentAttackRange { get; }

        /// <summary>사거리 표시 원 반지름.</summary>
        float RangeIndicatorRadius { get; }

        /// <summary>현재값(%) 기준 스탯 적용. 모듈이 처리한 타입이면 true, 아니면 false(=Train이 폴백).</summary>
        bool ApplyAttackStatByCurrentValue(IStat stat);

        /// <summary>단일 스탯 적용(base 누적 이후 호출됨).</summary>
        void ApplyAttackStat(IStat stat);

        /// <summary>레벨 인지 스탯 적용. 처리했으면 true, 아니면 false(=Train base 폴백).</summary>
        bool ApplyAttackStatLevelAware(IStat stat, int newLevel, int prevLevel);

        /// <summary>업그레이드 데이터 적용(레벨업).</summary>
        void ApplyUpgrade(ITrainUpgradeData upgradeData, int prevLevel);

        /// <summary>엘리트 교체 시 진행도(스탯 델타) 이관.</summary>
        void CopyProgressFrom(IAttackModule source);
    }
}
