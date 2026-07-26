using System.Collections;
using System.Linq;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Cumic.Events;
using TrainDefense.Game.Events;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
   public partial class MainTrain : Train
   {
      // 영구 업그레이드 '자가 복구': 5초마다 살아있는 전 포탑을 동시에 일정 % 회복하는 중앙 타이머.
      private const float HealthRegenInterval = 5f;
      private float _healthRegenTimer;

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
      public int MaxTrainCount => maxTrainCount + Mathf.RoundToInt(
         (PermanentUpgradeManager.Instance != null
            ? PermanentUpgradeManager.Instance.GetValue(PermanentUpgradeType.MaxTurretCount)
            : 0f)
         + (SkillTreeManager.Instance != null
            ? SkillTreeManager.Instance.GetValue(SkillTreePassiveType.MaxTurretCount)
            : 0f));

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

      protected override void Update()
      {
         base.Update();
         _TickHealthRegen(Time.deltaTime);
         _UpdateWeaponInput();
      }

      // 자가 복구 레벨이 있으면 5초마다 살아있는 모든 포탑을 한 번에 회복. (메인 기차는 _currentAliveTrains에 미포함이라 자동 제외)
      private void _TickHealthRegen(float deltaTime)
      {
         if (_isDead) return;

         float regenPercent = 0f;

         var permanentUpgradeManager = PermanentUpgradeManager.Instance;
         if (permanentUpgradeManager != null)
            regenPercent += permanentUpgradeManager.GetValue(PermanentUpgradeType.HealthRegen);

         var skillTreeManager = SkillTreeManager.Instance;
         if (skillTreeManager != null)
            regenPercent += skillTreeManager.GetValue(SkillTreePassiveType.HealthRegen);

         if (regenPercent <= 0f) return;

         _healthRegenTimer += deltaTime;
         if (_healthRegenTimer < HealthRegenInterval) return;

         _healthRegenTimer -= HealthRegenInterval;

         float ratio = regenPercent / 100f;
         foreach (var train in _currentAliveTrains)
            train.RestoreHpByRatio(ratio);
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
         if (_currentAliveTrains.Count >= MaxTrainCount)
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
         // 도감 발견 기록용. 이 트레인을 처음 만들면 발견 처리된다.
         GameEventSystem.Publish(new TrainSpawnedEvent(trainData.Id));

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
         // 엘리트 전환 등으로 만들어진 새 트레인도 도감에 발견 처리한다.
         GameEventSystem.Publish(new TrainSpawnedEvent(newTrainData.Id));

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

               // 사라지지 않고 회색(흑백)으로 전환 — 원래 자리에 그대로 남긴다.
               train.ApplyDeadVisual();

               // 죽은 기차 정보 저장
               _deadTrains.Add(new DeadTrainInfo
               {
                  Train = train,
                  OriginalIndex = originalIndex
               });

               // 죽은 기차도 원래 자리를 유지하도록 산 기차·죽은 기차를 함께 원래 순서로 재정렬
               RearrangeAllTrainsToOriginalOrder();

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
         // 죽은 칸도 회색으로 자리를 유지하는 설계이므로, 산 칸만 따로(count 기준) 배치하면 죽은 칸과 중앙
         // 기준(halfLength)이 어긋나 위치가 틀어진다(죽은 칸 위로 산 칸이 겹치거나 밀림). 산 칸·죽은 칸을 함께
         // 원래 순서로 배치하는 RearrangeAllTrainsToOriginalOrder 단일 경로로 통일해 정렬 기준을 일치시킨다.
         RearrangeAllTrainsToOriginalOrder();
      }

      private void ApplyMainTrainModelOffset(float halfLength)
      {
         if (trainModel != null)
         {
            Vector3 modelPos = trainModel.localPosition;
            modelPos.x = halfLength;
            trainModel.localPosition = modelPos;
         }

         // 주무기 마운트도 기차 비주얼과 같은 x로 맞춰 포탑이 항상 기차(trainModel) 중앙 위에 오게 한다.
         // (trainModel만 halfLength로 밀면 turretMount는 루트 원점(x=0)에 남아 포탑이 기차에서 어긋난다.)
         if (turretMount != null)
         {
            Vector3 mountPos = turretMount.localPosition;
            mountPos.x = halfLength;
            turretMount.localPosition = mountPos;
         }
      }

      public void RearrangeAllTrainsToOriginalOrder()
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
         foreach (var train in _currentAliveTrains)
         {
            train.RestoreHpToMax();
         }

         ReviveAllDeadTrains();

         // 이 시점은 timeScale=0 + SuppressSFX(true) 상태이므로 ignoreSuppress로 우회 재생한다.
         SoundManager.Instance?.PlaySFX(SoundType.SFX_Game_Heal, ignoreSuppress: true);

         // 상점 진입 시 자기강화(시한버프)를 즉시 해제하고 액티브 스킬 쿨타임을 초기화한다.
         // (timeScale=0이라 스킬 Tick이 멈춰 버프가 다음 맵까지 유지되고, Time.time 기반 쿨타임이
         //  진행되지 않아 상점을 다녀와도 쿨타임이 그대로 남던 문제 해결)
         foreach (var train in _currentAliveTrains)
         {
            train.ResetSkillStateForInspection();
         }

         // 모든 기차를 원래 순서대로 재정렬
         RearrangeAllTrainsToOriginalOrder();
      }

      // 죽은(부서진) 기차를 모두 부활시켜 산 기차 목록으로 되돌린다.
      // revivedHpRatio가 0보다 크면 부활 직후 HP를 최대 체력의 그 비율로 맞춘다(0이면 Resurrect의 풀피 유지).
      private void ReviveAllDeadTrains(float revivedHpRatio = 0f)
      {
         foreach (var deadTrainInfo in _deadTrains.ToList())
         {
            Train train = deadTrainInfo.Train;

            // HP 최대치로 복원 및 _isDead = false 설정 (레벨과 업그레이드는 유지)
            train.Resurrect();

            // 오브젝트 활성화
            train.gameObject.SetActive(true);

            // _deadTrains에서 제거하고 _currentAliveTrains에 다시 추가
            _deadTrains.Remove(deadTrainInfo);
            _currentAliveTrains.Add(train);

            if (revivedHpRatio > 0f)
               train.SetHpToRatio(revivedHpRatio);
         }
      }

      /// <summary>
      /// 긴급 수리: 부서진 기차를 모두 부활시키고, (부활 전부터) 살아있던 기차를 aliveHealRatio만큼 회복한다.
      /// 부활시킨 기차의 HP는 revivedHpRatio로 맞춘다.
      /// </summary>
      public void EmergencyRepair(float aliveHealRatio, float revivedHpRatio)
      {
         // 부활 전 기존 생존 기차만 비율 회복 (부활한 기차는 ReviveAllDeadTrains에서 HP를 따로 설정)
         foreach (var train in _currentAliveTrains.ToList())
         {
            train.RestoreHpByRatio(aliveHealRatio);
         }

         ReviveAllDeadTrains(revivedHpRatio);

         RearrangeAllTrainsToOriginalOrder();

         SoundManager.Instance?.PlaySFX(SoundType.SFX_Game_Heal, ignoreSuppress: true);
      }

      // 수리(회복/부활)가 의미 있는 상태인가 — 죽은 기차가 있거나 손상된 생존 기차가 있으면 true.
      // (긴급 수리 카드가 풀피 상태에서 죽은 카드로 뜨지 않도록 노출 조건에 사용)
      public bool HasRepairTarget => _deadTrains.Count > 0
         || _currentAliveTrains.Any(train => train != null && train.CurrentHpRatio < 1f);

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

         // 메인 터렛(선택 주무기)도 편성 포탑과 동일하게 상점 업그레이드를 받는다.
         _turret?.ApplyShopStats(upgradeData.Stats, newLevel, prevLevel);
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
            // TrainUpgradeManager.OnStatUpgradeSelect에서 UserDataManager.Instance.UpgradeLevel(upgradeId)로 기록됨
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

      // SlideOut으로 화면 밖에 보낸 대상들의 원래 localPosition.x(정위치). ResetSlidePosition으로 복귀할 때 사용.
      private readonly List<(Transform target, float originX)> _slideOutOrigins = new();

      // 슬라이드 연출(상점/맵 이동)은 산 기차뿐 아니라 죽은(회색) 기차도 함께 움직여야 한다.
      // 죽은 기차는 _currentAliveTrains에서 빠지고 _deadTrains에 남으므로 둘을 합쳐 순회한다.
      private IEnumerable<Train> _EnumerateAllTrains()
      {
         foreach (var train in _currentAliveTrains)
         {
            if (train != null)
               yield return train;
         }

         foreach (var deadTrainInfo in _deadTrains)
         {
            if (deadTrainInfo.Train != null)
               yield return deadTrainInfo.Train;
         }
      }

      public void SlideIn(float delay, float duration, float? screenLeftXOverride = null)
      {
         Camera cam = Camera.main;
         // screenLeftXOverride: 연출 중 카메라가 줌/이동 중이라 '현재' 화면 왼쪽 경계가 의도와 다를 때, 기차의 시작 기준이 될 화면 왼쪽 X를 직접 지정한다.
         float camLeftX = screenLeftXOverride ?? (cam != null ? cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, 0f)).x : -10f);
         float rightmostX = transform.position.x;
         foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
             rightmostX = Mathf.Max(rightmostX, sr.bounds.max.x);
         float offsetX = camLeftX - rightmostX;

         foreach (var train in _EnumerateAllTrains())
         {
            float targetX = train.transform.localPosition.x;
            train.transform.localPosition += new Vector3(offsetX, 0f, 0f);
            train.transform.DOLocalMoveX(targetX, duration)
               .SetDelay(delay)
               .SetEase(Ease.InOutSine)
               .SetUpdate(true);
         }

         if (trainModel != null)
         {
            float targetX = trainModel.localPosition.x;
            trainModel.localPosition += new Vector3(offsetX, 0f, 0f);
            trainModel.DOLocalMoveX(targetX, duration)
               .SetDelay(delay)
               .SetEase(Ease.InOutSine)
               .SetUpdate(true);
         }

         // 주무기 마운트(포탑)도 기차와 함께 슬라이드 인. (누락 시 연출 중 포탑만 제자리에 남는다.)
         if (turretMount != null)
         {
            float targetX = turretMount.localPosition.x;
            turretMount.localPosition += new Vector3(offsetX, 0f, 0f);
            turretMount.DOLocalMoveX(targetX, duration)
               .SetDelay(delay)
               .SetEase(Ease.InOutSine)
               .SetUpdate(true);
         }
      }

      /// <summary>
      /// 정위치에서 전진 방향(+X, 화면 오른쪽 밖)으로 슬라이드 아웃(열차 출발 연출). 출발 전 위치를 _slideOutOrigins에 저장하며,
      /// 연출이 끝나면 <see cref="ResetSlidePosition"/>으로 정위치에 즉시 복귀시킨다.
      /// </summary>
      public void SlideOut(float delay, float duration, float extraOffsetX = 0f)
      {
         Camera cam = Camera.main;
         // 전진 방향(+X, 화면 오른쪽)으로 출발: 기차의 가장 왼쪽 끝이 화면 오른쪽 경계에 닿도록 밀어낸다.
         // extraOffsetX: 연출 중 카메라가 오른쪽으로 따라 이동하면 그만큼 더 밀어내, 기차가 최종 화면 밖까지 완전히 나가게 한다.
         float camRightX = cam != null ? cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, 0f)).x : 10f;
         float leftmostX = transform.position.x;
         foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
             leftmostX = Mathf.Min(leftmostX, sr.bounds.min.x);
         float offsetX = camRightX - leftmostX + extraOffsetX;

         _slideOutOrigins.Clear();

         foreach (var train in _EnumerateAllTrains())
         {
            _slideOutOrigins.Add((train.transform, train.transform.localPosition.x));
            train.transform.DOLocalMoveX(train.transform.localPosition.x + offsetX, duration)
               .SetDelay(delay)
               .SetEase(Ease.InOutSine)
               .SetUpdate(true);
         }

         if (trainModel != null)
         {
            _slideOutOrigins.Add((trainModel, trainModel.localPosition.x));
            trainModel.DOLocalMoveX(trainModel.localPosition.x + offsetX, duration)
               .SetDelay(delay)
               .SetEase(Ease.InOutSine)
               .SetUpdate(true);
         }

         // 주무기 마운트(포탑)도 기차와 함께 슬라이드 아웃. ResetSlidePosition이 _slideOutOrigins로 자동 복귀시킨다.
         if (turretMount != null)
         {
            _slideOutOrigins.Add((turretMount, turretMount.localPosition.x));
            turretMount.DOLocalMoveX(turretMount.localPosition.x + offsetX, duration)
               .SetDelay(delay)
               .SetEase(Ease.InOutSine)
               .SetUpdate(true);
         }
      }

      /// <summary>
      /// <see cref="SlideOut"/>으로 화면 밖에 보낸 대상들을 정위치로 즉시 복귀시킨다.
      /// (페이드로 화면이 가려진 동안 호출해 순간이동이 보이지 않게 한다.)
      /// </summary>
      public void ResetSlidePosition()
      {
         foreach (var (target, originX) in _slideOutOrigins)
         {
            if (target == null) continue;
            target.DOKill();
            Vector3 p = target.localPosition;
            target.localPosition = new Vector3(originX, p.y, p.z);
         }
         _slideOutOrigins.Clear();
      }

      public void UndeadTrain()
      {
         isUnDead = true;
      }

      // 메인 기차(엔진)는 무적이다. 적이 보조 포탑을 조준해 쏜 투사체라도
      // 경로상 메인 기차 콜라이더에 닿으면 데미지가 들어오는데(Train은 IProjectileTarget이고
      // GetNearTrain만 메인을 제외할 뿐 투사체 트리거 충돌은 막지 못함), 이때 메인 기차가 죽으면
      // 보조 포탑이 멀쩡해도 게임오버가 발생하는 버그가 있었다.
      // 게임오버는 보조 포탑이 모두 전멸했을 때(CheckDeadTrain)만 발생해야 하므로 메인 기차는 피해를 무시한다.
      public override void TakeDamage(float damage, bool isCritical)
      {
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
