using System;
using UnityEngine;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
    /// <summary>
    /// 범위 공격 기차(shell). 공통 동작은 Train + RangeAttackModule이 담당하고,
    /// 이 클래스는 범위 전용 표시/스탯 타입(SlowRate 등)만 모듈로 위임한다.
    /// </summary>
    public class RangeTrain : Train, ISlowProvider, IShoveSuppressible
    {
        private RangeTrainData rangeTrainData => _trainData as RangeTrainData;
        private RangeAttackModule _module => _attackModule as RangeAttackModule;

        public RangeTrainStatus BaseStatus => _module != null ? _module.BaseStatus : default;

        public override void StatusUpgrade(RangeTrainStatus upgradeData) => _module?.StatusUpgrade(upgradeData);

        // 누적 둔화율(%)을 둔화 배율로 변환.
        public float GetSlowValue() => _module != null ? _module.GetSlowValue() : 1f;

        public void SetSuppressMainProjectileShove(bool suppress) => _module?.SetSuppressMainProjectileShove(suppress);

        // 레인지 포탑은 사거리가 아닌 공격 범위(AttackArea)를 표시한다.
        public override float RangeIndicatorRadius => _module != null ? _module.RangeIndicatorRadius : CurrentAttackRange;

        public override void ApplyPassiveSkills()
        {
            var passives = rangeTrainData?.PassiveSkillDatas;
            if (passives == null) return;
            // 삼중택일은 픽한 스킬 1개만 부여. (Train._IsPassiveApplied 공용 규칙)
            foreach (var p in passives)
                if (_IsPassiveApplied(p.Id))
                    _skillModule.RegisterPassiveFromData(p);
        }

        public override string GetStatSummary()
        {
            var s = _module != null ? _module.CurrentStatus : default;
            return $"DMG={s.AttackDamage} | RANGE={s.AttackRange} | AREA={s.AttackArea} | MaxHp={_currentMaxHp}";
        }

        public override (string label, string value)[] GetStatDetails()
        {
            var s = _module != null ? _module.CurrentStatus : default;
            Func<string, string, string> L = TrainDefense.Localize.LocalizeHelper.GetByKey;
            return new[]
            {
                (L("Detail_HP", "HP"), $"{Mathf.RoundToInt(_currentMaxHp)}"),
                (L("Detail_Damage", "공격력"), $"{Mathf.RoundToInt(s.AttackDamage)}"),
                (L("Detail_Range", "사거리"), $"{s.AttackRange:F1}"),
                (L("Detail_Area", "범위"), $"{s.AttackArea:F1}"),
                (L("Detail_Speed", "공격속도"), $"{ToAttackSpeed(s.AttackInterval):F2}"),
                (L("Detail_CritChance", "크리티컬 확률"), $"{s.CriticalChance:F0}%"),
                (L("Detail_CritDamage", "크리티컬 데미지"), $"+{Projectile.BaseCriticalDamagePercent + s.CriticalDamage:F0}%"),
            };
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            if (rangeTrainData != null)
                Gizmos.DrawWireSphere(transform.position, rangeTrainData.RangeTrainStatus.AttackRange);
        }
    }
}
