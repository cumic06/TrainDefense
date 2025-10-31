using UnityEngine;

namespace TrainDefense.Game.Datas
{
    public interface IIconData : IData
    {
        string IconId { get; }
        Sprite Icon { get; }
    }
}