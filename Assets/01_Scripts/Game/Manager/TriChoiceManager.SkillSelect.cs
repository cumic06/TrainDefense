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

            var db = DatabaseManager.Instance?.GetTrainSkillDataDB();

            if (db == null)
                return (null, TrainChoiceSkillType.None);

            var picked = db.GetRandomSkillForTrain(choice.EliteTrainDataId);

            if (picked.skillData != null)
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
    }
}
