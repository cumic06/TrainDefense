using UnityEngine;

namespace TrainDefense.Game.Events
{
   /// <summary>
   /// Train이 다른 Train으로 대체될 때 발행되는 이벤트 (Elite Train 전환용)
   /// </summary>
   public class ReplaceTrainEvent
   {
      private Train _oldTrain;
      private Train _newTrain;
      private Sprite _newIcon;

      public Train OldTrain => _oldTrain;
      public Train NewTrain => _newTrain;
      public Sprite NewIcon => _newIcon;

      public ReplaceTrainEvent(Train oldTrain, Train newTrain, Sprite newIcon)
      {
         _oldTrain = oldTrain;
         _newTrain = newTrain;
         _newIcon = newIcon;
      }
   }
}