using System.Collections.Generic;
using UnityEngine;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game.TrackEvent
{
    /// <summary>
    /// 자연환경 선로 이벤트 6종(비·안개·폭염·모래폭풍·한파·순풍)의 아이콘을 미리 만들어 둔 PNG에서 불러온다.
    /// 아이콘 파일: Assets/Resources/Sprite/TrackEvent/Weather_{TrackEventType}.png (256x256 손그림 스타일, Bilinear 필터).
    /// 데이터에 아이콘(TrackEventData.Icon)이 지정돼 있으면 그쪽을 먼저 쓴다(GetIcon).
    /// </summary>
    public static class TrackEventWeatherIcons
    {
        private const string ICON_PATH_FORMAT = "Sprite/TrackEvent/Weather_{0}";

        private static readonly HashSet<TrackEventType> WEATHER_ICON_TYPES = new()
        {
            TrackEventType.Rain,
            TrackEventType.Fog,
            TrackEventType.Heatwave,
            TrackEventType.Sandstorm,
            TrackEventType.ColdSnap,
            TrackEventType.Tailwind,
        };

        private static readonly Dictionary<TrackEventType, Sprite> _iconCache = new();

        /// <summary>
        /// 이벤트에 붙일 아이콘. 데이터에 지정된 아이콘이 있으면 그것을, 없으면 날씨 아이콘을 돌려준다.
        /// 날씨가 아닌 이벤트에 아이콘도 없으면 null.
        /// </summary>
        public static Sprite GetIcon(TrackEventData data)
        {
            if (data == null)
                return null;

            Sprite dataIcon = data.Icon;

            if (dataIcon != null)
                return dataIcon;

            return TryGetWeatherIcon(data.EventType, out Sprite weatherIcon) ? weatherIcon : null;
        }

        /// <summary>날씨류 이벤트 타입이면 Resources의 아이콘 PNG를 (한 번만) 불러와 돌려준다.</summary>
        public static bool TryGetWeatherIcon(TrackEventType type, out Sprite icon)
        {
            if (_iconCache.TryGetValue(type, out icon) && icon != null)
                return true;

            if (!WEATHER_ICON_TYPES.Contains(type))
            {
                icon = null;

                return false;
            }

            icon = Resources.Load<Sprite>(string.Format(ICON_PATH_FORMAT, type));

            if (icon == null)
            {
                Debug.LogWarning($"[TrackEventWeatherIcons] 날씨 아이콘이 없습니다: Resources/{string.Format(ICON_PATH_FORMAT, type)}.png");

                return false;
            }

            _iconCache[type] = icon;

            return true;
        }
    }
}
