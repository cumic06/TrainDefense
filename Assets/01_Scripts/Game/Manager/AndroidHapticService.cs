using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game
{
#if UNITY_ANDROID && !UNITY_EDITOR
   internal sealed class AndroidHapticService : IHapticService
   {
      private static readonly Dictionary<HapticFeedbackType, HapticPattern> Patterns = new()
      {
         { HapticFeedbackType.Selection, new HapticPattern(16L, 40) },
         { HapticFeedbackType.LightImpact, new HapticPattern(24L, 70) },
         { HapticFeedbackType.MediumImpact, new HapticPattern(32L, 120) },
         { HapticFeedbackType.HeavyImpact, new HapticPattern(48L, 180) },
         { HapticFeedbackType.Success, new HapticPattern(28L, 100) },
         { HapticFeedbackType.Warning, new HapticPattern(40L, 140) },
         { HapticFeedbackType.Error, new HapticPattern(60L, 220) },
      };

      private readonly AndroidJavaObject _vibrator;
      private readonly int _sdkInt;

      public bool IsSupported { get; }

      public AndroidHapticService()
      {
         try
         {
            using AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            using AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (currentActivity == null)
            {
               return;
            }

            using AndroidJavaClass versionClass = new("android.os.Build$VERSION");
            _sdkInt = versionClass.GetStatic<int>("SDK_INT");

            // API 31+ prefers VibrationManager.
            if (_sdkInt >= 31)
            {
               using AndroidJavaObject vibrationManager = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibration_manager");
               if (vibrationManager != null)
               {
                  _vibrator = vibrationManager.Call<AndroidJavaObject>("getDefaultVibrator");
               }
            }

            // Fallback for older Android (and if VibrationManager unavailable).
            _vibrator ??= currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");

            if (_vibrator == null)
            {
               return;
            }

            IsSupported = _vibrator.Call<bool>("hasVibrator");
         }
         catch (Exception exception)
         {
            Debug.LogWarning($"[HapticManager] Android vibrator initialization failed: {exception.Message}");
         }
      }

      public void Play(HapticFeedbackType type)
      {
         if (!IsSupported || _vibrator == null)
         {
            return;
         }

         if (!Patterns.TryGetValue(type, out HapticPattern pattern))
         {
            pattern = Patterns[HapticFeedbackType.Selection];
         }

         try
         {
            if (_sdkInt >= 26)
            {
               using AndroidJavaClass vibrationEffectClass = new("android.os.VibrationEffect");
               using AndroidJavaObject vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                  "createOneShot",
                  pattern.DurationMs,
                  pattern.Amplitude);

               _vibrator.Call("vibrate", vibrationEffect);
               return;
            }

            _vibrator.Call("vibrate", pattern.DurationMs);
         }
         catch (Exception exception)
         {
            Debug.LogWarning($"[HapticManager] Android vibration failed: {exception.Message}");
         }
      }
   }
#else
   internal sealed class AndroidHapticService : IHapticService
   {
      public bool IsSupported => false;

      public void Play(HapticFeedbackType type)
      {
      }
   }
#endif
}
