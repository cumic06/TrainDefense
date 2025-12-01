using UnityEngine;

namespace TrainDefense.Game.Datas
{
    public interface IPrefabData : IData
    {
        string PrefabId { get; }
        GameObject Prefab { get; }
    }
}