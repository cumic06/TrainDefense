namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// Train 업그레이드 데이터 인터페이스
    /// </summary>
    public interface ITrainUpgradeData : IDescribableData, IIconData
    {
        TrainStatusData StatusUpgrade { get; }
    }
}