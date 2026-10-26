using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 몬스터 러시 — 기존 MonsterRushEvent(스폰 간격 배율 + WarningUI 경고)를 그대로 재사용하는 예시 구현.
    /// MonsterSpawner가 역 도착(InspectionStartEvent) 때 스폰 간격을 다시 계산하므로 러시는 역까지 유지된다.
    /// 그래서 데이터 duration은 0(역까지)으로 두는 것을 전제로 하고, End에서 되돌릴 상태가 없다.
    /// (중간에 끝내려고 MonsterRushEvent(1f)를 다시 쏘면 WarningUI가 한 번 더 뜬다.)
    /// </summary>
    public class MonsterRushTrackEventEffect : ITrackEventEffect
    {
        private const float DEFAULT_SPAWN_TIME_MULTIPLIER = 0.5f;

        public void Begin(TrackEventData data)
        {
            float multiplier = data != null && data.Magnitude > 0f ? data.Magnitude : DEFAULT_SPAWN_TIME_MULTIPLIER;
            GameEventSystem.Publish(new MonsterRushEvent(multiplier));
        }

        public void Tick(float deltaTime)
        {
        }

        public void End(bool isInterrupted)
        {
        }
    }
}
