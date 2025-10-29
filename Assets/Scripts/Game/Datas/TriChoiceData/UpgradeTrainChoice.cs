using System;
using System.Linq;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class UpgradeTrainChoice : IChoiceOption
    {
        [SerializeField]
        private string id;

        [SerializeField]
        [Tooltip("업그레이드 대상 Train ID")]
        private string targetTrainId;

        [SerializeField]
        [Tooltip("업그레이드 데이터 목록 (가중치 기반)")]
        private WeightedUpgradeData[] weightedUpgrades;

        [NonSerialized]
        private TrainUpgradeData _selectedUpgrade;

        public string Id => id;

        public void Initialize(DB db)
        {
            _selectedUpgrade = SelectRandomUpgrade(db);
        }

        public ChoiceUIInfo GetUIInfo()
        {
            if (_selectedUpgrade == null)
            {
                var db = Resources.Load<DB>("Data/DB");
                _selectedUpgrade = SelectRandomUpgrade(db);
            }

            if (_selectedUpgrade == null)
            {
                Debug.LogError($"UpgradeTrainChoice [{id}]: SelectedUpgradeData is null");
                return default;
            }

            return new ChoiceUIInfo
            {
                Icon = _selectedUpgrade.Icon,
                Name = _selectedUpgrade.UpgradeName,
                Description = _selectedUpgrade.Description
            };
        }

        public bool IsValid()
        {
            if (weightedUpgrades == null || weightedUpgrades.Length == 0) return false;
            if (string.IsNullOrEmpty(targetTrainId)) return false;
            return TrainDefense.Game.TrainManager.Instance.CheckHasTrainById(targetTrainId);
        }

        public void Execute()
        {
            if (_selectedUpgrade == null)
            {
                var db = Resources.Load<DB>("Data/DB");
                _selectedUpgrade = SelectRandomUpgrade(db);
            }

            if (_selectedUpgrade == null)
            {
                Debug.LogError($"UpgradeTrainChoice [{id}]: SelectedUpgradeData is null");
                return;
            }

            var main = TrainDefense.Game.TrainManager.Instance.MainTrain;
            if (main == null)
            {
                Debug.LogError("UpgradeTrainChoice: MainTrain is null");
                return;
            }

            main.UpgradeTrain(targetTrainId, _selectedUpgrade);
        }

        private TrainUpgradeData SelectRandomUpgrade(DB db)
        {
            if (db == null) return null;
            if (weightedUpgrades == null || weightedUpgrades.Length == 0) return null;

            if (weightedUpgrades.Length == 1)
            {
                return db.GetTrainUpgradeData(weightedUpgrades[0]?.UpgradeDataId);
            }

            float total = weightedUpgrades.Sum(w => w.UpgradeDataWeight);
            if (total <= 0)
            {
                return db.GetTrainUpgradeData(weightedUpgrades[0]?.UpgradeDataId);
            }

            float r = UnityEngine.Random.Range(0f, total);
            float acc = 0f;
            foreach (var w in weightedUpgrades)
            {
                acc += w.UpgradeDataWeight;
                if (r <= acc)
                {
                    return db.GetTrainUpgradeData(w.UpgradeDataId);
                }
            }

            return db.GetTrainUpgradeData(weightedUpgrades[^1]?.UpgradeDataId);
        }
    }
}


