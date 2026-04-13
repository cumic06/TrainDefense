using Cumic.Events;
using TrainDefense.Game;
using TrainDefense.Game.Datas;
using UnityEngine;

namespace TrainDefense
{
    public class LobbyGameSimulation : MonoBehaviour
    {
        [SerializeField]
        private Train[] trains;

        private StageData _currentStageData;

        private void Start()
        {
            GameEventSystem.Publish(new GameEnterEvent());
            SetStageData();
            SpawnTrain();
            MapSpawn();
            SetSpawnRule();
        }

        private void SetStageData()
        {
            var _stageDatas = DatabaseManager.Instance.GetStageDatas();
            _currentStageData = _stageDatas[Random.Range(0, _stageDatas.Length)];
        }

        private void SpawnTrain()
        {
            var main = TrainManager.Instance.MainTrain;
            main.UndeadTrain();
            for (int i = 0; i < trains.Length; i++)
            {
                main.SpawnTrain(trains[i]);
            }
        }

        private void SetSpawnRule()
        {
            StageSpawnData[] spawnDatas = { _currentStageData.SpawnDatas[0] };
            MonsterSpawner.Instance.SetSpawnRule(spawnDatas, _currentStageData.SpawnInterval);
            MonsterSpawner.Instance.StartSpawnMonster();
        }

        private void MapSpawn()
        {
            var mapData = DatabaseManager.Instance.GetMapData(_currentStageData);
            Instantiate(mapData.Prefab, Vector3.zero, Quaternion.identity);
        }
    }
}
