using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
    public partial class TriChoiceManager
    {
        public ITrainUpgradeData GetSelectedUpgrade(UpgradeTrainChoice choice)
        {
            if (choice == null)
                return null;

            if (_selectedUpgrades.TryGetValue(choice.Id, out var selectedUpgrade))
            {
                if (_IsUpgradeValidForCurrentLevel(choice, selectedUpgrade))
                {
                    return selectedUpgrade;
                }
                else
                {
                    // 저장된 업그레이드가 현재 레벨에 맞지 않으면 캐시 제거
                    _selectedUpgrades.Remove(choice.Id);
                }
            }

            selectedUpgrade = _SelectRandomUpgrade(choice);

            if (selectedUpgrade != null)
            {
                _selectedUpgrades[choice.Id] = selectedUpgrade;
            }

            return selectedUpgrade;
        }

        private bool _IsUpgradeValidForCurrentLevel(UpgradeTrainChoice choice, ITrainUpgradeData upgradeData)
        {
            bool hasNullArgs = choice == null || upgradeData == null;

            if (hasNullArgs)
                return false;

            var train = _GetTargetTrain(choice.TargetTrainId);

            if (train == null)
                return false;

            // 다음에 적용할 upgradeStats 인덱스 = 현재 레벨 (레벨 = 받은 업그레이드 횟수)
            int currentLevelIndex = train.CurrentLevel;

            if (currentLevelIndex >= upgradeData.MaxLevel)
                return false;

            bool isValidIndex = currentLevelIndex >= 0 && currentLevelIndex < upgradeData.MaxLevel;

            return isValidIndex;
        }

        private ITrainUpgradeData _SelectRandomUpgrade(UpgradeTrainChoice choice)
        {
            if (choice == null)
                return null;

            bool hasNoWeightedUpgrades = choice.WeightedUpgrades == null || choice.WeightedUpgrades.Length == 0;

            if (hasNoWeightedUpgrades)
                return null;

            var train = _GetTargetTrain(choice.TargetTrainId);

            if (train == null)
                return null;

            // 다음에 적용할 upgradeStats 인덱스 = 현재 레벨 (레벨 = 받은 업그레이드 횟수)
            int currentLevelIndex = train.CurrentLevel;

            var validUpgrades = choice.WeightedUpgrades
                .Select(w => new { Weight = w, UpgradeData = DatabaseManager.Instance.GetTrainUpgradeDataById(w?.UpgradeDataId) })
                .Where(x =>
                {
                    if (x.UpgradeData == null)
                        return false;

                    if (currentLevelIndex >= x.UpgradeData.MaxLevel)
                        return false;

                    bool isValidIndex = currentLevelIndex >= 0 && currentLevelIndex < x.UpgradeData.MaxLevel;

                    return isValidIndex;
                })
                .ToList();

            if (validUpgrades.Count == 0)
            {
                Debug.LogWarning($"UpgradeTrainChoice [{choice.Id}]: No upgrade data found for Train [{choice.TargetTrainId}] at level (Index: {currentLevelIndex})");

                return null;
            }

            if (validUpgrades.Count == 1)
            {
                return validUpgrades[0].UpgradeData;
            }

            float total = validUpgrades.Sum(x => x.Weight.UpgradeDataWeight);

            if (total <= 0)
            {
                return validUpgrades[0].UpgradeData;
            }

            float r = Random.Range(0f, total);
            float acc = 0f;

            foreach (var item in validUpgrades)
            {
                acc += item.Weight.UpgradeDataWeight;

                if (r <= acc)
                {
                    return item.UpgradeData;
                }
            }

            return validUpgrades[^1].UpgradeData;
        }

        private Train _GetTargetTrain(string targetTrainId)
        {
            var trainManager = TrainManager.Instance;

            if (trainManager == null || trainManager.MainTrain == null)
                return null;

            return trainManager.MainTrain.CurrentTrains.FirstOrDefault(train => train.TrainData.Id == targetTrainId);
        }
    }
}
