using UnityEngine;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// Train 업그레이드 데이터 인터페이스
    /// </summary>
    public interface ITrainUpgradeData
    {
        string Id { get; }
        Sprite Icon { get; }
        string UpgradeName { get; }
        string Description { get; }
        TrainStatusData StatusUpgrade { get; }
    }
}