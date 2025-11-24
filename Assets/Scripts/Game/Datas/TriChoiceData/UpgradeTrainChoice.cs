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

        private ITrainUpgradeData _selectedUpgrade;

        public string Id => id;
        public string TargetTrainId => targetTrainId;
        public ITrainUpgradeData SelectedUpgrade
        {
            get
            {
                if (_selectedUpgrade == null)
                {
                    var db = Resources.Load<DB>("Data/DB");
                    _selectedUpgrade = SelectRandomUpgrade(db);
                }
                return _selectedUpgrade;
            }
        }

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
                Name = _selectedUpgrade.Name,
                Description = _selectedUpgrade.Description
            };
        }

        public bool IsValid()
        {
            if (weightedUpgrades == null || weightedUpgrades.Length == 0) return false;
            if (string.IsNullOrEmpty(targetTrainId)) return false;
            
            var trainManager = TrainManager.Instance;
            if (trainManager == null || !trainManager.CheckHasTrainById(targetTrainId)) return false;

            // 현재 Train의 레벨에 해당하는 업그레이드 데이터가 있는지 확인
            var train = GetTargetTrain();
            if (train == null) return false;

            var db = Resources.Load<DB>("Data/DB");
            if (db == null) return false;

            // 현재 레벨에 맞는 업그레이드가 하나라도 있는지 확인
            bool hasValidUpgrade = weightedUpgrades.Any(w => 
            {
                var upgradeData = db.GetTrainUpgradeData(w?.UpgradeDataId);
                if (upgradeData == null) return false;
                
                // Train의 현재 레벨이 업그레이드 데이터의 최대 레벨보다 크거나 같으면 더 이상 업그레이드 불가
                if (train.CurrentLevel >= upgradeData.MaxLevel) return false;
                
                // 현재 레벨에 해당하는 업그레이드 데이터가 있는지 확인
                return upgradeData.Level == train.CurrentLevel;
            });

            return hasValidUpgrade;
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

            var main = TrainManager.Instance.MainTrain;
            if (main == null)
            {
                Debug.LogError("UpgradeTrainChoice: MainTrain is null");
                return;
            }

            main.UpgradeTrain(targetTrainId, _selectedUpgrade);
        }

        private ITrainUpgradeData SelectRandomUpgrade(DB db)
        {
            if (db == null) return null;
            if (weightedUpgrades == null || weightedUpgrades.Length == 0) return null;

            var train = GetTargetTrain();
            if (train == null) return null;

            // 현재 Train의 레벨에 맞는 업그레이드만 필터링
            var validUpgrades = weightedUpgrades
                .Select(w => new { Weight = w, UpgradeData = db.GetTrainUpgradeData(w?.UpgradeDataId) })
                .Where(x => 
                {
                    if (x.UpgradeData == null) return false;
                    
                    // Train의 현재 레벨이 업그레이드 데이터의 최대 레벨보다 크거나 같으면 제외
                    if (train.CurrentLevel >= x.UpgradeData.MaxLevel) return false;
                    
                    // 현재 레벨에 해당하는 업그레이드 데이터인지 확인
                    return x.UpgradeData.Level == train.CurrentLevel;
                })
                .ToList();

            if (validUpgrades.Count == 0)
            {
                Debug.LogWarning($"UpgradeTrainChoice [{id}]: No upgrade data found for Train [{targetTrainId}] at level [{train.CurrentLevel}]");
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

            float r = UnityEngine.Random.Range(0f, total);
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

        private Train GetTargetTrain()
        {
            var trainManager = TrainManager.Instance;
            if (trainManager == null || trainManager.MainTrain == null) return null;

            return trainManager.MainTrain.CurrentTrains.FirstOrDefault(train => train.TrainData.Id == targetTrainId);
        }
    }
}


