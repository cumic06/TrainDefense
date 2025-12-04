using System.Collections;
using Sirenix.OdinInspector;
using TrainDefense.Game.Datas;
using UnityEngine;

namespace TrainDefense.Game
{
    public class MonsterSpawner : MonoBehaviour
    {
        #region Field
        [SerializeField]
        [BoxGroup("SpawnSetting")]
        private float spawnInterval;
        [SerializeField]
        [BoxGroup("SpawnSetting")]
        private float spawnRange;
        #endregion

        private MonsterData[] _monsterDatas;
        private bool _stopSpawnMonster;

        private void Start()
        {
            LoadMonsterDatas();
            _stopSpawnMonster = false;
            StartCoroutine(SpawnMonster());
        }

        private void LoadMonsterDatas()
        {
            _monsterDatas = DatabaseManager.Instance.GetMonsterDatas();
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

                Vector3 spawnPos = RandomSpawnPos();

                MonsterData randomMonsterData = _monsterDatas[Random.Range(0, _monsterDatas.Length)];
                Monster spawnMonster = ResourceManager.Instance.Spawn(randomMonsterData.Prefab, spawnPos, parent: transform).GetComponent<Monster>();
                spawnMonster.Initialize(randomMonsterData);
                yield return spawnWait;
            }
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
    }
}