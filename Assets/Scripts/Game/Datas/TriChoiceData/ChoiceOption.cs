using System;
using System.Linq;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 선택지 타입
    /// </summary>
    public enum ChoiceType
    {
        AddTrain,       // Train 추가
        UpgradeTrain    // Train 업그레이드
    }

    /// <summary>
    /// 가중치를 가진 업그레이드 데이터
    /// </summary>
    [Serializable]
    public class WeightedUpgradeData
    {
        [SerializeField]
        [Tooltip("업그레이드 데이터")]
        private TrainUpgradeData upgradeData;

        [SerializeField]
        [Tooltip("선택 가중치 (높을수록 선택될 확률 증가)")]
        private float weight = 1f;

        public TrainUpgradeData UpgradeData => upgradeData;
        public float Weight => weight;
    }

    /// <summary>
    /// 3지선다 선택지 데이터 (단순화된 구조)
    /// </summary>
    [CreateAssetMenu(fileName = "ChoiceOption", menuName = "Data/ChoiceOption")]
    public class ChoiceOption : ScriptableObject
    {
        #region Fields
        [Header("Choice Settings")]
        [SerializeField]
        private string id;

        [SerializeField]
        private ChoiceType choiceType;

        [Header("Train Data (for AddTrain)")]
        [SerializeField]
        [Tooltip("소환할 Train 데이터 (AddTrain 타입일 때 필수)")]
        private TrainData trainData;

        [Header("Upgrade Data (for UpgradeTrain)")]
        [SerializeField]
        [Tooltip("업그레이드 데이터 목록 (가중치 기반 랜덤 선택)")]
        private WeightedUpgradeData[] weightedUpgrades;

        [SerializeField]
        [Tooltip("업그레이드 대상 Train의 ID")]
        private string targetTrainId;
        #endregion

        #region Runtime Fields
        /// <summary>
        /// 런타임에 선택된 업그레이드 데이터 (캐싱용)
        /// </summary>
        private TrainUpgradeData _selectedUpgradeData;
        #endregion

        public string Id => id;
        public ChoiceType ChoiceType => choiceType;
        public TrainData TrainData => trainData;
        public WeightedUpgradeData[] WeightedUpgrades => weightedUpgrades;
        public string TargetTrainId => targetTrainId;

        /// <summary>
        /// 선택지 초기화 (가중치 기반 랜덤 선택 수행)
        /// </summary>
        public void Initialize()
        {
            if (choiceType == ChoiceType.UpgradeTrain)
            {
                _selectedUpgradeData = SelectRandomUpgrade();
            }
        }

        /// <summary>
        /// 선택지 UI 정보 가져오기
        /// </summary>
        public ChoiceUIInfo GetUIInfo()
        {
            switch (choiceType)
            {
                case ChoiceType.AddTrain:
                    if (trainData == null)
                    {
                        Debug.LogError($"ChoiceOption [{id}]: TrainData is null");
                        return default;
                    }
                    return new ChoiceUIInfo
                    {
                        Icon = trainData.Icon,
                        Name = trainData.TrainName,
                        Description = trainData.Description
                    };

                case ChoiceType.UpgradeTrain:
                    if (_selectedUpgradeData == null)
                    {
                        Debug.LogError($"ChoiceOption [{id}]: SelectedUpgradeData is null. Did you call Initialize()?");
                        return default;
                    }

                    return new ChoiceUIInfo
                    {
                        Icon = _selectedUpgradeData.Icon,
                        Name = _selectedUpgradeData.UpgradeName,
                        Description = _selectedUpgradeData.Description
                    };

                default:
                    return default;
            }
        }

        /// <summary>
        /// 이 선택지가 현재 유효한지 확인
        /// </summary>
        public bool IsValid()
        {
            switch (choiceType)
            {
                case ChoiceType.AddTrain:
                    if (trainData == null) return false;
                    // Train이 없을 때만 유효
                    return !TrainManager.Instance.CheckHasTrain(trainData);

                case ChoiceType.UpgradeTrain:
                    if (weightedUpgrades == null || weightedUpgrades.Length == 0 || string.IsNullOrEmpty(targetTrainId))
                        return false;
                    // 대상 Train이 있을 때만 유효
                    return TrainManager.Instance.CheckHasTrainById(targetTrainId);

                default:
                    return false;
            }
        }

        /// <summary>
        /// 선택된 업그레이드 데이터 가져오기
        /// </summary>
        public TrainUpgradeData GetSelectedUpgradeData()
        {
            return _selectedUpgradeData;
        }

        /// <summary>
        /// 가중치 기반으로 랜덤 업그레이드 데이터 선택 (내부 메서드)
        /// </summary>
        private TrainUpgradeData SelectRandomUpgrade()
        {
            if (weightedUpgrades == null || weightedUpgrades.Length == 0)
            {
                Debug.LogError($"ChoiceOption [{id}]: WeightedUpgrades is empty");
                return null;
            }

            // 단일 항목인 경우 바로 반환
            if (weightedUpgrades.Length == 1)
            {
                return weightedUpgrades[0]?.UpgradeData;
            }

            // 총 가중치 계산
            float totalWeight = weightedUpgrades.Sum(w => w.Weight);
            if (totalWeight <= 0)
            {
                return weightedUpgrades[0]?.UpgradeData;
            }

            // 랜덤 값 생성 (0 ~ totalWeight)
            float randomValue = UnityEngine.Random.Range(0f, totalWeight);

            // 가중치에 따라 선택
            float cumulativeWeight = 0f;
            foreach (var weightedUpgrade in weightedUpgrades)
            {
                cumulativeWeight += weightedUpgrade.Weight;
                if (randomValue <= cumulativeWeight)
                {
                    var selected = weightedUpgrade.UpgradeData;
                    return selected;
                }
            }

            // fallback (이론상 도달하지 않음)
            return weightedUpgrades[^1]?.UpgradeData;
        }
    }

    /// <summary>
    /// 선택지 UI 표시용 정보
    /// </summary>
    public struct ChoiceUIInfo
    {
        public Sprite Icon;
        public string Name;
        public string Description;
    }
}

