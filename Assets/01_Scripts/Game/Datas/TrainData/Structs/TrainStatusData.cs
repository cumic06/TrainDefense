using System;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 기차의 기본 스탯. Struct로 유지해서 ScriptableObject 원본이
    /// 런타임에 변경되지 않도록 합니다.
    /// </summary>
    [Serializable]
    public struct TrainStatusData
    {
        public float MaxHp;
    }
}