using TrainDefense.Game.Datas;
using UnityEngine;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 한파 — 지속 동안 몬스터 이동속도 배율을 낮춘다. 실제 곱셈은 Monster._Move가 MonsterMoveSpeedMultiplier를 읽어 수행한다.
    /// 화면: 푸른 틴트 + 천천히 흔들리며 내리는 눈송이.
    /// </summary>
    public class ColdSnapTrackEventEffect : ITrackEventEffect
    {
        private const float DEFAULT_MULTIPLIER = 0.7f;

        private TrackEventWeatherEffect _weatherEffect;
        private float _appliedMultiplier = 1f;

        public void Begin(TrackEventData data)
        {
            _appliedMultiplier = data != null && data.Magnitude > 0f ? data.Magnitude : DEFAULT_MULTIPLIER;
            TrackEventModifiers.Multiply(TrackEventModifierType.MonsterMoveSpeed, _appliedMultiplier);
            _weatherEffect = TrackEventWeatherEffect.Create(TrackEventWeatherStyles.COLD_SNAP);
        }

        public void Tick(float deltaTime)
        {
        }

        public void End(bool isInterrupted)
        {
            TrackEventModifiers.Divide(TrackEventModifierType.MonsterMoveSpeed, _appliedMultiplier);

            if (_weatherEffect != null)
                _weatherEffect.FadeOutAndDestroy();
        }
    }
}
