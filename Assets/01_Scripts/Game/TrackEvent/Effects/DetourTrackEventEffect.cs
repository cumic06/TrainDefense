using TrainDefense.Game.Datas;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 험한 길(위험 카드) — 역 도착까지 평소 확률의 몬스터를 더 소환하고, 그동안 처치 골드·경험치를 올린다.
    /// magnitude = 몬스터 등장 배율(1.3이면 평소의 30%를 더 소환), subMagnitude = 처치 골드·경험치 배율.
    /// 빙판 미끄럼(한파 갈림길)은 같은 효과에서 경험치를 빼고 골드만 올린다.
    /// </summary>
    public class DetourTrackEventEffect : ITrackEventEffect
    {
        private const float DEFAULT_SPAWN_MULTIPLIER = 1.3f;
        private const float DEFAULT_REWARD_MULTIPLIER = 1.5f;

        private readonly bool _isExpRewarded;

        private TrackEventExtraSpawner _extraSpawner;
        private float _appliedRewardMultiplier = 1f;

        public DetourTrackEventEffect(bool isExpRewarded = true)
        {
            _isExpRewarded = isExpRewarded;
        }

        public static float GetSpawnMultiplier(TrackEventData data)
        {
            return data != null && data.Magnitude > 1f ? data.Magnitude : DEFAULT_SPAWN_MULTIPLIER;
        }

        public static float GetRewardMultiplier(TrackEventData data)
        {
            return data != null && data.SubMagnitude > 1f ? data.SubMagnitude : DEFAULT_REWARD_MULTIPLIER;
        }

        public void Begin(TrackEventData data)
        {
            _extraSpawner = new TrackEventExtraSpawner(GetSpawnMultiplier(data) - 1f, spawner => spawner.SpawnRandomMonsterNow());
            _appliedRewardMultiplier = GetRewardMultiplier(data);
            TrackEventModifiers.Multiply(TrackEventModifierType.KillGold, _appliedRewardMultiplier);

            if (_isExpRewarded)
                TrackEventModifiers.Multiply(TrackEventModifierType.KillExp, _appliedRewardMultiplier);
        }

        public void Tick(float deltaTime)
        {
            _extraSpawner?.Tick(deltaTime);
        }

        public void End(bool isInterrupted)
        {
            _extraSpawner = null;
            TrackEventModifiers.Divide(TrackEventModifierType.KillGold, _appliedRewardMultiplier);

            if (_isExpRewarded)
                TrackEventModifiers.Divide(TrackEventModifierType.KillExp, _appliedRewardMultiplier);
        }
    }
}
