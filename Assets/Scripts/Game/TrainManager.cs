using System.Linq;
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

        public Train GetNearTrain(Vector3 position)
        {
            IOrderedEnumerable<Train> nearTrains = mainTrain.CurrentTrains.OrderBy(x => Vector3.Distance(position, x.transform.position));
            if (nearTrains.Count() == 0) return null;
            
            return nearTrains.FirstOrDefault();
        }

        public Train[] GetTrains()
        {
            return mainTrain.CurrentTrains.ToArray();
        }
    }
}