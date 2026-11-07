using TrainDefense.Game.Datas;
using TrainDefense.Game.TrackEvent.Effects;

namespace TrainDefense.Game.TrackEvent
{
    /// <summary>
    /// 이벤트 타입 → 효과 구현 매핑. 새 이벤트를 붙일 때는 Effects/ 에 ITrackEventEffect 구현을 만들고 여기 한 줄만 추가한다.
    /// 구현이 없는 타입은 null을 돌려주고, TrackEventManager가 그 추첨을 건너뛴다.
    /// </summary>
    public static class TrackEventEffectFactory
    {
        public static ITrackEventEffect Create(TrackEventType eventType)
        {
            return eventType switch
            {
                TrackEventType.MonsterRush => new MonsterRushTrackEventEffect(),
                TrackEventType.Overdrive => new OverdriveTrackEventEffect(),
                TrackEventType.Sandstorm => new SandstormTrackEventEffect(),
                TrackEventType.ExpBoost => new ExpBoostTrackEventEffect(),
                TrackEventType.ColdSnap => new ColdSnapTrackEventEffect(),
                TrackEventType.GoldenMonster => new GoldenMonsterTrackEventEffect(),
                TrackEventType.EliteAmbush => new EliteAmbushTrackEventEffect(),
                TrackEventType.SupportBombard => new SupportBombardTrackEventEffect(),
                TrackEventType.SupplyDrop => new SupplyDropTrackEventEffect(),
                TrackEventType.RepairStop => new RepairStopTrackEventEffect(),
                TrackEventType.FreeUpgrade => new FreeUpgradeTrackEventEffect(),
                TrackEventType.Tailwind => new TailwindTrackEventEffect(),

                // 위험/안전 선택
                TrackEventType.DetourChoice => new DetourTrackEventEffect(),
                TrackEventType.SafePass => new SafePassTrackEventEffect(),
                TrackEventType.EngineOverload => new EngineOverloadTrackEventEffect(),

                // 날씨 전용
                TrackEventType.Rain => new ModifierTrackEventEffect(TrackEventModifierType.MonsterMoveSpeed, TrackEventModifierType.TurretRange, TrackEventWeatherStyles.RAIN),
                TrackEventType.Fog => new ModifierTrackEventEffect(TrackEventModifierType.TurretRange, TrackEventModifierType.KillExp, TrackEventWeatherStyles.FOG),
                TrackEventType.Heatwave => new ModifierTrackEventEffect(TrackEventModifierType.TurretDamage, TrackEventModifierType.MonsterMoveSpeed, TrackEventWeatherStyles.HEATWAVE),

                // 날씨 갈림길 — 배율 두 개짜리는 날씨와 같은 범용 효과를 쓴다 (첫 배율 magnitude, 두 번째 subMagnitude)
                TrackEventType.SandstormClearView => new ModifierTrackEventEffect(TrackEventModifierType.TurretRange),
                TrackEventType.SandstormPushThrough => new ModifierTrackEventEffect(TrackEventModifierType.MonsterMoveSpeed, TrackEventModifierType.KillGold),
                TrackEventType.RainCover => new ModifierTrackEventEffect(TrackEventModifierType.TurretRange, TrackEventModifierType.TurretAttackSpeed),
                TrackEventType.RainCooling => new ModifierTrackEventEffect(TrackEventModifierType.TurretAttackSpeed, TrackEventModifierType.TurretRange),
                TrackEventType.FogMarch => new ModifierTrackEventEffect(TrackEventModifierType.TurretRange, TrackEventModifierType.KillExp),
                TrackEventType.FogSearch => new ModifierTrackEventEffect(TrackEventModifierType.TurretRange, TrackEventModifierType.KillGold),
                TrackEventType.HeatwaveOverheat => new ModifierTrackEventEffect(TrackEventModifierType.TurretDamage, TrackEventModifierType.MonsterMoveSpeed),
                TrackEventType.HeatwaveExhausted => new ModifierTrackEventEffect(TrackEventModifierType.MonsterAttack, TrackEventModifierType.MonsterHealth),
                TrackEventType.HeatwaveElites => new HeatedEliteTrackEventEffect(),
                TrackEventType.ColdSnapIceSlide => new DetourTrackEventEffect(false),
                TrackEventType.ColdSnapBlizzardPush => new ModifierTrackEventEffect(TrackEventModifierType.TurretRange, TrackEventModifierType.KillGold),

                // 선택지
                TrackEventType.ChoiceGoldBonus => new ModifierTrackEventEffect(TrackEventModifierType.KillGold),
                TrackEventType.ChoiceExpBonus => new ModifierTrackEventEffect(TrackEventModifierType.KillExp),
                TrackEventType.ChoiceAttackSpeed => new ModifierTrackEventEffect(TrackEventModifierType.TurretAttackSpeed),
                TrackEventType.ChoiceRange => new ModifierTrackEventEffect(TrackEventModifierType.TurretRange),
                TrackEventType.ChoiceMonsterSlow => new ModifierTrackEventEffect(TrackEventModifierType.MonsterMoveSpeed),
                TrackEventType.ChoiceRepair => new RepairStopTrackEventEffect(),
                TrackEventType.ChoiceTurretDamage => new ModifierTrackEventEffect(TrackEventModifierType.TurretDamage),
                // TODO: 구현 순서는 plans/선로이벤트_구현계획.md "4. 이벤트 아이디어" 우선순위를 따른다.
                _ => null,
            };
        }
    }
}
