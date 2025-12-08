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
    }
}