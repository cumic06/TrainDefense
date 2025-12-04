using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class AddTrainChoice : IChoiceOption
    {
        #region Fields
        [SerializeField]
        private string id;

        [SerializeField]
        [Tooltip("소환할 Train 데이터 ID")]
        private string trainDataId;
        #endregion

        public string Id => id;
        public string TrainDataId => trainDataId;

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(trainDataId)) return false;
            return !TrainManager.Instance.CheckHasTrainById(trainDataId);
        }

        public void Execute()
        {
            var trainData = DatabaseManager.Instance.GetTrainData(trainDataId);

            if (trainData == null || trainData.Prefab == null)
            {
                Debug.LogError($"AddTrainChoice [{id}]: Invalid TrainData");
                return;
            }

            var main = TrainManager.Instance.MainTrain;
            if (main == null)
            {
                Debug.LogError("AddTrainChoice: MainTrain is null");
                return;
            }

            main.SpawnTrain(trainData.Prefab.GetComponent<Train>());
        }
    }
}