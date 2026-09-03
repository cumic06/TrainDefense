using System.Collections.Generic;
using System.Linq;
using Cumic;
using Cumic.Events;
using Sirenix.OdinInspector;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.Tutorial;
using UnityEngine;

namespace TrainDefense.Game
{
    public class TrainUpgradeManager : Singleton<TrainUpgradeManager>
    {
        private bool _trainInfoSlotTutorialStarted = false;
        private bool _eliteTrainTutorialStarted = false;

        [ShowInInspector, ReadOnly, FoldoutGroup("디버그 - 상점 업그레이드")]
        private Dictionary<string, int> ShopUpgrades
        {
            get
            {
                if (UserDataManager.Instance == null) return null;
                return UserDataManager.Instance.GetAllUpgradeIds()
                    .ToDictionary(id => id, id => UserDataManager.Instance.GetUpgradeLevel(id));
            }
        }

        [ShowInInspector, ReadOnly, FoldoutGroup("디버그 - 기차 스탯")]
        private List<string> TrainStats
        {
            get
            {
                if (TrainManager.Instance == null) return null;
                return TrainManager.Instance.GetTrains()
                    .Where(t => t != null)
                    .Select(t => $"[{t.name}] Lv{t.CurrentLevel} | {t.GetStatSummary()}")
                    .ToList();
            }
        }

        private void Start()
        {
            GameEventSystem.Subscribe<StatUpgradeSelectEvent>(OnStatUpgradeSelect);
            GameEventSystem.Subscribe<AddTrainEvent>(OnAddTrain);
            GameEventSystem.Subscribe<ReplaceTrainEvent>(OnReplaceTrain);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<StatUpgradeSelectEvent>(OnStatUpgradeSelect);
            GameEventSystem.Unsubscribe<AddTrainEvent>(OnAddTrain);
            GameEventSystem.Unsubscribe<ReplaceTrainEvent>(OnReplaceTrain);

            GameEventSystem.Unsubscribe<MainTrainFiredEvent>(OnMainTrainFired);

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnTutorialComplete -= OnTrainInfoSlotTutorialComplete;
                TutorialManager.Instance.OnTutorialComplete -= _OnInspectionTimeTutorialComplete;
                TutorialManager.Instance.OnTutorialComplete -= OnMainTrainAttackTutorialComplete;
            }
        }

        private void OnStatUpgradeSelect(StatUpgradeSelectEvent statUpgradeSelectEvent)
        {
            if (statUpgradeSelectEvent == null || string.IsNullOrEmpty(statUpgradeSelectEvent.UpgradeId))
            {
                Debug.LogWarning("UpgradeManager: StatUpgradeSelectEvent is null or UpgradeId is empty");
                return;
            }

            string upgradeId = statUpgradeSelectEvent.UpgradeId;
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
            int newLevel = UserDataManager.Instance.GetUpgradeLevel(upgradeId);
            ApplyUpgrade(upgradeData, newLevel);

            // 업그레이드 적용 이후 UI 및 기타 시스템에 알려주기 위한 이벤트 발행
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
        }

        private void OnReplaceTrain(ReplaceTrainEvent replaceTrainEvent)
        {
            if (replaceTrainEvent?.NewTrain == null)
            {
                Debug.LogWarning("TrainUpgradeManager: ReplaceTrainEvent NewTrain is null");
                return;
            }

            // 트레인 교체는 엘리트 획득 경로(EliteTrainChoice)에서만 발생한다.
            CheckAndStartEliteTrainTutorial();
        }

        private void ApplyUpgrade(UpgradeData upgradeData, int newLevel)
        {
            if (upgradeData == null) return;

            switch (upgradeData.UpgradeDataType)
            {
                case UpgradeDataType.TrainUpgrade:
                    ApplyTrainUpgrade(upgradeData, newLevel);
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
        private void ApplyTrainUpgrade(UpgradeData upgradeData, int newLevel)
        {
            if (TrainManager.Instance == null || TrainManager.Instance.MainTrain == null)
            {
                Debug.LogWarning("UpgradeManager: TrainManager or MainTrain is null");
                return;
            }

            TrainManager.Instance.ApplyUpgrade(upgradeData, newLevel, newLevel - 1);
        }

        /// <summary>
        /// Train 외의 업그레이드 적용
        /// </summary>
        private void ApplyNonTrainUpgrade(UpgradeData upgradeData)
        {
            // 드롭 시점에 계산되므로 구매 시 별도 처리 없음
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

            TutorialManager.Instance.StartTutorial("trainInfoSlotTutorial");
        }

        private void OnTrainInfoSlotTutorialComplete(string sequenceId)
        {
            if (sequenceId != "trainInfoSlotTutorial") return;

            TutorialManager.Instance.OnTutorialComplete -= OnTrainInfoSlotTutorialComplete;
            TutorialManager.Instance.OnTutorialComplete += _OnInspectionTimeTutorialComplete;

            Debug.Log("TrainUpgradeManager: Starting inspectionTime tutorial");
            TutorialManager.Instance.StartTutorial("inspectionTimeTutorial");
        }

        private void _OnInspectionTimeTutorialComplete(string sequenceId)
        {
            if (sequenceId != "inspectionTimeTutorial") return;

            TutorialManager.Instance.OnTutorialComplete -= _OnInspectionTimeTutorialComplete;

            Debug.Log("TrainUpgradeManager: inspectionTimeTutorial complete → EngageStart");
            GameEventSystem.Publish(new EngageStartEvent());

            // 전투 시작 직후 메인 트레인 공격 튜토리얼 시작
            StartMainTrainAttackTutorial();
        }

        /// <summary>
        /// 메인 트레인 공격(화면 터치로 포탑 발사) 튜토리얼을 시작합니다.
        /// 실제로 발사(MainTrainFiredEvent)하면 완료됩니다.
        /// </summary>
        private void StartMainTrainAttackTutorial()
        {
            if (TutorialManager.Instance == null) return;
            if (TutorialManager.Instance.IsTutorialCompleted("mainTrainAttackTutorial")) return;

            GameEventSystem.Subscribe<MainTrainFiredEvent>(OnMainTrainFired);
            TutorialManager.Instance.OnTutorialComplete += OnMainTrainAttackTutorialComplete;

            if (!TutorialManager.Instance.StartTutorial("mainTrainAttackTutorial"))
            {
                // 시작 실패 시 구독 롤백
                GameEventSystem.Unsubscribe<MainTrainFiredEvent>(OnMainTrainFired);
                TutorialManager.Instance.OnTutorialComplete -= OnMainTrainAttackTutorialComplete;
            }
        }

        private void OnMainTrainFired(MainTrainFiredEvent firedEvent)
        {
            // 메인 트레인 공격 튜토리얼 진행 중 발사하면 완료 처리
            if (TutorialManager.Instance != null
                && TutorialManager.Instance.CurrentSequenceId == "mainTrainAttackTutorial")
            {
                TutorialManager.Instance.SkipCurrent();
            }
        }

        private void OnMainTrainAttackTutorialComplete(string sequenceId)
        {
            if (sequenceId != "mainTrainAttackTutorial") return;

            TutorialManager.Instance.OnTutorialComplete -= OnMainTrainAttackTutorialComplete;
            GameEventSystem.Unsubscribe<MainTrainFiredEvent>(OnMainTrainFired);
        }

        /// <summary>
        /// 엘리트 기차를 획득(트레인 교체)했을 때 엘리트 튜토리얼을 시작합니다.
        /// 시작했으면 true를 반환합니다.
        /// </summary>
        private bool CheckAndStartEliteTrainTutorial()
        {
            if (_eliteTrainTutorialStarted) return false;
            if (TutorialManager.Instance == null) return false;

            _eliteTrainTutorialStarted = true;
            Debug.Log("TrainUpgradeManager: Starting elite train tutorial");
            return TutorialManager.Instance.StartTutorial("eliteTrainTutorial");
        }
    }
}