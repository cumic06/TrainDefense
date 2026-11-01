using TrainDefense.Game.Datas;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 배율 1~2개를 거는 범용 효과 — 날씨(비·안개·폭염)와 선택지(골드·경험치·공속·사거리·몬스터 감속)가 쓴다.
    /// 첫 배율 = data.Magnitude, 두 번째 배율 = data.SubMagnitude. 화면 연출(날씨)이 있으면 함께 띄운다.
    /// 다른 이벤트와 같은 배율을 동시에 쓸 수 있도록 Multiply/Divide로 걸고 푼다.
    /// </summary>
    public class ModifierTrackEventEffect : ITrackEventEffect
    {
        private readonly TrackEventModifierType _primaryType;
        private readonly TrackEventModifierType? _secondaryType;
        private readonly TrackEventWeatherEffect.WeatherStyle? _weatherStyle;

        private float _primaryMultiplier = 1f;
        private float _secondaryMultiplier = 1f;
        private TrackEventWeatherEffect _weatherEffect;

        public ModifierTrackEventEffect(TrackEventModifierType primaryType,
                                        TrackEventModifierType? secondaryType = null,
                                        TrackEventWeatherEffect.WeatherStyle? weatherStyle = null)
        {
            _primaryType = primaryType;
            _secondaryType = secondaryType;
            _weatherStyle = weatherStyle;
        }

        public void Begin(TrackEventData data)
        {
            _primaryMultiplier = data != null && data.Magnitude > 0f ? data.Magnitude : 1f;
            TrackEventModifiers.Multiply(_primaryType, _primaryMultiplier);

            if (_secondaryType.HasValue)
            {
                _secondaryMultiplier = data != null && data.SubMagnitude > 0f ? data.SubMagnitude : 1f;
                TrackEventModifiers.Multiply(_secondaryType.Value, _secondaryMultiplier);
            }

            if (_weatherStyle.HasValue)
                _weatherEffect = TrackEventWeatherEffect.Create(_weatherStyle.Value);
        }

        public void Tick(float deltaTime)
        {
        }

        public void End(bool isInterrupted)
        {
            TrackEventModifiers.Divide(_primaryType, _primaryMultiplier);

            if (_secondaryType.HasValue)
                TrackEventModifiers.Divide(_secondaryType.Value, _secondaryMultiplier);

            if (_weatherEffect != null)
                _weatherEffect.FadeOutAndDestroy();
        }
    }
}
