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

        // 승격될 원본 포탑(편성에 없으면 null). 상점 카드 롱프레스가 승격 후 스탯을 미리 계산할 때 쓴다.
        public Train FindBaseTrain()
        {
            var mainTrain = TrainManager.Instance != null ? TrainManager.Instance.MainTrain : null;
            return mainTrain != null ? mainTrain.CurrentTrains.FirstOrDefault(t => t.TrainData.Id == baseTrainId) : null;
        }

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
            // 승격 조건이 있으면 원본 포탑의 현재 스탯이 전부 만족해야 하고, 없으면 강화 카드 등급의 합(Train.ELITE_PROMOTION_GRADE_SUM)으로 판정한다.
            if (!_IsPromotionConditionSatisfied(baseTrain, eliteTrainData)) return false;

            // 스킬이 하나라도 있어야 엘리트 카드로 유효
            return databaseManager.GetTrainSkillDataDB().HasSkillForTrain(eliteTrainDataId);
        }

        private static bool _IsPromotionConditionSatisfied(Train baseTrain, TrainData eliteTrainData)
        {
            var conditions = eliteTrainData.ElitePromotionConditions;

            if (conditions.Length == 0) return baseTrain.IsEliteEligible;

            foreach (var condition in conditions)
            {
                if (!condition.IsSatisfiedBy(baseTrain)) return false;
            }

            return true;
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
