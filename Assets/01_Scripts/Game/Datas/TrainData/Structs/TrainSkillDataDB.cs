using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class TrainSkillDataDB
    {
        [SerializeField]
        public List<TrainPassiveSkillData> trainPassiveSkillDataList = new();

        // trainDataId → (skillData, skillType) 런타임 인덱스
        [NonSerialized]
        private Dictionary<string, List<(IData skillData, TrainChoiceSkillType skillType)>> _index;

        public IReadOnlyList<TrainPassiveSkillData> TrainPassiveSkillDataList => trainPassiveSkillDataList;

        /// <summary>
        /// TrainData 목록을 순회해 trainDataId → skills 역방향 인덱스를 빌드합니다.
        /// DatabaseManager 초기화 시 한 번 호출하세요.
        /// </summary>
        public void BuildIndex(IEnumerable<TrainData> allTrains)
        {
            _index = new Dictionary<string, List<(IData, TrainChoiceSkillType)>>();

            foreach (var trainData in allTrains)
            {
                if (trainData == null) continue;

                var skills = new List<(IData, TrainChoiceSkillType)>();

                var passiveIds = trainData.PassiveSkillDataIds;
                if (passiveIds != null)
                {
                    foreach (var skillId in passiveIds)
                    {
                        if (string.IsNullOrEmpty(skillId)) continue;
                        var skill = trainPassiveSkillDataList.Find(s => s != null && s.Id == skillId);
                        if (skill != null)
                            skills.Add((skill, TrainChoiceSkillType.Passive));
                        else
                            UnityEngine.Debug.LogWarning($"[TrainSkillDataDB] TrainData[{trainData.Id}]: PassiveSkill '{skillId}' not found");
                    }
                }

                if (skills.Count > 0)
                    _index[trainData.Id] = skills;
            }
        }

        /// <summary>
        /// trainDataId에 연결된 스킬 중 랜덤으로 하나를 반환합니다.
        /// 인덱스가 없으면 allTrains 없이는 결과 없음 — DatabaseManager.Awake에서 BuildIndex를 먼저 호출하세요.
        /// </summary>
        public (IData skillData, TrainChoiceSkillType skillType) GetRandomSkillForTrain(string trainDataId)
        {
            if (_index == null)
            {
                UnityEngine.Debug.LogWarning("TrainSkillDataDB: BuildIndex가 호출되지 않은 상태에서 GetRandomSkillForTrain 호출됨");
                return (null, TrainChoiceSkillType.None);
            }

            if (!_index.TryGetValue(trainDataId, out var skills) || skills.Count == 0)
                return (null, TrainChoiceSkillType.None);

            var picked = skills[UnityEngine.Random.Range(0, skills.Count)];
            return picked;
        }

        /// <summary>
        /// trainDataId에 연결된 스킬이 있는지 여부를 반환합니다.
        /// </summary>
        public bool HasSkillForTrain(string trainDataId)
        {
            if (_index == null)
            {
                UnityEngine.Debug.LogWarning("TrainSkillDataDB: BuildIndex가 호출되지 않은 상태에서 HasSkillForTrain 호출됨");
                return false;
            }
            return _index.TryGetValue(trainDataId, out var skills) && skills.Count > 0;
        }

        /// <summary>
        /// trainDataId에 연결된 모든 스킬을 반환합니다. 없으면 빈 목록.
        /// </summary>
        public IReadOnlyList<(IData skillData, TrainChoiceSkillType skillType)> GetSkillsForTrain(string trainDataId)
        {
            if (_index == null)
            {
                UnityEngine.Debug.LogWarning("TrainSkillDataDB: BuildIndex가 호출되지 않은 상태에서 GetSkillsForTrain 호출됨");
                return System.Array.Empty<(IData, TrainChoiceSkillType)>();
            }
            return _index.TryGetValue(trainDataId, out var skills) ? skills : System.Array.Empty<(IData, TrainChoiceSkillType)>();
        }
    }
}
