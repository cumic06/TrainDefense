using System.Collections.Generic;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// Train 업그레이드 데이터 인터페이스
    /// </summary>
    public interface ITrainUpgradeData : IDescribableData, IIconData
    {
        /// <summary>
        /// 지정된 레벨의 스탯 업그레이드를 반환합니다.
        /// </summary>
        /// <param name="level">레벨 (0-based, CurrentLevel과 동일)</param>
        TrainStatusData GetStatusUpgrade(int level);
        int MaxLevel { get; }

        /// <summary>
        /// 트라이초이스 업그레이드 카드에 표시할 "현재→다음" 미리보기 줄. 타입별 구현이 채운다.
        /// </summary>
        /// <param name="trainData">대상 기차 데이터(타입 판별용)</param>
        /// <param name="currentLevel">현재 누적 레벨(증가 전)</param>
        /// <param name="nextLevel">다음 업그레이드 레벨 인덱스</param>
        IEnumerable<TrainStatLine> GetUpgradePreview(TrainData trainData, int currentLevel, int nextLevel);
    }
}