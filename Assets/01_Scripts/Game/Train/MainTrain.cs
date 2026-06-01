using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Cumic.Events;
using TrainDefense.Game.Events;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
   public class MainTrain : Train
   {
      #region Field

      [SerializeField]
      private float moveSpeed;

      [SerializeField]
      [BoxGroup("TrainSetting")]
      private int maxTrainCount;
      [SerializeField]
      [BoxGroup("TrainSetting")]
      private float trainOffset;
      [SerializeField]
      [BoxGroup("TrainSetting")]
      [Tooltip("MainTrain의 비주얼 모델. 편성 길이에 맞춰 오른쪽으로 밀어 형태 중앙이 카메라 타깃(=MainTrain transform)과 일치하게 한다.")]
      private Transform trainModel;
      [SerializeField]
      [Header("테스트용")]
      [BoxGroup("TrainSetting")]
      private Train startTrainablePrefab;
      [SerializeField]
      [BoxGroup("TrainSetting")]
      private bool isUnDead = false;

      [SerializeField]
      [BoxGroup("GameOverEffect")]
      private float gameOverSlowDuration = 2f;
      #endregion

      private TrainChoiceSkillType _pendingSkillType = TrainChoiceSkillType.None;
      private string _pendingSelectedSkillId = null;
      private readonly List<Train> _currentAliveTrains = new();//살아있는 Train만 있는 목록
      public List<Train> CurrentAliveTrains => _currentAliveTrains;
      public int MaxTrainCount => maxTrainCount;

      private readonly List<Train> _currentTrains = new();//모든 Train 목록
      public List<Train> CurrentTrains => _currentTrains;

      public int CurrentTrainCount => _currentTrains.Count;

      private struct DeadTrainInfo
      {
         public Train Train;
         public int OriginalIndex;
      }

      private readonly List<DeadTrainInfo> _deadTrains = new();//죽은 Train 목록
      private readonly Dictionary<Train, int> _trainOriginalIndexMap = new();//Train의 원래 인덱스 매핑

      // Elite 교체로 소비된 base train ID. 한 번 들어가면 게임 세션 동안 영구.
      // TriChoice가 이 ID를 targetTrainId/replaceTrainId/trainDataId로 참조하는 카드는 모두 노출 차단.
      private readonly HashSet<string> _replacedTrainIds = new();
      public bool IsTrainIdReplaced(string trainId) => !string.IsNullOrEmpty(trainId) && _replacedTrainIds.Contains(trainId);

      protected override void Start()
      {
         base.Start();

         GameEventSystem.Subscribe<TriChoiceSelectEvent>(OnTriChoiceSelect);
         GameEventSystem.Subscribe<TrainDeadEvent>(CheckDeadTrain);
         GameEventSystem.Subscribe<InspectionStartEvent>(OnInspectionStart);

         if (!_initialized)
         {
            _currentAliveTrains.Clear();
            _currentTrains.Clear();
            _deadTrains.Clear();
            _trainOriginalIndexMap.Clear();
            _replacedTrainIds.Clear();

            if (startTrainablePrefab != null && startTrainablePrefab.TryGetComponent(out Train train))
            {
               SpawnTrain(startTrainablePrefab);
            }
         }
      }

      protected override void OnDestroy()
      {
         base.OnDestroy();
         Time.fixedDeltaTime = 0.02f;
         GameEventSystem.Unsubscribe<TriChoiceSelectEvent>(OnTriChoiceSelect);
         GameEventSystem.Unsubscribe<TrainDeadEvent>(CheckDeadTrain);
         GameEventSystem.Unsubscribe<InspectionStartEvent>(OnInspectionStart);
      }

      private void FixedUpdate()
      {
         if (_isDead)
            return;

         Move();
      }

      private void OnTriChoiceSelect(TriChoiceSelectEvent triChoiceSelectEvent)
      {
         var choice = triChoiceSelectEvent.ChoiceOption;
         if (choice == null)
            return;
         choice.Execute();
      }

      private void Move()
      {
         transform.Translate(Vector3.right * Time.deltaTime * moveSpeed);
      }

      [Button("SpawnTrain")]
      public void SpawnTrain(Train trainPrefab)
      {
         if (trainPrefab == null)
         {
            Debug.LogError("SpawnTrain: Train prefab is null");
            return;
         }

         SpawnTrain(DatabaseManager.Instance.GetTrainData(trainPrefab.Id));
      }

      public void SpawnTrain(TrainData trainData, TrainChoiceSkillType skillType = TrainChoiceSkillType.None, string selectedSkillId = null)
      {
         if (_currentAliveTrains.Count >= maxTrainCount)
         {
            Debug.LogWarning("Train count is max");
            return;
         }

         if (trainData == null || trainData.Prefab == null)
         {
            Debug.LogError("SpawnTrain: TrainData or prefab is null");
            return;
         }

         Train trainPrefab = trainData.Prefab.GetComponent<Train>();
         if (trainPrefab == null)
         {
            Debug.LogError($"SpawnTrain: Prefab for TrainData [{trainData.Id}] has no Train component");
            return;
         }

         _pendingSkillType = skillType;
         Train trainObject = Instantiate(trainPrefab, transform);
         trainObject.Initialize(trainData, _pendingSkillType, selectedSkillId);
         _pendingSkillType = TrainChoiceSkillType.None;
         trainObject.IsUnDead = isUnDead;
         _currentAliveTrains.Add(trainObject);
         _currentTrains.Add(trainObject);
         int originalIndex = _currentTrains.Count - 1;
         _trainOriginalIndexMap[trainObject] = originalIndex;

         // 새로 생성된 train에 기존 업그레이드 적용
         ApplyExistingUpgradesToTrain(trainObject);
         trainObject.ApplyPassiveSkills();

         GameEventSystem.Publish(new AddTrainEvent(trainData.Icon, trainObject));

         // 살아있는 기차 재정렬
         RearrangeTrains();
      }

      public void UpgradeTrain(string targetTrainId, ITrainUpgradeData upgradeData)
      {
         Train upgradeTrain = _currentAliveTrains.FirstOrDefault(train => train.TrainData.Id == targetTrainId);

         if (upgradeTrain != null)
         {
            upgradeTrain.Upgrade(upgradeData);
            GameEventSystem.Publish(new UpgradeTrainEvent(upgradeTrain, upgradeData));
         }
      }

      /// <summary>
      /// 기존 Train을 새로운 Train으로 대체합니다. (Elite Train 전환용)
      /// </summary>
      /// <param name="oldTrainId">대체할 기존 Train ID</param>
      /// <param name="newTrainPrefab">새로운 Train 프리팹</param>
      public void ReplaceTrain(string oldTrainId, Train newTrainPrefab)
      {
         if (newTrainPrefab == null)
         {
            Debug.LogError("ReplaceTrain: New Train prefab is null");
            return;
         }

         ReplaceTrain(oldTrainId, DatabaseManager.Instance.GetTrainData(newTrainPrefab.Id));
      }

      public void ReplaceTrain(string oldTrainId, TrainData newTrainData, TrainChoiceSkillType skillType = TrainChoiceSkillType.None, string selectedSkillId = null)
      {
         // 대체할 기존 Train 찾기
         Train oldTrain = _currentAliveTrains.FirstOrDefault(train => train.TrainData.Id == oldTrainId);
         if (oldTrain == null)
         {
            Debug.LogError($"ReplaceTrain: Train with ID [{oldTrainId}] not found");
            return;
         }

         // 기존 Train의 인덱스 저장
         int aliveIndex = _currentAliveTrains.IndexOf(oldTrain);
         int originalIndex = _trainOriginalIndexMap.ContainsKey(oldTrain) ? _trainOriginalIndexMap[oldTrain] : aliveIndex;
         Vector3 oldPosition = oldTrain.transform.localPosition;

         if (newTrainData == null || newTrainData.Prefab == null)
         {
            Debug.LogError($"ReplaceTrain: New TrainData is invalid for replacing [{oldTrainId}]");
            return;
         }

         Train newTrainPrefab = newTrainData.Prefab.GetComponent<Train>();
         if (newTrainPrefab == null)
         {
            Debug.LogError($"ReplaceTrain: Prefab for TrainData [{newTrainData.Id}] has no Train component");
            return;
         }

         _pendingSkillType = skillType;
         _pendingSelectedSkillId = selectedSkillId;
         // 새로운 Train 생성 (기존 Train 제거 전에 생성하여 이벤트에서 참조 가능)
         Train newTrain = Instantiate(newTrainPrefab, transform);
         newTrain.Initialize(newTrainData, _pendingSkillType, _pendingSelectedSkillId);
         _pendingSkillType = TrainChoiceSkillType.None;
         _pendingSelectedSkillId = null;
         newTrain.IsUnDead = isUnDead;

         // 기존 트레인의 누적 강화(영구 상점 + 카드)를 새 인스턴스에 통째로 승계.
         // ApplyExistingUpgradesToTrain은 호출하지 않는다 — oldTrain의 currentStat에 이미 영구 업그레이드가 반영되어 있어
         // CopyProgressFrom의 delta가 영구 + 카드를 모두 옮긴다. 둘 다 호출하면 영구분이 중복 적용된다.
         newTrain.CopyProgressFrom(oldTrain);
         newTrain.ApplyPassiveSkills();

         // Elite 생성에 소비된 base ID는 이후 TriChoice에서 영구 차단 (다른 Elite 변형 / base 업그레이드 / 재추가 모두 금지).
         _replacedTrainIds.Add(oldTrainId);

         // UI 업데이트 이벤트 발행 (oldTrain 참조가 유효한 동안)
         GameEventSystem.Publish(new ReplaceTrainEvent(oldTrain, newTrain, newTrainData?.Icon));

         // 기존 Train 제거
         _currentAliveTrains.Remove(oldTrain);
         _currentTrains.Remove(oldTrain);
         _trainOriginalIndexMap.Remove(oldTrain);
         Destroy(oldTrain.gameObject);

         // 기존 위치에 삽입
         _currentAliveTrains.Insert(aliveIndex, newTrain);
         if (originalIndex < _currentTrains.Count)
         {
            _currentTrains.Insert(originalIndex, newTrain);
         }
         else
         {
            _currentTrains.Add(newTrain);
         }
         _trainOriginalIndexMap[newTrain] = originalIndex;

         // 기존 위치 복원
         newTrain.transform.localPosition = oldPosition;

         // 살아있는 기차 재정렬
         RearrangeTrains();
      }

      private void CheckDeadTrain(TrainDeadEvent trainDeadEvent)
      {
         if (isUnDead)
            return;

         foreach (var train in _currentAliveTrains.ToList())
         {
            if (trainDeadEvent.Train == train)
            {
               _currentAliveTrains.Remove(train);

               // 원래 인덱스 가져오기
               int originalIndex = _trainOriginalIndexMap.ContainsKey(train) ? _trainOriginalIndexMap[train] : _currentAliveTrains.Count;

               // 오브젝트 비활성화
               train.gameObject.SetActive(false);

               // 죽은 기차 정보 저장
               _deadTrains.Add(new DeadTrainInfo
               {
                  Train = train,
                  OriginalIndex = originalIndex
               });

               // 살아있는 기차 재정렬
               RearrangeTrains();

               if (_currentAliveTrains.Count == 0)
               {
                  OnDead();
               }
               break;
            }
         }
      }

      private void RearrangeTrains()
      {
         int count = _currentAliveTrains.Count;
         float halfLength = count * trainOffset * 0.5f;

         ApplyMainTrainModelOffset(halfLength);

         // 살아있는 기차만 연속적으로 재정렬 (편성 중앙이 MainTrain transform에 오도록 오른쪽으로 halfLength 이동)
         for (int i = 0; i < count; i++)
         {
            Vector3 newPos = new Vector3(halfLength - trainOffset * (i + 1), 0f, 0f);
            _currentAliveTrains[i].transform.localPosition = newPos;
         }
      }

      private void ApplyMainTrainModelOffset(float halfLength)
      {
         if (trainModel == null)
            return;

         Vector3 modelPos = trainModel.localPosition;
         modelPos.x = halfLength;
         trainModel.localPosition = modelPos;
      }

      private void RearrangeAllTrainsToOriginalOrder()
      {
         // 모든 기차를 원래 순서대로 재정렬
         // _currentTrains와 _deadTrains를 합쳐서 원래 인덱스 순서로 정렬
         var allTrains = new List<(Train train, int originalIndex)>();

         // 살아있는 기차 추가
         foreach (var train in _currentAliveTrains)
         {
            if (_trainOriginalIndexMap.ContainsKey(train))
            {
               allTrains.Add((train, _trainOriginalIndexMap[train]));
            }
         }

         // 죽은 기차 추가
         foreach (var deadTrainInfo in _deadTrains)
         {
            allTrains.Add((deadTrainInfo.Train, deadTrainInfo.OriginalIndex));
         }

         // 원래 인덱스 순서로 정렬
         allTrains.Sort((a, b) => a.originalIndex.CompareTo(b.originalIndex));

         int total = allTrains.Count;
         float halfLength = total * trainOffset * 0.5f;

         ApplyMainTrainModelOffset(halfLength);

         // 정렬된 순서대로 위치 재설정 (편성 중앙이 MainTrain transform에 오도록 오른쪽으로 halfLength 이동)
         for (int i = 0; i < total; i++)
         {
            Vector3 newPos = new Vector3(halfLength - trainOffset * (i + 1), 0f, 0f);
            allTrains[i].train.transform.localPosition = newPos;
         }
      }

      private void OnInspectionStart(InspectionStartEvent inspectionStartEvent)
      {
         // 살아있는 기차도 Inspection 시작 시 HP를 최대치로 회복
         foreach (var train in _currentAliveTrains)
         {
            train.RestoreHpToMax();
         }

         // 죽은 기차 복원
         foreach (var deadTrainInfo in _deadTrains.ToList())
         {
            Train train = deadTrainInfo.Train;

            // HP 최대치로 복원 및 _isDead = false 설정 (레벨과 업그레이드는 유지)
            train.Resurrect();

            // 오브젝트 활성화
            train.gameObject.SetActive(true);

            // _deadTrains에서 제거하고 _currentTrains에 다시 추가
            _deadTrains.Remove(deadTrainInfo);
            _currentAliveTrains.Add(train);
         }

         // 모든 기차를 원래 순서대로 재정렬
         RearrangeAllTrainsToOriginalOrder();

         // 점검(상점 진입) 시 체력 회복 효과음 1회.
         // 이 시점은 timeScale=0 + SuppressSFX(true) 상태이므로 ignoreSuppress로 우회 재생한다.
         SoundManager.Instance?.PlaySFX(SoundType.SFX_Game_Heal, ignoreSuppress: true);
      }

      public bool CheckHasTrain(TrainData trainData)
      {
         return _currentAliveTrains.Any(train => train.TrainData.Id == trainData.Id);
      }

      public bool CheckHasTrainById(string trainId)
      {
         return _currentAliveTrains.Any(train => train.TrainData.Id == trainId);
      }

      public void ApplyUpgrade(UpgradeData upgradeData, int newLevel, int prevLevel)
      {
         if (upgradeData == null)
            return;

         if (upgradeData.Stats == null || upgradeData.Stats.Length == 0)
            return;

         foreach (var train in _currentAliveTrains)
         {
            train.ApplyStatsLevelAware(upgradeData.Stats, newLevel, prevLevel);
         }
      }

      /// <summary>
      /// 새로 생성된 train에 UserDataManager._upgradeLevels에 저장된 상점 업그레이드를 적용합니다.
      /// UpgradeManager.ApplyTrainUpgrade와 동일한 방식으로 작동합니다.
      /// </summary>
      private void ApplyExistingUpgradesToTrain(Train train)
      {
         if (train == null)
         {
            Debug.LogWarning("ApplyExistingUpgradesToTrain: train is null");
            return;
         }

         if (UserDataManager.Instance == null)
         {
            Debug.LogWarning("ApplyExistingUpgradesToTrain: UserDataManager.Instance is null");
            return;
         }

         if (DatabaseManager.Instance == null)
         {
            Debug.LogWarning("ApplyExistingUpgradesToTrain: DatabaseManager.Instance is null");
            return;
         }

         // UserDataManager._upgradeLevels dictionary의 모든 항목을 순회
         var upgradeIds = UserDataManager.Instance.GetAllUpgradeIds();
         if (upgradeIds == null)
         {
            Debug.LogWarning("ApplyExistingUpgradesToTrain: GetAllUpgradeIds returned null");
            return;
         }

         int totalAppliedCount = 0;
         foreach (var upgradeId in upgradeIds)
         {
            if (string.IsNullOrEmpty(upgradeId))
            {
               Debug.LogWarning("ApplyExistingUpgradesToTrain: upgradeId is null or empty");
               continue;
            }

            // UserDataManager에서 해당 업그레이드의 레벨 확인
            // UpgradeManager.OnBuyShopItem에서 UserDataManager.Instance.UpgradeLevel(upgradeId)로 기록됨
            int upgradeLevel = UserDataManager.Instance.GetUpgradeLevel(upgradeId);

            if (upgradeLevel <= 0)
            {
               Debug.LogWarning($"ApplyExistingUpgradesToTrain: upgradeLevel is {upgradeLevel} for upgradeId '{upgradeId}'");
               continue;
            }

            // UpgradeData 조회
            UpgradeData upgradeData = DatabaseManager.Instance.GetUpgradeData(upgradeId);
            if (upgradeData == null)
            {
               Debug.LogWarning($"ApplyExistingUpgradesToTrain: UpgradeData not found for ID '{upgradeId}'");
               continue;
            }

            // TrainUpgrade 타입의 업그레이드만 적용
            if (upgradeData.UpgradeDataType != UpgradeDataType.TrainUpgrade)
            {
               continue;
            }

            if (upgradeData.Stats == null || upgradeData.Stats.Length == 0)
            {
               Debug.LogWarning($"ApplyExistingUpgradesToTrain: Stats is null or empty for upgradeId '{upgradeId}'");
               continue;
            }

            // 레벨 누적 방식으로 적용: per-call 반올림 오차 방지
            train.ApplyStatsLevelAware(upgradeData.Stats, upgradeLevel);

            totalAppliedCount++;
         }

         if (totalAppliedCount > 0)
         {
         }
      }

      public void UndeadTrain()
      {
         isUnDead = true;
      }

      protected override void OnDead()
      {
         if (isUnDead)
            return;

         base.OnDead();
         GameEventSystem.Publish(new GameOverStartEvent(gameOverSlowDuration, transform));
      }
   }
}
