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
      private float spawnRange;
      [SerializeField]
      [BoxGroup("SpawnSetting")]
      [Tooltip("스테이지(맵) 변경 1회당 스폰 간격 단축 %")]
      private float stageSpawnAccelPercent = 10f;
      [SerializeField]
      [BoxGroup("SpawnSetting")]
      private float maxSpawnAccelPercent = 50f;
      [SerializeField]
      [BoxGroup("SpawnSetting")]
      private MonsterSpawnType spawnMode = MonsterSpawnType.CameraBased;

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

      #endregion

      [ShowInInspector]
      private bool _stopSpawnMonster;

      private StageSpawnData[] _currentSpawnDatas;
      // 씬에 설정된 공통 스폰 영역. 첫 SetSpawnRule 때 보존하고, 맵별 영역이 없을 때 이 값으로 복귀.
      private List<SpawnAreaInfo> _defaultSpawnAreas;
      // 현재 맵의 스폰 이펙트(없으면 null). 몬스터 스폰 시 spawnPos에 생성.
      private GameObject _currentSpawnEffect;

      [ShowInInspector]
      private readonly List<Monster> _spawnedMonsters = new();

      private float _originalSpawnInterval;
      private int _originalSpawnCount;
      private int _stationPassedCount = 0;
      // 스테이지(맵) 변경 누적 횟수. 스폰 가속의 기준(검문 기반에서 변경, 2026-07-04).
      private int _stageChangeCount = 0;

      private EliteData _eliteData;
      // 스폰 루프(틱) 카운터. eliteSpawnCycle틱마다 1마리를 엘리트로. 스테이지가 바뀌어도 이월.
      [ShowInInspector]
      private int _eliteSpawnCounter;

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
         GameEventSystem.Subscribe<StageSelectEvent>(OnStageSelect);
      }

      private void OnDestroy()
      {
         GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
         GameEventSystem.Unsubscribe<MonsterRushEvent>(OnMonsterRush);
         GameEventSystem.Unsubscribe<InspectionStartEvent>(OnInspectionStart);
         GameEventSystem.Unsubscribe<TriChoiceSelectEvent>(OnTriChoiceSelect);
         GameEventSystem.Unsubscribe<StageSelectEvent>(OnStageSelect);
      }
      #endregion

      private void OnGameEnter(GameEnterEvent gameEnterEvent)
      {
         if (DatabaseManager.Instance == null) return;

         _eliteData = DatabaseManager.Instance.GetEliteData();
         _eliteSpawnCounter = 0;
         _stationPassedCount = 0;
         _stageChangeCount = 0;
         spawnCount = _originalSpawnCount;
         StartCoroutine(SpawnMonster());
      }

      private void OnInspectionStart(InspectionStartEvent inspectionStartEvent)
      {
         DestroyAllMonsters();
         _stationPassedCount++;
         spawnInterval = _GetAcceleratedInterval();
      }

      private void OnTriChoiceSelect(TriChoiceSelectEvent triChoiceSelectEvent)
      {
         if (_stopSpawnMonster)
         {
            StartSpawnMonster();
         }
      }

      // 스테이지(맵) 변경마다 한 번에 소환하는 몬스터 수를 1 늘린다.
      private void OnStageSelect(StageSelectEvent stageSelectEvent)
      {
         spawnCount++;
      }

      public void SetSpawnRule(StageSpawnData[] spawnDatas, float monsterSpawnInterval, List<SpawnAreaInfo> mapSpawnAreas = null, GameObject mapSpawnEffect = null)
      {
         _currentSpawnDatas = spawnDatas;
         _originalSpawnInterval = monsterSpawnInterval;
         // 스폰풀/인터벌만 갱신(레벨업·스테이지 변경 시 호출). 역 누적 가속은 유지하고 현재 가속을 반영.
         spawnInterval = _GetAcceleratedInterval();

         // 맵별 영역(MapData.CustomSpawnAreas)이 있으면 그걸 쓰고, 없으면 씬 기본(공통)으로 복귀.
         _defaultSpawnAreas ??= customSpawnAreas != null ? new List<SpawnAreaInfo>(customSpawnAreas) : new List<SpawnAreaInfo>();
         customSpawnAreas = (mapSpawnAreas != null && mapSpawnAreas.Count > 0) ? mapSpawnAreas : _defaultSpawnAreas;

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

            // 엘리트 스폰: 확률이 아니라 "스폰 루프(틱) eliteSpawnCycle회마다 1마리"를 고정 주기로 스폰.
            // 틱 기준이라 엘리트 시간 간격 = eliteSpawnCycle × 스폰 간격(spawnCount와 무관).
            // 스테이지1 통과(EliteStartStationCount) 전에는 등장하지 않는다.
            bool spawnEliteThisTick = false;
            if (_eliteData != null && _stationPassedCount >= EliteStartStationCount)
            {
               _eliteSpawnCounter++;
               if (_eliteSpawnCounter >= Mathf.Max(1, _eliteData.eliteSpawnCycle))
               {
                  _eliteSpawnCounter = 0;
                  spawnEliteThisTick = true;
               }
            }

            // 한 spawnInterval마다 spawnCount 마리를 각자 다른 위치·몬스터로 소환
            for (int spawnIndex = 0; spawnIndex < Mathf.Max(1, spawnCount); spawnIndex++)
            {
               Vector3 spawnPos = RandomSpawnPos();

               if (_currentSpawnEffect != null)
               {
                  ResourceManager.Instance.Spawn(_currentSpawnEffect, spawnPos, parent: transform);
               }

               // 가중치 선택 로직. 엘리트 주기가 찬 틱이면 첫 슬롯은 적격(EliteChanceMultiplier>0) 몬스터 중에서만 뽑아 엘리트화.
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
                     GameEventSystem.Publish(new MonsterSpawnedEvent(selectedData.MonsterId));
                  }
               }
            }

            yield return spawnWait;
         }
      }

      // 엘리트는 스테이지1 통과 후부터 등장(역 3개 후 4번째 검문에 맵 변경).
      private const int EliteStartStationCount = 4;

      // eliteEligibleOnly = true 면 엘리트 가능(EliteChanceMultiplier>0) 몬스터 중에서만 뽑는다. 적격 후보가 없으면 null.
      private StageSpawnData SelectMonsterData(bool eliteEligibleOnly = false)
      {
         if (_currentSpawnDatas == null || _currentSpawnDatas.Length == 0)
            return null;

         bool useProbability = false;
         float totalProbability = 0f;
         foreach (var data in _currentSpawnDatas)
         {
            if (eliteEligibleOnly && data.EliteChanceMultiplier <= 0f)
               continue;

            if (data.Probability > 0)
            {
               useProbability = true;
               totalProbability += data.Probability;
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
            if (eliteEligibleOnly && _currentSpawnDatas[i].EliteChanceMultiplier <= 0f)
               continue;

            if (_currentSpawnDatas[i].Probability > 0)
            {
               if (randomPoint < _currentSpawnDatas[i].Probability)
               {
                  return _currentSpawnDatas[i];
               }
               randomPoint -= _currentSpawnDatas[i].Probability;
            }
         }

         return _currentSpawnDatas[^1]; // fallback
      }

      private Vector3 RandomSpawnPos()
      {
         Vector3 spawnPos = Vector3.zero;
         if (spawnMode == MonsterSpawnType.CustomArea)
         {
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
         }
         else
         {
            Camera camera = Camera.main;

            Vector3 bottomLeft = camera.ViewportToWorldPoint(new Vector3(0, 0, camera.transform.position.z));
            Vector3 topRight = camera.ViewportToWorldPoint(new Vector3(1, 1, camera.transform.position.z));

            float minX = bottomLeft.x;
            float maxX = topRight.x;
            float minY = bottomLeft.y;
            float maxY = topRight.y;

            int randomDirection = Random.Range(0, 4);

            switch (randomDirection)
            {
               case 0:
                  spawnPos = new(Random.Range(minX, maxX), maxY + spawnRange, 0);
                  break;
               case 1:
                  spawnPos = new(Random.Range(minX, maxX), minY - spawnRange, 0);
                  break;
               case 2:
                  spawnPos = new(minX - spawnRange, Random.Range(minY, maxY), 0);
                  break;
               case 3:
                  spawnPos = new(maxX + spawnRange, Random.Range(minY, maxY), 0);
                  break;
            }
            return spawnPos;
         }

         return spawnPos;
      }

#if UNITY_EDITOR
      private void OnDrawGizmos()
      {
         if (spawnMode == MonsterSpawnType.CustomArea && customSpawnAreas != null)
         {
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