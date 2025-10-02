using TrainDefense.Game.Data;
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

        [SerializeField]
        private TrainData trainData;

        [SerializeField]
        [Tooltip("업그레이드인 경우, TrainData의 Upgrades 리스트 인덱스 (0부터 시작)")]
        private int upgradeLevel;
        #endregion

        public string Id => id;
        public ChoiceType ChoiceType => choiceType;
        public TrainData TrainData => trainData;
        public int UpgradeLevel => upgradeLevel;

        /// <summary>
        /// 선택지 UI 정보 가져오기
        /// </summary>
        public ChoiceUIInfo GetUIInfo()
        {
            if (trainData == null)
            {
                Debug.LogError($"ChoiceOption [{id}]: TrainData is null");
                return default;
            }

            switch (choiceType)
            {
                case ChoiceType.AddTrain:
                    return new ChoiceUIInfo
                    {
                        Icon = trainData.Icon,
                        Name = trainData.TrainName,
                        Description = trainData.Description
                    };

                case ChoiceType.UpgradeTrain:
                    var upgradeInfo = trainData.GetUpgrade(upgradeLevel);
                    return new ChoiceUIInfo
                    {
                        Icon = upgradeInfo.Icon,
                        Name = upgradeInfo.UpgradeName,
                        Description = upgradeInfo.Description
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
            if (trainData == null) return false;

            switch (choiceType)
            {
                case ChoiceType.AddTrain:
                    // Train이 없을 때만 유효
                    return !TrainManager.Instance.CheckHasTrain(trainData);

                case ChoiceType.UpgradeTrain:
                    // Train이 있을 때만 유효
                    return TrainManager.Instance.CheckHasTrain(trainData);

                default:
                    return false;
            }
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

