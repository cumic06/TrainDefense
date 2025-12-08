using System;
using System.Linq;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class UpgradeTrainChoice : IChoiceOption
    {
        #region Fields
        [SerializeField]
        private string id;

        [SerializeField]
        [Tooltip("업그레이드 대상 Train ID")]
        private string targetTrainId;

        [SerializeField]
        [Tooltip("업그레이드 데이터 목록 (가중치 기반)")]
        private WeightedUpgradeData[] weightedUpgrades;
        #endregion

        public string Id => id;
        public string TargetTrainId => targetTrainId;
        public WeightedUpgradeData[] WeightedUpgrades => weightedUpgrades;

        public bool IsValid()
        {
            if (weightedUpgrades == null || weightedUpgrades.Length == 0) return false;
            if (string.IsNullOrEmpty(targetTrainId)) return false;

            var trainManager = TrainManager.Instance;
            if (trainManager == null || !trainManager.CheckHasTrainById(targetTrainId)) return false;

            // 현재 Train의 레벨에 해당하는 업그레이드 데이터가 있는지 확인
            var train = trainManager.MainTrain?.CurrentTrains.FirstOrDefault(t => t.TrainData.Id == targetTrainId);
            if (train == null) return false;

            // 현재 레벨에 맞는 업그레이드가 하나라도 있는지 확인
            int currentLevel = train.CurrentLevel;
            bool hasValidUpgrade = weightedUpgrades.Any(w =>
            {
                var upgradeData = DatabaseManager.Instance.GetTrainUpgradeDataById(w?.UpgradeDataId);
                if (upgradeData == null) return false;

                // Train의 현재 레벨이 업그레이드 데이터의 최대 레벨보다 크거나 같으면 더 이상 업그레이드 불가
                if (currentLevel >= upgradeData.MaxLevel) return false;

                // 현재 레벨에 해당하는 업그레이드 데이터가 있는지 확인
                // upgradeStats 배열의 인덱스 = 레벨 (CurrentLevel은 0-based)
                return currentLevel >= 0 && currentLevel < upgradeData.MaxLevel;
            });

            return hasValidUpgrade;
        }

        public void Execute()
        {
            var triChoiceManager = TriChoiceManager.Instance;
            if (triChoiceManager == null)
            {
                Debug.LogError("TriChoiceManager.Instance is null");
                return;
            }

            var selectedUpgrade = triChoiceManager.GetSelectedUpgrade(this);
            if (selectedUpgrade == null)
            {
                Debug.LogError($"UpgradeTrainChoice [{id}]: SelectedUpgradeData is null");
                return;
            }

            var main = TrainManager.Instance.MainTrain;
            if (main == null)
            {
                Debug.LogError("UpgradeTrainChoice: MainTrain is null");
                return;
            }

            main.UpgradeTrain(targetTrainId, selectedUpgrade);
        }
    }
}