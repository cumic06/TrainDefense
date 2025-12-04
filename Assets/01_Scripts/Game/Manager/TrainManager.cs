using UnityEngine;
using Cumic;
using TrainDefense.Game.Datas;
using Cumic.Events;
using Unity.Cinemachine;

namespace TrainDefense.Game
{
    public class TrainManager : Singleton<TrainManager>
    {
        #region Field
        [SerializeField]
        private MainTrain mainTrain;
        [SerializeField]
        private CinemachineCamera cinemachineCamera;
        #endregion

        public MainTrain MainTrain => mainTrain;

        private void Start()
        {
            GameEventSystem.Subscribe<GameEnterEvent>(OnGameEnter);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
        }

        private void OnGameEnter(GameEnterEvent gameEnterEvent)
        {
            if (mainTrain != null)
            {
                var trainData = DatabaseManager.Instance.GetTrainData(mainTrain.Id);
                var trainObject = ResourceManager.Instance.Spawn(trainData.Prefab).GetComponent<MainTrain>();
                mainTrain = trainObject;
                mainTrain.Initialize(trainData);
                cinemachineCamera.Target.TrackingTarget = mainTrain.transform;
            }
        }

        public bool CheckHasTrain(TrainData trainData)
        {
            return mainTrain.CheckHasTrain(trainData);
        }

        public bool CheckHasTrainById(string trainId)
        {
            return mainTrain.CheckHasTrainById(trainId);
        }

        public bool IsMaxTrainCountReached()
        {
            return mainTrain.CurrentTrainCount >= mainTrain.MaxTrainCount;
        }

        public Train GetNearTrain(Vector3 position)
        {
            var trains = mainTrain.CurrentAliveTrains;
            if (trains.Count == 0) return null;

            Train closest = null;
            float minSqrDistance = float.MaxValue;

            foreach (var train in trains)
            {
                if (train.IsDead || train.IsMainTrain || train == null || !train.gameObject.activeInHierarchy) continue;

                float sqrDistance = position.SqrDistance(train.transform.position);

                if (sqrDistance < minSqrDistance)
                {
                    minSqrDistance = sqrDistance;
                    closest = train;
                }
            }

            return closest;
        }

        public Train[] GetTrains()
        {
            return mainTrain.CurrentAliveTrains.ToArray();
        }

        public void ApplyUpgrade(UpgradeData upgradeData)
        {
            if (mainTrain == null)
            {
                Debug.LogWarning("TrainManager: MainTrain is null");
                return;
            }

            mainTrain.ApplyUpgrade(upgradeData);
        }
    }
}