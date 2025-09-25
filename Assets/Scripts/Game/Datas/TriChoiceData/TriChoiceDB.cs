using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [CreateAssetMenu(fileName = "TriChoiceDB", menuName = "Data/TriChoiceDB")]
    public class TriChoiceDB : ScriptableObject
    {
        #region Fields
        [SerializeField]
        private List<TriChoiceDBData> triChoiceDBDatas = new();
        #endregion

        public IReadOnlyList<TriChoiceDBData> TriChoiceDBDatas => triChoiceDBDatas;
    }

    [Serializable]
    public class TriChoiceDBData
    {
        public TriChoiceData TriChoiceData;
        public float Weight;
    }
}