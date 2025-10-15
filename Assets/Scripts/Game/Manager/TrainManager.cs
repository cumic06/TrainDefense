using UnityEngine;
using Cumic;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game
{
    public class TrainManager : Singleton<TrainManager>
    {
        #region Field
        [SerializeField]
        private MainTrain mainTrain;
        #endregion

        public MainTrain MainTrain => mainTrain;

        public bool CheckHasTrain(TrainData trainData)
        {
            return mainTrain.CheckHasTrain(trainData);
        }

        public bool CheckHasTrainById(string trainId)
        {
            return mainTrain.CheckHasTrainById(trainId);
        }

        public Train GetNearTrain(Vector3 position)
        {
            var trains = mainTrain.CurrentTrains;
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
            return mainTrain.CurrentTrains.ToArray();
        }
    }
}