using System.Collections;
using System.Collections.Generic;
using Cumic;
using Cumic.Events;
using Sirenix.OdinInspector;
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
        private SpawnMode spawnMode = SpawnMode.CameraBased;

        [SerializeField]
        [BoxGroup("SpawnSetting")]
        [ShowIf("spawnMode", SpawnMode.CustomArea)]
        private List<SpawnAreaInfo> customSpawnAreas;

        [System.Serializable]
        public struct SpawnAreaInfo
        {
            public Vector2 Offset;
            public Vector2 Size;
        }

        #endregion

        public enum SpawnMode
        {
            CameraBased,
            CustomArea
        }

        private StageSpawnData[] _currentSpawnDatas;
        [ShowInInspector]
        private bool _stopSpawnMonster;
        private readonly List<Monster> _spawnedMonsters = new();

        private float _originalSpawnInterval;

        private void Start()
        {
            _originalSpawnInterval = spawnInterval;
            StopSpawnMonster();
            GameEventSystem.Subscribe<GameEnterEvent>(OnGameEnter);
            GameEventSystem.Subscribe<MonsterRushEvent>(OnMonsterRush);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
            GameEventSystem.Unsubscribe<MonsterRushEvent>(OnMonsterRush);
        }

        private void OnGameEnter(GameEnterEvent gameEnterEvent)
        {
            StartCoroutine(SpawnMonster());
        }

        public void SetSpawnRule(StageSpawnData[] spawnDatas)
        {
            _currentSpawnDatas = spawnDatas;
            spawnInterval = _originalSpawnInterval;
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
            spawnInterval = _originalSpawnInterval * monsterRushEvent.SpawnTimeMultiplier;
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
            WaitForSeconds spawnWait = new(spawnInterval);

            while (true)
            {
                if (_stopSpawnMonster)
                {
                    yield return new WaitUntil(() => !_stopSpawnMonster);
                }

                if (_currentSpawnDatas == null || _currentSpawnDatas.Length == 0)
                {
                    yield return spawnWait;
                    continue;
                }

                Vector3 spawnPos = RandomSpawnPos();

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
                        _spawnedMonsters.Add(spawnMonster);

                        // 몬스터 스폰 이벤트 발행
                        GameEventSystem.Publish(new MonsterSpawnedEvent(selectedData.MonsterId));
                    }
                }

                yield return spawnWait;
            }
        }

        private StageSpawnData SelectMonsterData()
        {
            if (_currentSpawnDatas == null || _currentSpawnDatas.Length == 0) return null;

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
            if (spawnMode == SpawnMode.CustomArea)
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
                        spawnPos = new(maxY + spawnRange, Random.Range(minX, maxX), 0);
                        break;
                }
                return spawnPos;
            }

            return spawnPos;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (spawnMode == SpawnMode.CustomArea && customSpawnAreas != null)
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