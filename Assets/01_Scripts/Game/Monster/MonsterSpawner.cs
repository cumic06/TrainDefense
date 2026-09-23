using System.Collections;
using System.Collections.Generic;
using Cumic;
using Cumic.Events;
using Sirenix.OdinInspector;
using TrainDefense;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game
{
   public class MonsterSpawner : Singleton<MonsterSpawner>
   {
      #region Field
      [SerializeField]
      [BoxGroup("SpawnSetting")]
      private float spawnInterval;
      [SerializeField]
      [BoxGroup("SpawnSetting")]
      [Tooltip("한 spawnInterval마다 소환할 몬스터 마리 수 (기본 1)")]
      private int spawnCount = 1;
      [SerializeField]
      [BoxGroup("SpawnSetting")]
      [Tooltip("spawnCount 상한")]
      private int maxSpawnCount = 4;
      [SerializeField]
      [BoxGroup("SpawnSetting")]
      [Tooltip("spawnCount가 처음 +1 되는 검문(역 도착) 횟수")]
      private int spawnCountFirstIncreaseStation = 2;
      [SerializeField]
      [BoxGroup("SpawnSetting")]
      [Tooltip("첫 증가 이후 spawnCount가 다시 +1 되는 검문 간격")]
      private int spawnCountIncreaseStationInterval = 3;
      [SerializeField]
      [BoxGroup("SpawnSetting")]
      [Tooltip("검문(역 도착) 1회당 스폰 간격 단축 %")]
      private float stationSpawnAccelPercent = 3f;
      [SerializeField]
      [BoxGroup("SpawnSetting")]
      [Tooltip("스폰 간격 단축 상한 %")]
      private float maxSpawnAccelPercent = 60f;
      [SerializeField]
      [BoxGroup("SpawnSetting")]
      [Tooltip("CameraBased: 메인 카메라가 실제로 보는 화면 가장자리 바깥에 스폰(줌·종횡비가 바뀌어도 항상 화면 밖).\n" +
               "CustomArea: 기차 기준 고정 오프셋 영역에 스폰.\n" +
               "맵 데이터(MapData.customSpawnAreas)에 전용 영역이 있으면 모드와 무관하게 그 영역을 우선 사용한다.")]
      private MonsterSpawnType spawnMode = MonsterSpawnType.CameraBased;

      [SerializeField]
      [BoxGroup("SpawnSetting")]
      [ShowIf("spawnMode", MonsterSpawnType.CameraBased)]
      [Tooltip("CameraBased: 화면 가장자리에서 바깥으로 이만큼 떨어진 곳부터 스폰(월드 단위). 몬스터가 화면 안에서 튀어나오지 않게 하는 여백.")]
      private float spawnRange;
      [SerializeField]
      [BoxGroup("SpawnSetting")]
      [ShowIf("spawnMode", MonsterSpawnType.CameraBased)]
      [Tooltip("CameraBased: spawnRange 바깥으로 이어지는 스폰 띠의 두께. 이 두께 안에서 랜덤 위치에 스폰(0이면 선 위에만).")]
      private float spawnDepth = 1f;
      [SerializeField]
      [BoxGroup("SpawnSetting")]
      [ShowIf("spawnMode", MonsterSpawnType.CameraBased)]
      [EnumToggleButtons]
      [Tooltip("CameraBased: 스폰할 화면 가장자리(복수 선택). 매 스폰마다 선택된 가장자리 중 하나를 균등 확률로 고른다. 아무것도 없으면 Top+Bottom으로 동작.")]
      private SpawnEdge spawnEdges = SpawnEdge.Top | SpawnEdge.Bottom;

      [SerializeField]
      [BoxGroup("SpawnSetting")]
      [ShowIf("spawnMode", MonsterSpawnType.CustomArea)]
      private List<SpawnAreaInfo> customSpawnAreas;

      [System.Serializable]
      public struct SpawnAreaInfo
      {
         public Vector2 Offset;
         public Vector2 Size;
      }

      // CameraBased 모드에서 스폰할 화면 가장자리. 플래그라 복수 선택 가능.
      [System.Flags]
      public enum SpawnEdge
      {
         None = 0,
         Top = 1 << 0,
         Bottom = 1 << 1,
         Left = 1 << 2,
         Right = 1 << 3
      }

      private static readonly SpawnEdge[] AllSpawnEdges = { SpawnEdge.Top, SpawnEdge.Bottom, SpawnEdge.Left, SpawnEdge.Right };
      private const SpawnEdge DefaultSpawnEdges = SpawnEdge.Top | SpawnEdge.Bottom;

      #endregion

      [ShowInInspector]
      private bool _stopSpawnMonster;

      private StageSpawnData[] _currentSpawnDatas;
      // 씬에 설정된 공통 스폰 영역. 첫 SetSpawnRule 때 보존하고, 맵별 영역이 없을 때 이 값으로 복귀.
      private List<SpawnAreaInfo> _defaultSpawnAreas;
      // 현재 맵이 전용 스폰 영역(MapData.customSpawnAreas)을 지정했는지. true면 spawnMode와 무관하게 그 영역(기차 기준)을 쓴다.
      private bool _usingMapSpawnAreas;
      // 현재 맵의 스폰 이펙트(없으면 null). 몬스터 스폰 시 spawnPos에 생성.
      private GameObject _currentSpawnEffect;

      [ShowInInspector]
      private readonly List<Monster> _spawnedMonsters = new();

      private float _originalSpawnInterval;
      private int _originalSpawnCount;
      private int _stationPassedCount = 0;

      private EliteData _eliteData;
      // 엘리트 구간(역) 예산. 정산 때 구간시간 ÷ (eliteSpawnCycle × 스폰간격)을 적립해 정수부만
      // 이번 구간에 배치하고, 소수 잔여분은 다음 구간으로 이월한다(스테이지가 바뀌어도 이월).
      [ShowInInspector]
      private float _eliteBudget;
      // 이번 구간에서 엘리트를 승격할 구간 경과 시각(초) 목록. (N+1)등분 지점, 앞에서부터 소비.
      [ShowInInspector, ReadOnly]
      private readonly List<float> _eliteSpawnSchedule = new();
      // 검문 직후 true — 스폰 재개 후 첫 틱에 정산한다(맵 변경 검문에서 새 맵 간격·구간시간 반영).
      private bool _eliteSettlePending;
      private float _currentSegmentDuration;
      // ChangeStageTimeEvent(남은 시간)로 매 프레임 갱신되는 현재 구간 경과 시각.
      private float _currentSegmentElapsed;

      #region UnityLifeCycle
      private void Start()
      {
         _originalSpawnInterval = spawnInterval;
         _originalSpawnCount = spawnCount;
         StopSpawnMonster();
         GameEventSystem.Subscribe<GameEnterEvent>(OnGameEnter);
         GameEventSystem.Subscribe<MonsterRushEvent>(OnMonsterRush);
         GameEventSystem.Subscribe<InspectionStartEvent>(OnInspectionStart);
         GameEventSystem.Subscribe<TriChoiceSelectEvent>(OnTriChoiceSelect);
         GameEventSystem.Subscribe<ChangeStageTimeEvent>(OnChangeStageTime);
      }

      private void OnDestroy()
      {
         GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
         GameEventSystem.Unsubscribe<MonsterRushEvent>(OnMonsterRush);
         GameEventSystem.Unsubscribe<InspectionStartEvent>(OnInspectionStart);
         GameEventSystem.Unsubscribe<TriChoiceSelectEvent>(OnTriChoiceSelect);
         GameEventSystem.Unsubscribe<ChangeStageTimeEvent>(OnChangeStageTime);
      }
      #endregion

      private void OnGameEnter(GameEnterEvent gameEnterEvent)
      {
         if (DatabaseManager.Instance == null) return;

         _eliteData = DatabaseManager.Instance.GetEliteData();
         _eliteBudget = 0f;
         _eliteSpawnSchedule.Clear();
         _eliteSettlePending = true;
         _stationPassedCount = 0;
         spawnCount = _originalSpawnCount;
         StartCoroutine(SpawnMonster());
      }

      private void OnInspectionStart(InspectionStartEvent inspectionStartEvent)
      {
         DestroyAllMonsters();
         _stationPassedCount++;
         _UpdateSpawnCountByStation();
         spawnInterval = _GetAcceleratedInterval();

         // 미발동 엘리트는 예산으로 환급하고, 다음 구간 정산을 예약한다.
         _eliteBudget += _eliteSpawnSchedule.Count;
         _eliteSpawnSchedule.Clear();
         _eliteSettlePending = true;
      }

      private void OnChangeStageTime(ChangeStageTimeEvent changeStageTimeEvent)
      {
         _currentSegmentElapsed = _currentSegmentDuration - changeStageTimeEvent.StageTime;
      }

      // 검문(역 도착) 누적 횟수로 spawnCount를 결정한다.
      // spawnCountFirstIncreaseStation번째 검문에 처음 +1, 이후 spawnCountIncreaseStationInterval 검문마다 +1, maxSpawnCount 상한.
      private void _UpdateSpawnCountByStation()
      {
         int increase = 0;
         if (_stationPassedCount >= spawnCountFirstIncreaseStation)
            increase = spawnCountIncreaseStationInterval > 0
               ? 1 + (_stationPassedCount - spawnCountFirstIncreaseStation) / spawnCountIncreaseStationInterval
               : 1;
         spawnCount = Mathf.Min(_originalSpawnCount + increase, maxSpawnCount);
      }

      private void OnTriChoiceSelect(TriChoiceSelectEvent triChoiceSelectEvent)
      {
         if (_stopSpawnMonster)
         {
            StartSpawnMonster();
         }
      }

      public void SetSpawnRule(StageSpawnData[] spawnDatas, float monsterSpawnInterval, List<SpawnAreaInfo> mapSpawnAreas = null, GameObject mapSpawnEffect = null)
      {
         _currentSpawnDatas = spawnDatas;
         _originalSpawnInterval = monsterSpawnInterval;
         // 스폰풀/인터벌만 갱신(레벨업·스테이지 변경 시 호출). 역 누적 가속은 유지하고 현재 가속을 반영.
         spawnInterval = _GetAcceleratedInterval();

         // 맵별 영역(MapData.CustomSpawnAreas)이 있으면 그걸 쓰고(spawnMode 무관 우선), 없으면 씬 기본(공통 규칙)으로 복귀.
         _defaultSpawnAreas ??= customSpawnAreas != null ? new List<SpawnAreaInfo>(customSpawnAreas) : new List<SpawnAreaInfo>();
         _usingMapSpawnAreas = mapSpawnAreas != null && mapSpawnAreas.Count > 0;
         customSpawnAreas = _usingMapSpawnAreas ? mapSpawnAreas : _defaultSpawnAreas;

         // 맵별 스폰 이펙트(MapData.SpawnEffectPrefab). null이면 이펙트 없음.
         _currentSpawnEffect = mapSpawnEffect;
      }

      public void StartSpawnMonster()
      {
         _stopSpawnMonster = false;
      }

      public void StopSpawnMonster()
      {
         _stopSpawnMonster = true;
      }

      private void OnMonsterRush(MonsterRushEvent monsterRushEvent)
      {
         spawnInterval = _GetAcceleratedInterval() * monsterRushEvent.SpawnTimeMultiplier;
      }

      private float _GetAcceleratedInterval()
      {
         float totalAccel = Mathf.Min(_stationPassedCount * stationSpawnAccelPercent / 100f,
                                       maxSpawnAccelPercent / 100f);
         return _originalSpawnInterval * (1f - totalAccel);
      }

      //private void SpawnBoss(string bossMonsterId)
      //{
      //    if (string.IsNullOrEmpty(bossMonsterId)) return;

      //    MonsterData bossData = DatabaseManager.Instance.GetMonsterData(bossMonsterId);
      //    if (bossData == null)
      //    {
      //        Debug.LogWarning($"MonsterSpawner: Boss monster data not found for ID: {bossMonsterId}");
      //        return;
      //    }

      //    Vector3 spawnPos = RandomSpawnPos();
      //    GameObject bossObject = ResourceManager.Instance.Spawn(bossData.Prefab, spawnPos, parent: transform);

      //    // Boss 컴포넌트 우선, 없으면 Monster 컴포넌트 사용
      //    Monster bossMonster = bossObject.GetComponent<Monster>();

      //    if (bossMonster != null)
      //    {
      //        bossMonster.Initialize(bossData);
      //        _spawnedMonsters.Add(bossMonster);
      //        Debug.Log($"[MonsterSpawner] Boss spawned: {bossMonsterId}");

      //        // 보스도 몬스터 스폰 이벤트 발행
      //        GameEventSystem.Publish(new MonsterSpawnedEvent(bossMonsterId, true));
      //    }
      //}

      private IEnumerator SpawnMonster()
      {
         while (true)
         {
            if (_stopSpawnMonster)
            {
               yield return new WaitUntil(() => !_stopSpawnMonster);
            }

            WaitForSeconds spawnWait = new(spawnInterval);

            if (_currentSpawnDatas == null || _currentSpawnDatas.Length == 0)
            {
               yield return spawnWait;
               continue;
            }

            // 엘리트 스폰: 구간(역) 예산 스케줄 방식. 검문 후 첫 틱에 정산(_ScheduleEliteSpawns)하고,
            // 배치된 구간 경과 시각을 지나면 그 틱의 첫 슬롯을 엘리트로 승격한다.
            if (_eliteSettlePending)
            {
               _ScheduleEliteSpawns();
               _eliteSettlePending = false;
            }

            bool spawnEliteThisTick = false;
            if (_eliteSpawnSchedule.Count > 0 && _currentSegmentElapsed >= _eliteSpawnSchedule[0])
            {
               _eliteSpawnSchedule.RemoveAt(0);
               spawnEliteThisTick = true;
            }

            // 한 spawnInterval마다 spawnCount 마리를 각자 다른 위치·몬스터로 소환
            for (int spawnIndex = 0; spawnIndex < Mathf.Max(1, spawnCount); spawnIndex++)
            {
               Vector3 spawnPos = RandomSpawnPos();

               if (_currentSpawnEffect != null)
               {
                  ResourceManager.Instance.Spawn(_currentSpawnEffect, spawnPos, parent: transform);
               }

               // 가중치 선택 로직. 엘리트 주기가 찬 틱이면 첫 슬롯은 적격(_IsEliteEligible) 몬스터 중에서만 뽑아 엘리트화.
               bool spawnAsElite = spawnEliteThisTick && spawnIndex == 0;
               StageSpawnData selectedData = SelectMonsterData(spawnAsElite);
               if (spawnAsElite && selectedData == null)
               {
                  // 이 맵에 엘리트 가능 몬스터가 없으면 일반 스폰으로 대체.
                  spawnAsElite = false;
                  selectedData = SelectMonsterData();
               }
               if (selectedData != null)
               {
                  // MonsterData 가져오기 (DatabaseManager를 통해 ID로 조회)
                  MonsterData monsterData = DatabaseManager.Instance.GetMonsterData(selectedData.MonsterId);
                  if (monsterData != null)
                  {
                     Monster spawnMonster = ResourceManager.Instance.Spawn(monsterData.Prefab, spawnPos, parent: transform).GetComponent<Monster>();
                     spawnMonster.Initialize(monsterData);

                     if (spawnAsElite)
                     {
                        EliteVariant eliteVariant = _eliteData.SelectVariant();
                        spawnMonster.ApplyElite(_eliteData, eliteVariant);
                     }

                     _spawnedMonsters.Add(spawnMonster);

                     // 몬스터 스폰 이벤트 발행
                     GameEventSystem.Publish(new MonsterSpawnedEvent(selectedData.MonsterId, spawnAsElite));
                  }
               }
            }

            yield return spawnWait;
         }
      }

      // 엘리트는 검문(역 도착)을 이만큼 지난 뒤부터 등장한다. 검문 카운트는 스테이지당 5회(역 4개 + 맵 변경 1회).
      // 판을 20역으로 늘릴 때도 이 값은 옮기지 않았다 — 플레이어가 강해지는 속도는 흐른 시간이 아니라 상점 방문 수를
      // 따라가므로, 같은 경과 시간으로 미루면 그만큼 쉬워진다.
      private const int ELITE_START_STATION_COUNT = 6;

      // 엘리트 적격 역 도착 수 = SpawnLevel + 유예. 상위 슬롯 몬스터가 등장하자마자
      // 엘리트로 나오면 급격한 벽이 돼서, 등장 후 유예를 둔다.
      // 예전에는 배수(×2.2)였는데, 해금을 맵 등장 구간에 맞추면서 SpawnLevel이 역 번호와 같은
      // 스케일이 됐다. 배수를 그대로 두면 적격 역이 종착역(20)을 넘어 엘리트가 한 번도 안 나온다.
      private const int ELITE_DELAY_STATIONS = 2;

      private bool _IsEliteEligible(StageSpawnData data)
      {
         var stageManager = TrainDefense.Game.Manager.StageManager.Instance;
         int progressCount = stageManager != null ? stageManager.TotalInspectionPassedCount : 0;
         return progressCount >= data.SpawnLevel + ELITE_DELAY_STATIONS;
      }

      // 검문 후 다음 구간의 엘리트를 정산한다. 예산 = 구간시간 ÷ (eliteSpawnCycle × 현재 스폰간격)을 적립해
      // 정수부만큼 구간 (N+1)등분 지점에 배치하고, 소수 잔여분은 다음 구간으로 이월한다.
      // 등장 빈도가 스폰 간격에 비례해 맵별 엘리트 개성(빠른 맵 = 잦은 물몸, 느린 맵 = 드문 탱커)이 유지되고,
      // 배치가 구간 양끝을 피해서 역 도착 직전 스폰 낭비가 없다.
      // ELITE_START_STATION_COUNT 전에는 예산을 적립하지 않는다(데뷔 전 이월 방지).
      private void _ScheduleEliteSpawns()
      {
         if (_eliteData == null || _stationPassedCount < ELITE_START_STATION_COUNT)
            return;

         float segmentDuration = GameEventSystem.Query<GetCurrentInspectionDurationEvent, float>(new GetCurrentInspectionDurationEvent());
         if (segmentDuration <= 0f || spawnInterval <= 0f)
            return;

         _currentSegmentDuration = segmentDuration;
         // 정산은 구간 시작 직후라 경과 0으로 동기화(직전 구간의 낡은 경과값으로 첫 엘리트가 즉시 발동하는 것 방지).
         _currentSegmentElapsed = 0f;
         _eliteBudget += segmentDuration / (Mathf.Max(1, _eliteData.eliteSpawnCycle) * spawnInterval);

         int eliteCountThisSegment = Mathf.FloorToInt(_eliteBudget);
         _eliteBudget -= eliteCountThisSegment;

         _eliteSpawnSchedule.Clear();
         for (int i = 1; i <= eliteCountThisSegment; i++)
         {
            _eliteSpawnSchedule.Add(segmentDuration * i / (eliteCountThisSegment + 1));
         }
      }

      // 선택 가중치. 일반 스폰은 스폰 확률 그대로, 엘리트 후보는 확률의 역수 —
      // 자주 나오는 잡몹(확률 6)일수록 엘리트로는 덜 뽑히고 드문 상위 슬롯(확률 1)일수록 자주 뽑혀
      // 엘리트가 탱커 기반으로 정렬된다. (6/3/1 → 1:2:6)
      private float _GetSelectionWeight(StageSpawnData data, bool eliteEligibleOnly)
      {
         if (data.Probability <= 0)
            return 0f;

         return eliteEligibleOnly ? 1f / data.Probability : data.Probability;
      }

      // eliteEligibleOnly = true 면 엘리트 적격 레벨에 도달한 몬스터 중에서만 뽑는다. 적격 후보가 없으면 null.
      private StageSpawnData SelectMonsterData(bool eliteEligibleOnly = false)
      {
         if (_currentSpawnDatas == null || _currentSpawnDatas.Length == 0)
            return null;

         bool useProbability = false;
         float totalProbability = 0f;
         foreach (var data in _currentSpawnDatas)
         {
            if (eliteEligibleOnly && !_IsEliteEligible(data))
               continue;

            float weight = _GetSelectionWeight(data, eliteEligibleOnly);
            if (weight > 0)
            {
               useProbability = true;
               totalProbability += weight;
            }
         }

         if (!useProbability)
         {
            if (eliteEligibleOnly)
               return null;

            return _currentSpawnDatas[Random.Range(0, _currentSpawnDatas.Length)];
         }

         float randomPoint = Random.value * totalProbability;

         for (int i = 0; i < _currentSpawnDatas.Length; i++)
         {
            if (eliteEligibleOnly && !_IsEliteEligible(_currentSpawnDatas[i]))
               continue;

            float weight = _GetSelectionWeight(_currentSpawnDatas[i], eliteEligibleOnly);
            if (weight > 0)
            {
               if (randomPoint < weight)
               {
                  return _currentSpawnDatas[i];
               }
               randomPoint -= weight;
            }
         }

         return _currentSpawnDatas[^1]; // fallback
      }

      // 맵 전용 영역이 지정돼 있거나 CustomArea 모드면 기차 기준 영역, 아니면 카메라 화면 기준.
      private bool _UseCustomAreaSpawn => _usingMapSpawnAreas || spawnMode == MonsterSpawnType.CustomArea;

      private Vector3 RandomSpawnPos()
      {
         if (!_UseCustomAreaSpawn)
         {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
               return _RandomCameraEdgePos(mainCamera);
            // 메인 카메라가 없으면(비정상) 씬 기본 영역으로 폴백.
         }

         return _RandomCustomAreaPos();
      }

      // 기차(MainTrain) 위치 + 영역 Offset/Size 안의 랜덤 위치.
      private Vector3 _RandomCustomAreaPos()
      {
         Vector3 spawnPos = Vector3.zero;
         if (TrainManager.Instance != null && customSpawnAreas != null && customSpawnAreas.Count > 0)
         {
            Train mainTrain = TrainManager.Instance.MainTrain;
            if (mainTrain != null)
            {
               var area = customSpawnAreas[Random.Range(0, customSpawnAreas.Count)];
               float rx = Random.Range(-area.Size.x / 2f, area.Size.x / 2f);
               float ry = Random.Range(-area.Size.y / 2f, area.Size.y / 2f);

               Vector3 selectedPos = (Vector3)area.Offset + new Vector3(rx, ry, 0);
               spawnPos = mainTrain.transform.position + selectedPos;
            }
         }
         return spawnPos;
      }

      // 카메라가 실제로 보는 화면(뷰포트 0~1) 가장자리 바깥 [spawnRange, spawnRange + spawnDepth] 띠 안의 랜덤 위치.
      // 줌아웃·종횡비 변화(AspectFit)에도 항상 화면 밖에서 스폰된다.
      private Vector3 _RandomCameraEdgePos(Camera mainCamera)
      {
         _GetCameraViewRect(mainCamera, out float minX, out float maxX, out float minY, out float maxY);

         SpawnEdge edge = _PickRandomSpawnEdge();
         float distance = spawnRange + Random.Range(0f, Mathf.Max(0f, spawnDepth));

         switch (edge)
         {
            case SpawnEdge.Bottom:
               return new Vector3(Random.Range(minX, maxX), minY - distance, 0f);
            case SpawnEdge.Left:
               return new Vector3(minX - distance, Random.Range(minY, maxY), 0f);
            case SpawnEdge.Right:
               return new Vector3(maxX + distance, Random.Range(minY, maxY), 0f);
            case SpawnEdge.Top:
            default:
               return new Vector3(Random.Range(minX, maxX), maxY + distance, 0f);
         }
      }

      // 활성화된 가장자리 중 하나를 균등 확률로 고른다. 하나도 없으면(미설정) Top+Bottom으로 동작.
      private SpawnEdge _PickRandomSpawnEdge()
      {
         SpawnEdge enabledEdges = spawnEdges == SpawnEdge.None ? DefaultSpawnEdges : spawnEdges;

         int enabledCount = 0;
         foreach (SpawnEdge edge in AllSpawnEdges)
         {
            if ((enabledEdges & edge) != 0)
               enabledCount++;
         }

         int pickIndex = Random.Range(0, enabledCount);
         foreach (SpawnEdge edge in AllSpawnEdges)
         {
            if ((enabledEdges & edge) == 0)
               continue;
            if (pickIndex == 0)
               return edge;
            pickIndex--;
         }

         return SpawnEdge.Top;
      }

      // 카메라 뷰포트 (0,0)~(1,1)의 월드 XY 범위. z는 카메라→게임 평면(z=0) 거리(직교 카메라면 XY에 영향 없음).
      private static void _GetCameraViewRect(Camera mainCamera, out float minX, out float maxX, out float minY, out float maxY)
      {
         float depth = Mathf.Abs(mainCamera.transform.position.z);
         Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
         Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1f, 1f, depth));

         minX = Mathf.Min(bottomLeft.x, topRight.x);
         maxX = Mathf.Max(bottomLeft.x, topRight.x);
         minY = Mathf.Min(bottomLeft.y, topRight.y);
         maxY = Mathf.Max(bottomLeft.y, topRight.y);
      }

#if UNITY_EDITOR
      private void OnDrawGizmos()
      {
         if (_UseCustomAreaSpawn)
         {
            _DrawCustomAreaGizmos();
         }
         else
         {
            _DrawCameraEdgeGizmos();
         }
      }

      private void _DrawCustomAreaGizmos()
      {
         if (customSpawnAreas == null)
            return;

         Gizmos.color = Color.cyan;

         Vector3 basePos = Vector3.zero;
         if (Application.isPlaying && TrainManager.Instance != null && TrainManager.Instance.MainTrain != null)
         {
            basePos = TrainManager.Instance.MainTrain.transform.position;
         }

         foreach (var area in customSpawnAreas)
         {
            Gizmos.DrawCube(basePos + (Vector3)area.Offset, area.Size);
         }
      }

      // 카메라 화면(흰 선)과 가장자리별 스폰 띠(하늘색)를 그린다. 에디터 모드에서도 Main Camera 기준으로 표시.
      private void _DrawCameraEdgeGizmos()
      {
         Camera mainCamera = Camera.main;
         if (mainCamera == null)
            return;

         _GetCameraViewRect(mainCamera, out float minX, out float maxX, out float minY, out float maxY);
         float width = maxX - minX;
         float height = maxY - minY;
         float centerX = (minX + maxX) * 0.5f;
         float centerY = (minY + maxY) * 0.5f;

         Gizmos.color = Color.white;
         Gizmos.DrawWireCube(new Vector3(centerX, centerY, 0f), new Vector3(width, height, 0f));

         SpawnEdge enabledEdges = spawnEdges == SpawnEdge.None ? DefaultSpawnEdges : spawnEdges;
         float depth = Mathf.Max(0f, spawnDepth);
         float bandCenterOffset = spawnRange + depth * 0.5f;

         Gizmos.color = Color.cyan;
         if ((enabledEdges & SpawnEdge.Top) != 0)
            Gizmos.DrawWireCube(new Vector3(centerX, maxY + bandCenterOffset, 0f), new Vector3(width, depth, 0f));
         if ((enabledEdges & SpawnEdge.Bottom) != 0)
            Gizmos.DrawWireCube(new Vector3(centerX, minY - bandCenterOffset, 0f), new Vector3(width, depth, 0f));
         if ((enabledEdges & SpawnEdge.Left) != 0)
            Gizmos.DrawWireCube(new Vector3(minX - bandCenterOffset, centerY, 0f), new Vector3(depth, height, 0f));
         if ((enabledEdges & SpawnEdge.Right) != 0)
            Gizmos.DrawWireCube(new Vector3(maxX + bandCenterOffset, centerY, 0f), new Vector3(depth, height, 0f));
      }
#endif

      public void RemoveMonster(Monster monster)
      {
         if (monster != null)
         {
            _spawnedMonsters.Remove(monster);
         }
      }

      public void DestroyAllMonsters()
      {
         // 리스트를 복사해서 순회 (제거 중 리스트 변경 방지)
         List<Monster> monstersToDestroy = new(_spawnedMonsters);

         foreach (Monster monster in monstersToDestroy)
         {
            if (monster != null && monster.gameObject != null)
            {
               ResourceManager.Instance.Destroy(monster.gameObject);
            }
         }

         _spawnedMonsters.Clear();
      }
   }
}