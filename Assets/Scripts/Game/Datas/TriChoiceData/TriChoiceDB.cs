using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [CreateAssetMenu(fileName = "TriChoiceDB", menuName = "Data/TriChoiceDB")]
    public class TriChoiceDB : ScriptableObject
    {
        #region Fields
        [SerializeField]
        private TriChoiceDBData[] triChoiceDBDatas;
        #endregion

        public TriChoiceDBData[] TriChoiceDBDatas => triChoiceDBDatas;
    }

    [Serializable]
    public class TriChoiceDBData
    {
        public TriChoiceData TriChoiceData;
        public float Weight;
    }
}