using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TrainDefense.Game
{
    public class MonsterSpawner : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private Monster[] monsterPrefabs;
        [SerializeField]
        [BoxGroup("SpawnSetting")]
        private float spawnInterval;
        [SerializeField]
        [BoxGroup("SpawnSetting")]
        private float spawnRange;
        #endregion

        private void Start()
        {
            StartCoroutine(SpawnMonster());
        }

        private IEnumerator SpawnMonster()
        {
            WaitForSeconds spawnWait = new(spawnInterval);

            while (true)
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

                Monster randomMonster = monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];
                Monster spawnMonster = ResourceManager.Instance.Spawn(randomMonster, spawnPos, parent: transform);

                yield return spawnWait;
            }
        }
    }
}