using Cumic;
using UnityEngine;

namespace TrainDefense.Game
{
   public class NullHapticService : IHapticService
   {
      public bool IsSupported => false;

      public void Play(HapticFeedbackType type)
      {
      }
   }
}
