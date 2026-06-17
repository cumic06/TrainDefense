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
      private float spawnRange;
      [SerializeField]
      [BoxGroup("SpawnSetting")]
      private float stationSpawnAccelPercent = 8f;
      [SerializeField]
      [BoxGroup("SpawnSetting")]
      private float maxSpawnAccelPercent = 80f;
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
      private int _stationPassedCount = 0;

      private EliteData _eliteData;
      [ShowInInspector]
      private float _currentEliteSpawnChance;
      private float _eliteRampElapsed;
      // 엘리트 배율 진행도(0~1). 한 판 동안 누적 증가하며 각 몬스터의 EliteChanceMultiplier에 곱해진다.
      [ShowInInspector]
      private float _currentEliteMultiplierProgress;
      private float _eliteMultiplierRampElapsed;

      #region UnityLifeCycle
      private void Start()
      {
         _originalSpawnInterval = spawnInterval;
         StopSpawnMonster();
         GameEventSystem.Subscribe<GameEnterEvent>(OnGameEnter);
         GameEventSystem.Subscribe<MonsterRushEvent>(OnMonsterRush);
         GameEventSystem.Subscribe<InspectionStartEvent>(OnInspectionStart);
         GameEventSystem.Subscribe<TriChoiceSelectEvent>(OnTriChoiceSelect);
      }

      private void OnDestroy()
      {
         GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
         GameEventSystem.Unsubscribe<MonsterRushEvent>(OnMonsterRush);
         GameEventSystem.Unsubscribe<InspectionStartEvent>(OnInspectionStart);
         GameEventSystem.Unsubscribe<TriChoiceSelectEvent>(OnTriChoiceSelect);
      }
      #endregion

      private void OnGameEnter(GameEnterEvent gameEnterEvent)
      {
         if (DatabaseManager.Instance == null) return;

         _eliteData = DatabaseManager.Instance.GetEliteData();
         _currentEliteSpawnChance = 0f;
         _eliteRampElapsed = 0f;
         _currentEliteMultiplierProgress = _eliteData != null ? Mathf.Clamp01(_eliteData.multiplierStartProgress) : 1f;
         _eliteMultiplierRampElapsed = 0f;
         _stationPassedCount = 0;
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

            UpdateEliteChance(spawnInterval);
            UpdateEliteMultiplierProgress(spawnInterval);

            if (_currentSpawnDatas == null || _currentSpawnDatas.Length == 0)
            {
               yield return spawnWait;
               continue;
            }

            Vector3 spawnPos = RandomSpawnPos();

            if (_currentSpawnEffect != null)
            {
               ResourceManager.Instance.Spawn(_currentSpawnEffect, spawnPos, parent: transform);
            }

            // 가중치 선택 로직
            StageSpawnData selectedData = SelectMonsterData();
            if (selectedData != null)
            {
               // MonsterData 가져오기 (DatabaseManager를 통해 ID로 조회)
               MonsterData monsterData = DatabaseManager.Instance.GetMonsterData(selectedData.MonsterId);
               if (monsterData != null)
               {
                  Monster spawnMonster = ResourceManager.Instance.Spawn(monsterData.Prefab, spawnPos, parent: transform).GetComponent<Monster>();
                  spawnMonster.Initialize(monsterData);

                  // 램프 ON(강한 몬스터): 한 판 누적 진행도를 곱해 초반엔 낮고 후반으로 갈수록 설정 배율까지 증가.
                  // 램프 OFF(약한 몬스터): 진행도 무관하게 설정 배율 그대로 적용.
                  float eliteProgress = selectedData.EliteChanceRamp ? _currentEliteMultiplierProgress : 1f;
                  float eliteMultiplier = selectedData.EliteChanceMultiplier * eliteProgress;
                  float eliteChance = _currentEliteSpawnChance * eliteMultiplier;
                  bool isElite = _eliteData != null && eliteChance > 0f && Random.value * 100f < eliteChance;
                  if (isElite)
                  {
                     spawnMonster.ApplyElite(_eliteData);
                  }

                  _spawnedMonsters.Add(spawnMonster);

                  // 몬스터 스폰 이벤트 발행
                  GameEventSystem.Publish(new MonsterSpawnedEvent(selectedData.MonsterId));
               }
            }

            yield return spawnWait;
         }
      }

      // 엘리트는 스테이지1 통과 후부터 등장(역 3개 후 4번째 검문에 맵 변경). 통과 직후 5%로 시작해 spawnMaxChance(15%)까지 ramp.
      private const int EliteStartStationCount = 4;
      private const float EliteStartChance = 5f;

      private void UpdateEliteChance(float elapsed)
      {
         if (_eliteData == null || _eliteData.spawnInterval <= 0f) return;

         // 스테이지1 통과 전엔 엘리트 없음
         if (_stationPassedCount < EliteStartStationCount) return;

         // 스테이지1 통과 직후 5%로 시작
         if (_currentEliteSpawnChance < EliteStartChance)
            _currentEliteSpawnChance = EliteStartChance;

         _eliteRampElapsed += elapsed;
         while (_eliteRampElapsed >= _eliteData.spawnInterval)
         {
            _currentEliteSpawnChance = Mathf.Min(
               _currentEliteSpawnChance + _eliteData.chanceGrowthPerInterval,
               _eliteData.spawnMaxChance);
            _eliteRampElapsed -= _eliteData.spawnInterval;
         }
      }

      private void UpdateEliteMultiplierProgress(float elapsed)
      {
         if (_eliteData == null || _eliteData.multiplierRampInterval <= 0f) return;
         if (_currentEliteMultiplierProgress >= 1f) return;

         _eliteMultiplierRampElapsed += elapsed;
         while (_eliteMultiplierRampElapsed >= _eliteData.multiplierRampInterval)
         {
            _currentEliteMultiplierProgress = Mathf.Min(
               1f,
               _currentEliteMultiplierProgress + _eliteData.multiplierGrowthPerInterval);
            _eliteMultiplierRampElapsed -= _eliteData.multiplierRampInterval;
         }
      }

      private StageSpawnData SelectMonsterData()
      {
         if (_currentSpawnDatas == null || _currentSpawnDatas.Length == 0)
            return null;

         bool useProbability = false;
         float totalProbability = 0f;
         foreach (var data in _currentSpawnDatas)
         {
            if (data.Probability > 0)
            {
               useProbability = true;
               totalProbability += data.Probability;
            }
         }

         if (!useProbability)
         {
            return _currentSpawnDatas[Random.Range(0, _currentSpawnDatas.Length)];
         }

         float randomPoint = Random.value * totalProbability;

         for (int i = 0; i < _currentSpawnDatas.Length; i++)
         {
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