using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
    public partial class TriChoiceManager
    {
        public (IData skillData, TrainChoiceSkillType skillType) GetSelectedAddSkill(AddTrainChoice choice)
        {
            if (choice == null)
                return (null, TrainChoiceSkillType.None);

            if (_selectedAddSkills.TryGetValue(choice.Id, out var cached))
                return cached;

            var db = DatabaseManager.Instance?.GetTrainSkillDataDB();

            if (db == null)
                return (null, TrainChoiceSkillType.None);

            var picked = db.GetRandomSkillForTrain(choice.TrainDataId);

            if (picked.skillData != null)
                _selectedAddSkills[choice.Id] = picked;

            return picked;
        }

        public TrainChoiceSkillType GetCachedAddSkillType(string choiceId)
        {
            if (_selectedAddSkills.TryGetValue(choiceId, out var cached))
                return cached.skillType;

            return TrainChoiceSkillType.None;
        }

        public string GetCachedAddSkillId(string choiceId)
        {
            if (_selectedAddSkills.TryGetValue(choiceId, out var cached))
                return cached.skillData?.Id;

            return null;
        }

        public (IData skillData, TrainChoiceSkillType skillType) GetSelectedEliteSkill(EliteTrainChoice choice)
        {
            if (choice == null)
                return (null, TrainChoiceSkillType.None);

            if (_selectedEliteSkills.TryGetValue(choice.Id, out var cached))
                return cached;

            // 해금된 개조 중에서만 뽑는다 — 잠긴 개조는 카드에 붙지 않는다.
            var unlockedSkills = choice.GetUnlockedSkills();

            if (unlockedSkills.Count == 0)
                return (null, TrainChoiceSkillType.None);

            var picked = unlockedSkills[UnityEngine.Random.Range(0, unlockedSkills.Count)];
            _selectedEliteSkills[choice.Id] = picked;

            return picked;
        }

        public TrainChoiceSkillType GetCachedEliteSkillType(string choiceId)
        {
            if (_selectedEliteSkills.TryGetValue(choiceId, out var cached))
                return cached.skillType;

            return TrainChoiceSkillType.None;
        }

        public string GetCachedEliteSkillId(string choiceId)
        {
            if (_selectedEliteSkills.TryGetValue(choiceId, out var cached))
                return cached.skillData?.Id;

            return null;
        }

        /// <summary>카드 종류를 모를 때(런 세이브) 승격·구매 캐시를 차례로 본다. 스탯 카드는 null.</summary>
        public string GetCachedSkillId(string choiceId)
        {
            return GetCachedEliteSkillId(choiceId) ?? GetCachedAddSkillId(choiceId);
        }

        // 세이브가 남긴 카드의 부여 스킬을 캐시에 되돌린다 — 저장 당시 뽑힌 스킬이 그대로 카드에 보이게.
        private void _RestoreSelectedSkill(IChoiceOption option, string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return;

            var db = DatabaseManager.Instance?.GetTrainSkillDataDB();

            if (db == null)
                return;

            switch (option)
            {
                case EliteTrainChoice eliteChoice:
                {
                    var skill = _FindSkill(db, eliteChoice.EliteTrainDataId, skillId);

                    if (skill.skillData != null)
                        _selectedEliteSkills[eliteChoice.Id] = skill;

                    break;
                }
                case AddTrainChoice addChoice:
                {
                    var skill = _FindSkill(db, addChoice.TrainDataId, skillId);

                    if (skill.skillData != null)
                        _selectedAddSkills[addChoice.Id] = skill;

                    break;
                }
            }
        }

        private static (IData skillData, TrainChoiceSkillType skillType) _FindSkill(TrainSkillDataDB db, string trainDataId, string skillId)
        {
            foreach (var skill in db.GetSkillsForTrain(trainDataId))
            {
                if (skill.skillData != null && skill.skillData.Id == skillId)
                    return skill;
            }

            return (null, TrainChoiceSkillType.None);
        }
    }
}
