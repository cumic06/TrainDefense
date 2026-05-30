using UnityEngine;

namespace TrainDefense
{
   public static class UserOptionDataParser
   {
      private const string HapticEnabledKey = "HapticEnabled";
      private const string BgmVolumeKey = "BgmVolume";
      private const string SfxVolumeKey = "SfxVolume";
      private const string BgmMutedKey = "BgmMuted";
      private const string SfxMutedKey = "SfxMuted";

      public static UserOptionData Load()
      {
         return new UserOptionData
         {
            IsHapticEnabled = PlayerPrefs.GetInt(HapticEnabledKey, 1) == 1,
            BgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, 0.5f),
            SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 0.5f),
            IsBgmMuted = PlayerPrefs.GetInt(BgmMutedKey, 0) == 1,
            IsSfxMuted = PlayerPrefs.GetInt(SfxMutedKey, 0) == 1,
         };
      }

      public static void Save(UserOptionData userOptionData)
      {
         if (userOptionData == null)
         {
            return;
         }

         PlayerPrefs.SetInt(HapticEnabledKey, userOptionData.IsHapticEnabled ? 1 : 0);
         PlayerPrefs.SetFloat(BgmVolumeKey, userOptionData.BgmVolume);
         PlayerPrefs.SetFloat(SfxVolumeKey, userOptionData.SfxVolume);
         PlayerPrefs.SetInt(BgmMutedKey, userOptionData.IsBgmMuted ? 1 : 0);
         PlayerPrefs.SetInt(SfxMutedKey, userOptionData.IsSfxMuted ? 1 : 0);
         PlayerPrefs.Save();
      }
   }
}
