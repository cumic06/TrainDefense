using System.Collections;
using System.Collections.Generic;
using Cumic;
using Cumic.Events;
using Sirenix.OdinInspector;
using TrainDefense.Game.Datas;
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
        #endregion

        private StageSpawnData[] _currentSpawnDatas;
        [ShowInInspector]
        private bool _stopSpawnMonster;
        private readonly List<Monster> _spawnedMonsters = new();

        private void Start()
        {
            StopSpawnMonster();
            GameEventSystem.Subscribe<GameEnterEvent>(OnGameEnter);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
        }

        private void OnGameEnter(GameEnterEvent gameEnterEvent)
        {
            StartSpawnMonster();
            StartCoroutine(SpawnMonster());
        }

        public void SetSpawnRule(StageSpawnData[] spawnDatas)
        {
            _currentSpawnDatas = spawnDatas;
        }

        public void StartSpawnMonster()
        {
            _stopSpawnMonster = false;
        }

        public void StopSpawnMonster()
        {
            _stopSpawnMonster = true;
        }

        private IEnumerator SpawnMonster()
        {
            WaitForSeconds spawnWait = new(spawnInterval);

            while (true)
            {
                if (_stopSpawnMonster)
                {
                    yield break;
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
            Camera camera = Camera.main;

            Vector3 bottomLeft = camera.ViewportToWorldPoint(new Vector3(0, 0, camera.transform.position.z));
            Vector3 topRight = camera.ViewportToWorldPoint(new Vector3(1, 1, camera.transform.position.z));

            float minX = bottomLeft.x;
            float maxX = topRight.x;
            float minY = bottomLeft.y;
            float maxY = topRight.y;

            int randomDirection = Random.Range(0, 4);
            Vector3 spawnPos = Vector3.zero;

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