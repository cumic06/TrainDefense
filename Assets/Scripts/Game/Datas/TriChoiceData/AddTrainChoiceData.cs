using TrainDefense.Game.Data;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [CreateAssetMenu(fileName = "AddTrainChoiceData", menuName = "Data/TriChoiceData/AddTrainChoiceData")]
    public class AddTrainChoiceData : TriChoiceData
    {
        #region Fields
        [SerializeField]
        private TrainData trainData;
        #endregion

        public TrainData TrainData => trainData;
    }
}