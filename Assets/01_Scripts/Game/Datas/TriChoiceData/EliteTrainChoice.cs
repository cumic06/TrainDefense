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

        [SerializeField]
        [Tooltip("적용할 스킬 분기 (None은 기존 데이터 호환을 위해 Passive로 처리)")]
        private TrainChoiceSkillType skillType;
        #endregion

        public string Id => id;
        public string BaseTrainId => baseTrainId;
        public string EliteTrainDataId => eliteTrainDataId;
        public TrainChoiceSkillType SkillType => skillType;
        public TrainChoiceSkillType EffectiveSkillType =>
            skillType == TrainChoiceSkillType.None
                ? TrainChoiceSkillType.Passive
                : skillType;

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
            if (baseTrain.CurrentLevel < 2) return false;

            return HasRequiredSkill(eliteTrainData);
        }

        private bool HasRequiredSkill(TrainData eliteTrainData)
        {
            if (eliteTrainData == null) return false;

            switch (EffectiveSkillType)
            {
                case TrainChoiceSkillType.Passive:
                    return eliteTrainData.PassiveSkillDatas != null && eliteTrainData.PassiveSkillDatas.Length > 0;
                case TrainChoiceSkillType.Active:
                    return eliteTrainData.TrainSkillData != null && eliteTrainData.TrainSkillData.HasActiveSkill;
                default:
                    return true;
            }
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

            main.ReplaceTrain(baseTrainId, eliteTrainData, EffectiveSkillType);
        }
    }
}
