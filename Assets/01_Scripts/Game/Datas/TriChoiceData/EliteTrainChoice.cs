using System;
using System.Linq;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class EliteTrainChoice : IChoiceOption
    {
        #region Fields
        [SerializeField]
        private string id;

        [SerializeField]
        [Tooltip("엘리트로 전환할 기존 Train 데이터 ID")]
        private string baseTrainId;

        [SerializeField]
        [Tooltip("전환 후 사용할 Elite Train 데이터 ID")]
        private string eliteTrainDataId;
        #endregion

        public string Id => id;
        public string BaseTrainId => baseTrainId;
        public string EliteTrainDataId => eliteTrainDataId;

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(baseTrainId)) return false;
            if (string.IsNullOrEmpty(eliteTrainDataId)) return false;

            var trainManager = TrainManager.Instance;
            if (trainManager == null) return false;

            var databaseManager = DatabaseManager.Instance;
            if (databaseManager == null) return false;

            var eliteTrainData = databaseManager.GetTrainData(eliteTrainDataId);
            if (eliteTrainData == null) return false;

            if (trainManager.CheckHasTrainById(eliteTrainDataId)) return false;
            if (trainManager.IsTrainIdReplaced(baseTrainId)) return false;

            var mainTrain = trainManager.MainTrain;
            if (mainTrain == null) return false;

            var baseTrain = mainTrain.CurrentTrains.FirstOrDefault(t => t.TrainData.Id == baseTrainId);
            if (baseTrain == null) return false;
            if (!baseTrain.IsEliteEligible) return false;

            // 스킬이 하나라도 있어야 엘리트 카드로 유효
            return databaseManager.GetTrainSkillDataDB().HasSkillForTrain(eliteTrainDataId);
        }

        public void Execute()
        {
            var eliteTrainData = DatabaseManager.Instance.GetTrainData(eliteTrainDataId);

            if (eliteTrainData == null || eliteTrainData.Prefab == null)
            {
                Debug.LogError($"EliteTrainChoice [{id}]: Invalid EliteTrainData");
                return;
            }

            var main = TrainManager.Instance.MainTrain;
            if (main == null)
            {
                Debug.LogError("EliteTrainChoice: MainTrain is null");
                return;
            }

            // TriChoiceManager 캐시에서 결정된 skillType과 선택된 스킬 ID 사용
            var skillType = TrainChoiceSkillType.None;
            string selectedSkillId = null;
            if (TriChoiceManager.Instance != null)
            {
                skillType = TriChoiceManager.Instance.GetCachedEliteSkillType(id);
                selectedSkillId = TriChoiceManager.Instance.GetCachedEliteSkillId(id);
            }

            main.ReplaceTrain(baseTrainId, eliteTrainData, skillType, selectedSkillId);
        }
    }
}
