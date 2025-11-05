using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class TriChoiceDB
    {
        #region Fields
        [Header("Train 추가 선택지")]
        [SerializeField]
        private List<ChoiceEntry> addTrainChoices = new();

        [Header("Train 업그레이드 선택지")]
        [SerializeField]
        private List<ChoiceEntry> upgradeTrainChoices = new();
        #endregion

        public IReadOnlyList<ChoiceEntry> AddTrainChoices => addTrainChoices;
        public IReadOnlyList<ChoiceEntry> UpgradeTrainChoices => upgradeTrainChoices;

        public IChoiceOption RandomChoice()
        {
            if (this == null)
            {
                Debug.LogError("TriChoiceDB not found");
                return null;
            }

            // 각 카테고리에서 유효한 선택지만 필터링
            List<ChoiceEntry> validAddTrain = GetValidChoices(AddTrainChoices);
            List<ChoiceEntry> validUpgradeTrain = GetValidChoices(UpgradeTrainChoices);

            // 유효한 카테고리 수집
            List<List<ChoiceEntry>> validCategories = new();
            if (validAddTrain.Count > 0) validCategories.Add(validAddTrain);
            if (validUpgradeTrain.Count > 0) validCategories.Add(validUpgradeTrain);

            if (validCategories.Count == 0)
            {
                Debug.LogWarning("No valid choices available");
                return null;
            }

            // 카테고리 중 하나를 균등 확률로 선택
            List<ChoiceEntry> selectedCategory = validCategories[Random.Range(0, validCategories.Count)];

            // 선택된 카테고리 내에서 가중치 기반 선택
            IChoiceOption selectedOption = SelectFromChoices(selectedCategory);

            // 선택지 초기화 (업그레이드 가중치 랜덤 선택)
            if (selectedOption != null)
            {
                var db = Resources.Load<DB>("Data/DB");
                selectedOption.Initialize(db);
            }

            return selectedOption;
        }

        private List<ChoiceEntry> GetValidChoices(IReadOnlyList<ChoiceEntry> entries)
        {
            List<ChoiceEntry> validChoices = new();

            foreach (var entry in entries)
            {
                if (entry.Option != null && entry.Option.IsValid())
                {
                    validChoices.Add(entry);
                }
            }

            return validChoices;
        }

        private IChoiceOption SelectFromChoices(List<ChoiceEntry> choices)
        {
            if (choices.Count == 0) return null;
            if (choices.Count == 1) return choices[0].Option;

            int totalWeight = 0;
            foreach (var entry in choices)
            {
                totalWeight += entry.Weight;
            }

            // 가중치가 모두 0이면 균등 확률로 선택
            if (totalWeight <= 0)
            {
                return choices[Random.Range(0, choices.Count)].Option;
            }

            // 가중치 기반 랜덤 선택
            int randomValue = Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var entry in choices)
            {
                currentWeight += entry.Weight;

                if (randomValue < currentWeight)
                {
                    return entry.Option;
                }
            }

            return choices[^1].Option;
        }
    }
}