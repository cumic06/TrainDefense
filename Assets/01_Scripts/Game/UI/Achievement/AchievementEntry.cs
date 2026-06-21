using UnityEngine;
using Cumic.Achievement;
using TrainDefense.Localize;

namespace TrainDefense.Game.UI.Achievement
{
    /// <summary>
    /// 업적 한 칸이 표시할 데이터를 담는 뷰모델. 정의(IAchievementData)와 진행 상태(AchievementState)를
    /// 합쳐 슬롯/상세 패널이 이 타입만 알면 되도록 한다. 제목·설명은 로컬라이즈 키가 있으면 적용하고,
    /// 없으면 카탈로그의 기본(한글) 텍스트로 fallback 한다.
    /// </summary>
    public class AchievementEntry
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public int CurrentValue { get; }
        public int TargetValue { get; }
        public bool IsUnlocked { get; }

        /// <summary>0~1 진행도. 목표가 0이면 달성 여부로 간주한다.</summary>
        public float Progress => TargetValue > 0 ? Mathf.Clamp01((float)CurrentValue / TargetValue) : (IsUnlocked ? 1f : 0f);

        /// <summary>"3/50" 형태. 현재값이 목표를 넘지 않게 표시한다.</summary>
        public string ProgressText => $"{Mathf.Min(CurrentValue, TargetValue)}/{TargetValue}";

        public AchievementEntry(IAchievementData data, AchievementState state)
        {
            Id = data.Id;
            Title = LocalizeHelper.GetByKey($"Achievement_{data.Id}_Title", data.Title);
            Description = LocalizeHelper.GetByKey($"Achievement_{data.Id}_Desc", data.Description);
            CurrentValue = state != null ? state.CurrentValue : 0;
            TargetValue = data.TargetValue;
            IsUnlocked = state != null && state.IsUnlocked;
        }
    }
}
