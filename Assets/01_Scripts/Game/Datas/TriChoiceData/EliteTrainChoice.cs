using System;
using System.Collections.Generic;
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
            // 부서진 포탑은 수리한 뒤에 개조한다 — 교체(ReplaceTrain)가 새 칸을 산 채로 만들어 수리 없이 되살아난다.
            if (baseTrain.IsDead) return false;
            // 개조 조건: 원본 포탑에 산 강화 카드 등급의 합(Train.ELITE_PROMOTION_GRADE_SUM)
            if (!baseTrain.IsEliteEligible) return false;

            // 해금된 개조가 하나라도 있어야 카드로 유효
            return HasUnlockedSkill();
        }

        /// <summary>해금된 개조가 하나라도 있는지. 상점 갱신마다 모든 개조 카드에 대해 불리므로 목록을 만들지 않는다.</summary>
        public bool HasUnlockedSkill()
        {
            foreach (var skill in _GetSkills())
            {
                if (_IsUnlocked(skill.skillData)) return true;
            }

            return false;
        }

        /// <summary>이 카드가 낼 수 있는 개조 — 해금된 것만. 카드가 뽑힐 때 이 중 하나가 무작위로 붙는다(TriChoiceManager.GetSelectedEliteSkill).</summary>
        public List<(IData skillData, TrainChoiceSkillType skillType)> GetUnlockedSkills()
        {
            var result = new List<(IData skillData, TrainChoiceSkillType skillType)>();

            foreach (var skill in _GetSkills())
            {
                if (_IsUnlocked(skill.skillData))
                    result.Add(skill);
            }

            return result;
        }

        private IReadOnlyList<(IData skillData, TrainChoiceSkillType skillType)> _GetSkills()
        {
            var databaseManager = DatabaseManager.Instance;

            return databaseManager != null
                ? databaseManager.GetTrainSkillDataDB().GetSkillsForTrain(eliteTrainDataId)
                : Array.Empty<(IData skillData, TrainChoiceSkillType skillType)>();
        }

        private static bool _IsUnlocked(IData skillData)
        {
            var userDataManager = UserDataManager.Instance;

            return userDataManager != null && skillData is TrainPassiveSkillData remodel && userDataManager.IsRemodelUnlocked(remodel);
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
