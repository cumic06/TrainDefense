using TrainDefense.Game.Datas;
using UnityEngine;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 모래폭풍 — 지속 시간 동안 전 포탑 사거리 배율(기본 0.7).
    /// 배율은 TrackEventModifiers에만 걸고, 포탑이 전투 판정 시 곱한다. 시간 만료는 TrackEventManager가 End로 처리한다.
    /// 화면: 모래빛 틴트 + 옆으로 빠르게 날리는 모래 알갱이.
    /// </summary>
    public class SandstormTrackEventEffect : ITrackEventEffect
    {
        private const float DEFAULT_MULTIPLIER = 0.7f;

        private TrackEventWeatherEffect _weatherEffect;
        private float _appliedMultiplier = 1f;

        public void Begin(TrackEventData data)
        {
            _appliedMultiplier = data != null && data.Magnitude > 0f ? data.Magnitude : DEFAULT_MULTIPLIER;
            TrackEventModifiers.Multiply(TrackEventModifierType.TurretRange, _appliedMultiplier);
            _weatherEffect = TrackEventWeatherEffect.Create(TrackEventWeatherStyles.SANDSTORM);
        }

        public void Tick(float deltaTime)
        {
        }

        public void End(bool isInterrupted)
        {
            TrackEventModifiers.Divide(TrackEventModifierType.TurretRange, _appliedMultiplier);

            if (_weatherEffect != null)
                _weatherEffect.FadeOutAndDestroy();
        }
    }
}
