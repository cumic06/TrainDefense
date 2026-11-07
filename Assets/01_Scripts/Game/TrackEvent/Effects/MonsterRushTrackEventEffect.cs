using TrainDefense.Game.Datas;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 몬스터 러시 — 지속 시간 동안 현재 맵에서 제일 약한 몬스터를 평소 소환 위에 더 소환한다.
    /// magnitude = 러시 동안의 전체 등장 배율(2.5면 평소 소환 1 + 약한 몬스터 1.5). 화면은 붐비지만 체력이 낮은 몬스터라
    /// 걸린 판과 안 걸린 판의 압박 차이가 작다. 소환기 주기는 건드리지 않으므로 End에서 되돌릴 것이 없다.
    /// </summary>
    public class MonsterRushTrackEventEffect : ITrackEventEffect
    {
        private const float DEFAULT_SPAWN_MULTIPLIER = 2.5f;

        private TrackEventExtraSpawner _extraSpawner;

        /// <summary>러시 동안의 전체 등장 배율. 배너 문구도 이 값을 표시한다.</summary>
        public static float GetSpawnMultiplier(TrackEventData data)
        {
            return data != null && data.Magnitude > 1f ? data.Magnitude : DEFAULT_SPAWN_MULTIPLIER;
        }

        public void Begin(TrackEventData data)
        {
            _extraSpawner = new TrackEventExtraSpawner(GetSpawnMultiplier(data) - 1f, spawner => spawner.SpawnWeakestMonsterNow());
        }

        public void Tick(float deltaTime)
        {
            _extraSpawner?.Tick(deltaTime);
        }

        public void End(bool isInterrupted)
        {
            _extraSpawner = null;
        }
    }
}
