using System;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 선택지 엔트리 클래스
    /// </summary>
    [Serializable]
    public class ChoiceEntry
    {
        [UnityEngine.SerializeReference]
        public IChoiceOption Option;
        public int Weight;
    }
}
