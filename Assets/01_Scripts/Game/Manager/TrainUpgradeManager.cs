using Cumic;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.Tutorial;
using UnityEngine;

namespace TrainDefense.Game
{
    public class TrainUpgradeManager : Singleton<TrainUpgradeManager>
    {
        private bool _skillTutorialStarted = false;
        private bool _trainInfoSlotTutorialStarted = false;

        private void Start()
        {
            GameEventSystem.Subscribe<BuyShopItemEvent>(OnBuyShopItem);
            GameEventSystem.Subscribe<AddTrainEvent>(OnAddTrain);
            GameEventSystem.Subscribe<ReplaceTrainEvent>(OnReplaceTrain);
            GameEventSystem.Subscribe<UpgradeTrainEvent>(OnUpgradeTrain);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<BuyShopItemEvent>(OnBuyShopItem);
            GameEventSystem.Unsubscribe<AddTrainEvent>(OnAddTrain);
            GameEventSystem.Unsubscribe<ReplaceTrainEvent>(OnReplaceTrain);
            GameEventSystem.Unsubscribe<UpgradeTrainEvent>(OnUpgradeTrain);

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnTutorialComplete -= OnTrainInfoSlotTutorialComplete;
            }
        }

        private void OnBuyShopItem(BuyShopItemEvent buyShopItemEvent)
        {
            if (buyShopItemEvent == null || string.IsNullOrEmpty(buyShopItemEvent.UpgradeId))
            {
                Debug.LogWarning("UpgradeManager: BuyShopItemEvent is null or UpgradeId is empty");
                return;
            }

            string upgradeId = buyShopItemEvent.UpgradeId;
            UpgradeData upgradeData = DatabaseManager.Instance.GetUpgradeData(upgradeId);

            if (upgradeData == null)
            {
                Debug.LogWarning($"UpgradeManager: UpgradeData not found for ID '{upgradeId}'");
                return;
            }

            int currentLevel = UserDataManager.Instance.GetUpgradeLevel(upgradeId);
            if (upgradeData.MaxUpgradeCount > 0 && currentLevel >= upgradeData.MaxUpgradeCount)
            {
                Debug.LogWarning($"UpgradeManager: Max upgrade count reached for '{upgradeId}'");
                return;
            }

            UserDataManager.Instance.UpgradeLevel(upgradeId);
            ApplyUpgrade(upgradeData);

            // 업그레이드된 기차 중 스킬이 있으면 튜토리얼 시작
            if (upgradeData.UpgradeDataType == UpgradeDataType.TrainUpgrade)
            {
                CheckAndStartSkillTutorial();
            }

            // 업그레이드 적용 이후 UI 및 기타 시스템에 알려주기 위한 이벤트 발행
            int newLevel = UserDataManager.Instance.GetUpgradeLevel(upgradeId);
            GameEventSystem.Publish(new UpgradeAppliedEvent(upgradeId, newLevel));
        }

        private void OnAddTrain(AddTrainEvent addTrainEvent)
        {
            if (addTrainEvent?.Train == null)
            {
                Debug.LogWarning("TrainUpgradeManager: AddTrainEvent Train is null");
                return;
            }

            // 첫 기차 추가 시 TrainInfoSlot → InspectionTime 튜토리얼 연쇄 시작
            CheckAndStartTrainInfoSlotTutorial();

            // 추가된 기차가 스킬을 가지면 튜토리얼 시작
            if (addTrainEvent.Train.HasSkill)
            {
                CheckAndStartSkillTutorial(addTrainEvent.Train);
            }
        }

        private void OnReplaceTrain(ReplaceTrainEvent replaceTrainEvent)
        {
            if (replaceTrainEvent?.NewTrain == null)
            {
                Debug.LogWarning("TrainUpgradeManager: ReplaceTrainEvent NewTrain is null");
                return;
            }

            Debug.Log($"TrainUpgradeManager: ReplaceTrain - {replaceTrainEvent.NewTrain.name}, HasSkill: {replaceTrainEvent.NewTrain.HasSkill}");

            // 대체된 새 기차가 스킬을 가지면 튜토리얼 시작
            if (replaceTrainEvent.NewTrain.HasSkill)
            {
                CheckAndStartSkillTutorial(replaceTrainEvent.NewTrain);
            }
        }

        private void OnUpgradeTrain(UpgradeTrainEvent upgradeTrainEvent)
        {
            if (upgradeTrainEvent?.Train == null)
            {
                Debug.LogWarning("TrainUpgradeManager: UpgradeTrainEvent Train is null");
                return;
            }

            // 업그레이드된 기차가 스킬을 가지면 튜토리얼 시작
            if (upgradeTrainEvent.Train.HasSkill)
            {
                CheckAndStartSkillTutorial(upgradeTrainEvent.Train);
            }
        }

        private void ApplyUpgrade(UpgradeData upgradeData)
        {
            if (upgradeData == null) return;

            switch (upgradeData.UpgradeDataType)
            {
                case UpgradeDataType.TrainUpgrade:
                    ApplyTrainUpgrade(upgradeData);
                    break;
                case UpgradeDataType.NonTrainUpgrade:
                    ApplyNonTrainUpgrade(upgradeData);
                    break;
                default:
                    Debug.LogWarning($"UpgradeManager: Unknown upgrade type '{upgradeData.UpgradeDataType}' for '{upgradeData.Id}'");
                    break;
            }
        }

        /// <summary>
        /// Train 관련 업그레이드 적용
        /// </summary>
        private void ApplyTrainUpgrade(UpgradeData upgradeData)
        {
            if (TrainManager.Instance == null || TrainManager.Instance.MainTrain == null)
            {
                Debug.LogWarning("UpgradeManager: TrainManager or MainTrain is null");
                return;
            }

            TrainManager.Instance.ApplyUpgrade(upgradeData);
        }

        /// <summary>
        /// Train 외의 업그레이드 적용
        /// </summary>
        private void ApplyNonTrainUpgrade(UpgradeData upgradeData)
        {
            // NonTrainUpgrade 타입의 업그레이드 처리 로직 추가 가능
            Debug.Log($"UpgradeManager: NonTrainUpgrade applied for '{upgradeData.Id}'");
        }

        /// <summary>
        /// 첫 기차 추가 시 TrainInfoSlot 튜토리얼을 시작합니다.
        /// 완료 후 InspectionTime 튜토리얼로 연쇄 진행합니다.
        /// </summary>
        private void CheckAndStartTrainInfoSlotTutorial()
        {
            if (_trainInfoSlotTutorialStarted) return;
            if (TutorialManager.Instance == null) return;

            _trainInfoSlotTutorialStarted = true;
            TutorialManager.Instance.OnTutorialComplete += OnTrainInfoSlotTutorialComplete;

            Debug.Log("TrainUpgradeManager: Starting trainInfoSlot tutorial");
            TutorialManager.Instance.StartTutorial("trainInfoSlotTutorial");
        }

        private void OnTrainInfoSlotTutorialComplete(string sequenceId)
        {
            if (sequenceId != "trainInfoSlotTutorial") return;

            TutorialManager.Instance.OnTutorialComplete -= OnTrainInfoSlotTutorialComplete;

            Debug.Log("TrainUpgradeManager: Starting inspectionTime tutorial");
            TutorialManager.Instance.StartTutorial("inspectionTimeTutorial");
        }

        /// <summary>
        /// 스킬이 있는 기차가 있으면 튜토리얼을 시작합니다.
        /// </summary>
        private void CheckAndStartSkillTutorial(Train trainToCheck = null)
        {
            // 이미 튜토리얼을 시작했으면 중복 실행 방지
            if (_skillTutorialStarted)
            {
                Debug.LogWarning("TrainUpgradeManager: Skill tutorial already started");
                return;
            }

            // 1. 전달된 기차가 스킬을 가지면 튜토리얼 시작
            if (trainToCheck != null && trainToCheck.HasSkill)
            {
                StartSkillTutorial();
                return;
            }

            // 2. 전달된 기차가 없으면 전체 기차 검사
            if (TrainManager.Instance == null)
            {
                Debug.LogWarning("TrainUpgradeManager: TrainManager.Instance is null");
                return;
            }

            var trains = TrainManager.Instance.GetTrains();
            if (trains == null || trains.Length == 0)
            {
                Debug.LogWarning("TrainUpgradeManager: No trains found");
                return;
            }

            // 스킬을 가진 기차가 있는지 확인
            foreach (var train in trains)
            {
                if (train != null && train.HasSkill)
                {
                    StartSkillTutorial();
                    return;
                }
            }

            Debug.LogWarning("TrainUpgradeManager: No trains with skills found");
        }

        /// <summary>
        /// 스킬 튜토리얼을 시작합니다.
        /// </summary>
        private void StartSkillTutorial()
        {
            _skillTutorialStarted = true;
            if (TutorialManager.Instance != null)
            {
                Debug.Log("TrainUpgradeManager: Starting skill tutorial");
                TutorialManager.Instance.StartTutorial("skillTutorial");
            }
            else
            {
                Debug.LogError("TrainUpgradeManager: TutorialManager.Instance is null");
            }
        }
    }
}