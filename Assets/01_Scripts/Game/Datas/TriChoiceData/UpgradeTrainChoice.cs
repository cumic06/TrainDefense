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
            // Train 초기 레벨은 -1, upgradeStats 배열은 0부터 시작
            // View 표시 및 업그레이드 적용 시: 레벨 + 1 인덱스 사용
            int currentLevelIndex = currentLevel + 1;

            bool hasValidUpgrade = weightedUpgrades.Any(w =>
            {
                var upgradeData = DatabaseManager.Instance.GetTrainUpgradeDataById(w?.UpgradeDataId);
                if (upgradeData == null) return false;

                // Train의 현재 레벨 인덱스가 업그레이드 데이터의 최대 레벨보다 크거나 같으면 더 이상 업그레이드 불가
                if (currentLevelIndex >= upgradeData.MaxLevel) return false;

                // 현재 레벨 인덱스에 해당하는 업그레이드 데이터가 있는지 확인
                return currentLevelIndex >= 0 && currentLevelIndex < upgradeData.MaxLevel;
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