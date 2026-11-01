using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 선로 이벤트 한 개의 정적 정의. 지금은 TrackEventManager 인스펙터에서 직접 채우고,
    /// 종류가 늘면 DB + 엑셀 임포터(EventData.xlsx)로 옮긴다(plans/선로이벤트_구현계획.md 3단계).
    /// 날씨·선택 분류는 인스펙터 목록에 없으면 TrackEventDefaults의 코드 기본값을 쓴다.
    /// </summary>
    [Serializable]
    public class TrackEventData : IData
    {
        #region Fields
        [SerializeField]
        private string id;
        [SerializeField]
        private TrackEventType eventType;
        [SerializeField]
        [Tooltip("나오는 방식. Random=확률 / Weather=N역마다 확정 / Choice=N역마다 2지선다 후보.")]
        private TrackEventCategory category;
        [SerializeField]
        [Tooltip("뽑기 가중치. 0이면 뽑히지 않는다.")]
        private float weight = 1f;
        [SerializeField]
        [Tooltip("이 누적 상점 수(StageManager.TotalInspectionPassedCount) 이상부터 등장한다. 초반 난이도 보호용.")]
        private int minStationCount;
        [SerializeField]
        [Tooltip("효과 지속 시간(초). 0 이하면 역 도착까지 유지된다. 날씨·선택은 이 값과 무관하게 역 도착까지 유지된다.")]
        private float duration = 10f;
        [SerializeField]
        [Tooltip("효과 세기. 타입마다 의미가 다르다(러시=스폰 간격 배율, 과부하=공속 배율, 보급=골드 배율 등).")]
        private float magnitude = 1f;
        [SerializeField]
        [Tooltip("보조 효과 세기. 효과가 두 개인 날씨(비·안개·폭염)의 두 번째 배율.")]
        private float subMagnitude = 1f;
        [SerializeField]
        [Tooltip("이벤트 이름 로컬라이즈 키. 배너·선택 카드에 쓴다.")]
        private string nameKey;
        [SerializeField]
        [Tooltip("이벤트 설명 로컬라이즈 키. 배너 UI에 쓴다.")]
        private string descriptionKey;
        [SerializeField]
        private Sprite icon;
        [SerializeField]
        [Tooltip("icon이 비어 있을 때 Resources/Sprite/ 아래에서 읽을 경로 (예: Upgrade/Upgrade_110004).")]
        private string iconId;
        #endregion

        #region Variables
        private bool _hasTriedIconLoad;
        #endregion

        #region IData
        public string Id => id;
        #endregion

        public TrackEventType EventType => eventType;
        public TrackEventCategory Category => category;
        public float Weight => weight;
        public int MinStationCount => minStationCount;
        public float Duration => duration;
        public bool IsUntilStation => duration <= 0f;
        public float Magnitude => magnitude;
        public float SubMagnitude => subMagnitude;
        public string NameKey => nameKey;
        public string DescriptionKey => descriptionKey;

        public Sprite Icon
        {
            get
            {
                if (icon == null && !_hasTriedIconLoad && !string.IsNullOrEmpty(iconId))
                {
                    _hasTriedIconLoad = true;
                    icon = Resources.Load<Sprite>($"Sprite/{iconId}");
                }

                return icon;
            }
        }

        // Unity 직렬화(인스펙터 목록)용.
        public TrackEventData()
        {
        }

        /// <summary>코드 기본값(TrackEventDefaults)용. 지속 시간 0 = 역 도착까지 유지.</summary>
        public TrackEventData(string id, TrackEventType eventType, TrackEventCategory category, float weight,
                              int minStationCount, float magnitude, float subMagnitude, string nameKey, string iconId)
        {
            this.id = id;
            this.eventType = eventType;
            this.category = category;
            this.weight = weight;
            this.minStationCount = minStationCount;
            this.duration = 0f;
            this.magnitude = magnitude;
            this.subMagnitude = subMagnitude;
            this.nameKey = nameKey;
            this.iconId = iconId;
        }
    }
}
