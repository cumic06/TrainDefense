using Cumic;
using TrainDefense.Game.Data;
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
    }
}