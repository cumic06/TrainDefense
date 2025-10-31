using System;
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

        private TrainData _trainData;

        public string Id => id;

        public void Initialize(DB db)
        {
            _trainData = db?.GetTrainData(trainDataId);
        }

        public ChoiceUIInfo GetUIInfo()
        {
            if (_trainData == null)
            {
                var db = Resources.Load<DB>("Data/DB");
                _trainData = db?.GetTrainData(trainDataId);
            }

            if (_trainData == null)
            {
                Debug.LogError($"AddTrainChoice [{id}]: TrainData is null");
                return default;
            }

            return new ChoiceUIInfo
            {
                Icon = _trainData.Icon,
                Name = _trainData.Name,
                Description = _trainData.Description
            };
        }

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(trainDataId)) return false;
            return !TrainManager.Instance.CheckHasTrainById(trainDataId);
        }

        public void Execute()
        {
            if (_trainData == null)
            {
                var db = Resources.Load<DB>("Data/DB");
                _trainData = db?.GetTrainData(trainDataId);
            }

            if (_trainData == null || _trainData.Prefab == null)
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

            main.SpawnTrain(_trainData.Prefab.GetComponent<Train>());
        }
    }
}