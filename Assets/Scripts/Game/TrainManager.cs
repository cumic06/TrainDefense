using Cumic;
using TrainDefense.Game.Datas;
using UnityEngine;

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
    }
}