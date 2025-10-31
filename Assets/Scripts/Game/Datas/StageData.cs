using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class StageData : IData
    {
        #region Fields
        [SerializeField]
        private string id;
        [SerializeField]
        private float[] stageInspectionTime;
        [SerializeField]
        private float stageEndTime;
        #endregion

        #region IData
        public string Id => id;
        #endregion

        public float[] StageInspectionTime => stageInspectionTime;
        public float StageEndTime => stageEndTime;
    }
}