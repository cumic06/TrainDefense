using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 선로 이벤트 한 개의 정적 정의. 지금은 TrackEventManager 인스펙터에서 직접 채우고,
    /// 종류가 늘면 DB + 엑셀 임포터(EventData.xlsx)로 옮긴다(plans/선로이벤트_구현계획.md 3단계).
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
        [Tooltip("뽑기 가중치. 0이면 뽑히지 않는다.")]
        private float weight = 1f;
        [SerializeField]
        [Tooltip("이 누적 상점 수(StageManager.TotalInspectionPassedCount) 이상부터 등장한다. 초반 난이도 보호용.")]
        private int minStationCount;
        [SerializeField]
        [Tooltip("효과 지속 시간(초). 0 이하면 역 도착까지 유지된다.")]
        private float duration = 10f;
        [SerializeField]
        [Tooltip("효과 세기. 타입마다 의미가 다르다(러시=스폰 간격 배율, 과부하=공속 배율, 보급=골드 배율 등).")]
        private float magnitude = 1f;
        [SerializeField]
        [Tooltip("이벤트 이름 로컬라이즈 키. 배너 UI에 쓴다.")]
        private string nameKey;
        [SerializeField]
        [Tooltip("이벤트 설명 로컬라이즈 키. 배너 UI에 쓴다.")]
        private string descriptionKey;
        [SerializeField]
        private Sprite icon;
        #endregion

        #region IData
        public string Id => id;
        #endregion

        public TrackEventType EventType => eventType;
        public float Weight => weight;
        public int MinStationCount => minStationCount;
        public float Duration => duration;
        public bool IsUntilStation => duration <= 0f;
        public float Magnitude => magnitude;
        public string NameKey => nameKey;
        public string DescriptionKey => descriptionKey;
        public Sprite Icon => icon;
    }
}
