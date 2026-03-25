using UnityEngine;

namespace TrainDefense
{
   public static class UserOptionDataParser
   {
      private const string HapticEnabledKey = "HapticEnabled";

      public static UserOptionData Load()
      {
         return new UserOptionData
         {
            IsHapticEnabled = PlayerPrefs.GetInt(HapticEnabledKey, 1) == 1,
         };
      }

      public static void Save(UserOptionData userOptionData)
      {
         if (userOptionData == null)
         {
            return;
         }

         PlayerPrefs.SetInt(HapticEnabledKey, userOptionData.IsHapticEnabled ? 1 : 0);
         PlayerPrefs.Save();
      }
   }
}
