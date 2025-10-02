using Cumic;
using TrainDefense.Game.Datas;
using UnityEngine;

namespace TrainDefense.Game
{
    public class TrainManager : Singleton<TrainManager>
    {
        [SerializeField]
        private MainTrain mainTrain;

        public bool CheckHasTrain(TrainData trainData)
        {
            return mainTrain.CheckHasTrain(trainData);
        }

        public bool CheckHasTrainById(string trainId)
        {
            return mainTrain.CheckHasTrainById(trainId);
        }
    }
}