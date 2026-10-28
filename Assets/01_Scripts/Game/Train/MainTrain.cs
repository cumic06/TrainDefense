using System.Collections;
using System.Linq;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Cumic.Events;
using TrainDefense.Game.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Manager;
using TrainDefense.Game.Stats;

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
      [BoxGroup("TrainSetting")]
      private bool isUnDead = false;

      [SerializeField]
      [BoxGroup("GameOverEffect")]
      private float gameOverSlowDuration = 2f;
      #endregion

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
         }
      }

      // 메인 기차는 무적이라 최대 체력 강화를 받지 않는다.
      protected override bool ReceivesMetaMaxHp => false;

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
         // 부서진 칸도 자리를 차지한다 — 회복 없는 난이도에선 상점에 부서진 칸이 남는다.
         if (_currentTrains.Count >= MaxTrainCount)
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

         Train trainObject = Instantiate(trainPrefab, transform);
         trainObject.Initialize(trainData, skillType, selectedSkillId);
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
         // 부서진 칸도 강화를 받는다. 수리하면 강화된 채로 돌아온다.
         Train upgradeTrain = _currentTrains.FirstOrDefault(train => train.TrainData.Id == targetTrainId);

         if (upgradeTrain != null)
         {
            upgradeTrain.Upgrade(upgradeData);
            // 런 세이브 복원용 이력. 레벨 숫자만으로는 어떤 업그레이드였는지 되돌릴 수 없다.
            _RecordUpgradeHistory(upgradeTrain, upgradeData);
         }
      }

      // 기존 Train을 새로운 Train으로 대체한다. (Elite Train 전환용)
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

         // 새로운 Train 생성 (기존 Train 제거 전에 생성하여 이벤트에서 참조 가능)
         Train newTrain = Instantiate(newTrainPrefab, transform);
         newTrain.Initialize(newTrainData, skillType, selectedSkillId);
         newTrain.IsUnDead = isUnDead;

         // 기존 트레인의 누적 강화(영구 상점 + 카드)를 새 인스턴스에 통째로 승계.
         // ApplyExistingUpgradesToTrain은 호출하지 않는다 — oldTrain의 currentStat에 이미 영구 업그레이드가 반영되어 있어
         // CopyProgressFrom의 delta가 영구 + 카드를 모두 옮긴다. 둘 다 호출하면 영구분이 중복 적용된다.
         // 상점 아이템(최종값 곱)은 delta로 옮길 수 없다 — 승계 계산에서 원본의 아이템 배율을 나눈 뒤, 새 기차에 다시 곱한다.
         // 개조 패시브(ApplyPassiveSkills)와 아이템은 같은 스탯이면 %가 합산된다(Train.AccumulateFinalValuePercent).
         var shopItemStats = GetShopItemStats();
         newTrain.CopyProgressFrom(oldTrain, shopItemStats);
         newTrain.ApplyPassiveSkills();
         newTrain.ApplyStatsToFinalValue(shopItemStats);

         // 런 세이브 복원용: 업그레이드 이력과 base 출처를 새 인스턴스로 승계한다.
         _TransferUpgradeHistory(oldTrain, newTrain, oldTrainId);

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

      /// <summary>
      /// 두 포탑의 편성 자리를 맞바꾼다(상점 위치 이동용). 순서 인덱스와 목록 순서를 함께 바꿔
      /// 이후 재정렬·런 세이브(CaptureFormation)가 바뀐 순서를 따르게 한다.
      /// 좌표는 두 칸끼리 교환만 하므로 상점 슬라이드 아웃 오프셋이 걸린 상태에서도 화면 밖 위치가 유지된다.
      /// </summary>
      public bool SwapTrainOrder(Train first, Train second)
      {
         if (first == null || second == null || first == second)
            return false;

         // 죽은 칸은 _deadTrains에 인덱스 사본이 따로 있어 맞바꾸지 않는다. (회복 없는 난이도에선 수리 후에 옮길 수 있다)
         if (first.IsDead || second.IsDead)
            return false;

         if (!_trainOriginalIndexMap.TryGetValue(first, out int firstIndex) || !_trainOriginalIndexMap.TryGetValue(second, out int secondIndex))
            return false;

         _trainOriginalIndexMap[first] = secondIndex;
         _trainOriginalIndexMap[second] = firstIndex;
         _SwapInList(_currentTrains, first, second);
         _SwapInList(_currentAliveTrains, first, second);

         Vector3 firstPosition = first.transform.localPosition;
         first.transform.localPosition = second.transform.localPosition;
         second.transform.localPosition = firstPosition;

         GameEventSystem.Publish(new TrainFormationReorderedEvent());

         return true;
      }

      private static void _SwapInList(List<Train> trains, Train first, Train second)
      {
         int firstIndex = trains.IndexOf(first);
         int secondIndex = trains.IndexOf(second);

         if (firstIndex < 0 || secondIndex < 0)
            return;

         trains[firstIndex] = second;
         trains[secondIndex] = first;
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
         // 회복 없는 난이도는 피해·부서진 칸을 그대로 상점에 들고 간다(상점 수리로만 회복).
         if (StageManager.HasStationArrivalRecovery)
            RepairAllTrains();

         foreach (var train in _currentAliveTrains)
         {
            train.ResetVisualForInspection();
         }

         // 모든 기차를 원래 순서대로 재정렬
         RearrangeAllTrainsToOriginalOrder();
      }

      /// <summary>산 포탑은 체력을 채우고 부서진 포탑은 되살린다. (역 도착 회복·상점 수리 공용)</summary>
      public void RepairAllTrains()
      {
         foreach (var train in _currentAliveTrains)
         {
            train.RestoreHpToMax();
         }

         ReviveAllDeadTrains();

         // 역 도착·상점 모두 timeScale=0 + SuppressSFX(true) 상태이므로 ignoreSuppress로 우회 재생한다.
         SoundManager.Instance?.PlaySFX(SoundType.SFX_Game_Heal, ignoreSuppress: true);
      }

      /// <summary>모든 포탑을 최대 체력의 ratio만큼 회복한다 — 부서진 포탑은 그 비율로 되살린다. (상점 수리 한 번)</summary>
      public void RepairTrainsByRatio(float ratio)
      {
         foreach (var train in _currentAliveTrains)
         {
            train.RestoreHpByRatio(ratio);
         }

         foreach (var deadTrainInfo in _deadTrains.ToList())
         {
            ReviveDeadTrain(deadTrainInfo);
            deadTrainInfo.Train.SetHpToRatio(ratio);
         }

         SoundManager.Instance?.PlaySFX(SoundType.SFX_Game_Heal, ignoreSuppress: true);
      }

      // 죽은(부서진) 기차를 모두 부활시켜 산 기차 목록으로 되돌린다. (Resurrect가 풀피로 되살린다)
      private void ReviveAllDeadTrains()
      {
         foreach (var deadTrainInfo in _deadTrains.ToList())
            ReviveDeadTrain(deadTrainInfo);
      }

      private void ReviveDeadTrain(DeadTrainInfo deadTrainInfo)
      {
         Train train = deadTrainInfo.Train;

         // HP 최대치로 복원 및 _isDead = false 설정 (레벨과 업그레이드는 유지)
         train.Resurrect();

         // 오브젝트 활성화
         train.gameObject.SetActive(true);

         // 부서질 때의 조준 각도 등을 지운다. 상점 수리로 살아난 칸은 역 도착 때의 일괄 초기화를 지나쳤다.
         train.ResetVisualForInspection();

         // _deadTrains에서 제거하고 _currentAliveTrains에 다시 추가
         _deadTrains.Remove(deadTrainInfo);
         _currentAliveTrains.Add(train);
      }

      // 부서진 칸도 보유로 본다 — 아니면 상점이 그 포탑의 강화 카드를 내리고 같은 포탑 구매 카드를 다시 낸다.
      public bool CheckHasTrainById(string trainId)
      {
         return _currentTrains.Any(train => train.TrainData.Id == trainId);
      }

      public void ApplyUpgrade(UpgradeData upgradeData, int newLevel, int prevLevel)
      {
         if (upgradeData == null)
            return;

         if (upgradeData.Stats == null || upgradeData.Stats.Length == 0)
            return;

         // 상점 아이템은 최종값에 곱한다(개조와 같은 경로). 같은 스탯의 % 합산은 Train(AccumulateFinalValuePercent)이 맡으므로
         // 이번 구매분의 %를 그대로 넘긴다.
         if (upgradeData.UpgradePool == UpgradePool.ShopItem)
         {
            var purchasedStats = _ScaleStats(upgradeData.Stats, newLevel - prevLevel);

            // 부서진 칸도 받는다(수리하면 아이템이 적용된 채로 돌아온다).
            foreach (var train in _currentTrains)
               train.ApplyStatsToFinalValue(purchasedStats);

            return;
         }

         foreach (var train in _currentTrains)
         {
            train.ApplyStatsLevelAware(upgradeData.Stats, newLevel, prevLevel);
         }
      }

      // 상점 아이템 총량 — 스탯마다 (값 × 구매 수)를 전부 더한 %. 새 포탑·승격·이어하기가 최종값 곱 경로(ApplyStatsToFinalValue)에 한 번에 넣는다.
      // 공격 간격은 음수가 빠름이라 그 경로의 역수 배율(1 ÷ (1 − p))과 맞는다.
      public static IStat[] GetShopItemStats()
      {
         var result = new List<IStat>();

         foreach (var pair in _GetShopItemTotals())
         {
            if (pair.Value != 0f)
               result.Add(new SimpleStat { Type = pair.Key, Value = pair.Value });
         }

         return result.ToArray();
      }

      // 구매 수만큼 곱한 % (같은 아이템을 한 번에 여러 개 받는 경우 대비).
      private static IStat[] _ScaleStats(IStat[] stats, int count)
      {
         var result = new List<IStat>();

         foreach (var stat in stats)
         {
            if (stat != null && stat.Value != 0f)
               result.Add(new SimpleStat { Type = stat.Type, Value = stat.Value * count });
         }

         return result.ToArray();
      }

      // 스탯별 합산 총량(%). 구매한 id만 돈다(DB 전체 배열 복사 없음).
      private static Dictionary<StatType, float> _GetShopItemTotals()
      {
         var totals = new Dictionary<StatType, float>();

         if (UserDataManager.Instance == null || DatabaseManager.Instance == null)
            return totals;

         foreach (var upgradeId in UserDataManager.Instance.GetAllUpgradeIds())
         {
            var upgradeData = DatabaseManager.Instance.GetUpgradeData(upgradeId);

            if (upgradeData == null || upgradeData.UpgradePool != UpgradePool.ShopItem || upgradeData.Stats == null)
               continue;

            int level = UserDataManager.Instance.GetUpgradeLevel(upgradeId);

            if (level <= 0)
               continue;

            foreach (var stat in upgradeData.Stats)
            {
               if (stat == null)
                  continue;

               totals.TryGetValue(stat.Type, out float current);
               totals[stat.Type] = current + stat.Value * level;
            }
         }

         return totals;
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

         // 레벨 누적 방식으로 적용: per-call 반올림 오차 방지
         foreach (var (stats, level) in GetExistingTrainUpgrades())
            train.ApplyStatsLevelAware(stats, level);

         // 상점 아이템은 최종값에 곱한다. 이어하기 복원 중엔 저장된 강화 증가량(아이템 배율을 뺀 크기)을 얹은 뒤
         // _RestoreFormationEntry가 곱한다 — 여기서 먼저 곱하면 증가량이 배율을 못 받는다.
         if (RunSave.RunSaveManager.Instance == null || !RunSave.RunSaveManager.Instance.IsRestoring)
            train.ApplyStatsToFinalValue(GetShopItemStats());
      }

      /// <summary>
      /// 지금까지 고른 판 중 전체 강화(TrainUpgrade 타입)의 스탯과 레벨. 새 포탑 생성 시 적용 목록이자,
      /// 상점 새 포탑 카드 미리보기(Train.GetPreviewStatDetails)가 같은 값을 계산하는 데 쓴다.
      /// </summary>
      public static List<(IStat[] stats, int level)> GetExistingTrainUpgrades()
      {
         var upgrades = new List<(IStat[] stats, int level)>();

         if (UserDataManager.Instance == null)
         {
            Debug.LogWarning("GetExistingTrainUpgrades: UserDataManager.Instance is null");
            return upgrades;
         }

         if (DatabaseManager.Instance == null)
         {
            Debug.LogWarning("GetExistingTrainUpgrades: DatabaseManager.Instance is null");
            return upgrades;
         }

         // UserDataManager._upgradeLevels dictionary의 모든 항목을 순회
         var upgradeIds = UserDataManager.Instance.GetAllUpgradeIds();
         if (upgradeIds == null)
         {
            Debug.LogWarning("GetExistingTrainUpgrades: GetAllUpgradeIds returned null");
            return upgrades;
         }

         foreach (var upgradeId in upgradeIds)
         {
            if (string.IsNullOrEmpty(upgradeId))
            {
               Debug.LogWarning("GetExistingTrainUpgrades: upgradeId is null or empty");
               continue;
            }

            // UserDataManager에서 해당 업그레이드의 레벨 확인
            // TrainUpgradeManager.OnStatUpgradeSelect에서 UserDataManager.Instance.UpgradeLevel(upgradeId)로 기록됨
            int upgradeLevel = UserDataManager.Instance.GetUpgradeLevel(upgradeId);

            if (upgradeLevel <= 0)
            {
               Debug.LogWarning($"GetExistingTrainUpgrades: upgradeLevel is {upgradeLevel} for upgradeId '{upgradeId}'");
               continue;
            }

            // UpgradeData 조회
            UpgradeData upgradeData = DatabaseManager.Instance.GetUpgradeData(upgradeId);
            if (upgradeData == null)
            {
               Debug.LogWarning($"GetExistingTrainUpgrades: UpgradeData not found for ID '{upgradeId}'");
               continue;
            }

            // TrainUpgrade 타입의 업그레이드만 적용. 상점 아이템은 최종값 곱이라 따로(GetShopItemStats) 다룬다.
            if (upgradeData.UpgradeDataType != UpgradeDataType.TrainUpgrade || upgradeData.UpgradePool == UpgradePool.ShopItem)
            {
               continue;
            }

            if (upgradeData.Stats == null || upgradeData.Stats.Length == 0)
            {
               Debug.LogWarning($"GetExistingTrainUpgrades: Stats is null or empty for upgradeId '{upgradeId}'");
               continue;
            }

            upgrades.Add((upgradeData.Stats, upgradeLevel));
         }

         return upgrades;
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
