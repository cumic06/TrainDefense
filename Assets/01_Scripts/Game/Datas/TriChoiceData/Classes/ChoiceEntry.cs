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
        public int Tier; // 0: 기본, 1: 엘리트 등 고급 Train (Upgrade 3번 이상 선택 시 활성화)
    }
}
