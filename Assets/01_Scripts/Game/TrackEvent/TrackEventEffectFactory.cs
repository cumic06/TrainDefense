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
                TrackEventType.GoldRush => new GoldRushTrackEventEffect(),
                TrackEventType.ExpBoost => new ExpBoostTrackEventEffect(),
                TrackEventType.ColdSnap => new ColdSnapTrackEventEffect(),
                TrackEventType.GoldenMonster => new GoldenMonsterTrackEventEffect(),
                TrackEventType.EliteAmbush => new EliteAmbushTrackEventEffect(),
                TrackEventType.SupportBombard => new SupportBombardTrackEventEffect(),
                TrackEventType.SupplyDrop => new SupplyDropTrackEventEffect(),
                TrackEventType.RepairStop => new RepairStopTrackEventEffect(),
                TrackEventType.FreeUpgrade => new FreeUpgradeTrackEventEffect(),
                TrackEventType.Tailwind => new TailwindTrackEventEffect(),
                // TODO: 구현 순서는 plans/선로이벤트_구현계획.md "4. 이벤트 아이디어" 우선순위를 따른다.
                _ => null,
            };
        }
    }
}
