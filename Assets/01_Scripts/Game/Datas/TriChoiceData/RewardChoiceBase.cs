using System;
using UnityEngine;
using TrainDefense.Localize;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 만렙(보유 기차 전부 만렙) 도달 후 정상 선택지(추가/강화/엘리트)를 채울 수 없을 때
    /// 빈 슬롯을 대체하는 보상 선택지의 공통 베이스.
    /// 기차를 참조하지 않으며, 표시 정보(아이콘·이름/설명 로컬라이즈 키)와 효과(Execute)만 가진다.
    /// </summary>
    [Serializable]
    public abstract class RewardChoiceBase : IChoiceOption
    {
        #region Fields
        [SerializeField]
        private string id;

        [SerializeField]
        [Tooltip("선택지 이름 로컬라이즈 키")]
        private string nameKey;

        [SerializeField]
        [Tooltip("선택지 설명 로컬라이즈 키")]
        private string descriptionKey;

        [SerializeField]
        [Tooltip("선택지 아이콘 ID (아이콘 바인딩 시 Sprite 매칭용)")]
        private string iconId;

        [SerializeField]
        [Tooltip("선택지 아이콘 스프라이트")]
        private Sprite icon;
        #endregion

        public string Id => id;
        public string NameKey => nameKey;
        public string DescriptionKey => descriptionKey;
        public string IconId => iconId;

        // TrainData와 동일 패턴: 키가 로컬라이즈 시트에 없으면 키 문자열을 그대로 반환(fallback).
        public string Name => LocalizeHelper.GetByKey(nameKey, nameKey);
        public string Description => LocalizeHelper.GetByKey(descriptionKey, descriptionKey);

        // 아이콘 Sprite는 런타임에 iconId로부터 lazy 로드해 캐싱한다(Resources/Sprite/{iconId}).
        public Sprite Icon
        {
            get
            {
                if (icon == null && !string.IsNullOrEmpty(iconId))
                {
                    icon = Resources.Load<Sprite>($"Sprite/{iconId}");

                    if (icon == null)
                        Debug.LogWarning($"RewardChoice [{id}]: Icon not found at 'Sprite/{iconId}'");
                }

                return icon;
            }
        }

        // 만렙 보상은 기본적으로 항상 노출 가능. 필요 시 파생 클래스가 조건을 덧붙인다.
        public virtual bool IsValid() => true;

        public abstract void Execute();
    }
}
