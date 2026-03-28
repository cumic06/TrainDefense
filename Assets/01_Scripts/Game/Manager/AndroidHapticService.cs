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
            // NOTE: Do NOT use 'using' on currentActivity — it is a global object
            // managed by Unity. Disposing it destroys the Java-side reference and
            // causes JNI crashes for all subsequent Android API calls.
            AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (currentActivity == null)
            {
               return;
            }

            AndroidJavaClass versionClass = new("android.os.Build$VERSION");
            AndroidJavaClass contextClass = new("android.content.Context");
            _sdkInt = versionClass.GetStatic<int>("SDK_INT");

            // API 31+ prefers VibrationManager.
            if (_sdkInt >= 31)
            {
               try
               {
                  string vibratorManagerService = contextClass.GetStatic<string>("VIBRATOR_MANAGER_SERVICE");
                  AndroidJavaObject vibrationManager = currentActivity.Call<AndroidJavaObject>("getSystemService", vibratorManagerService);
                  if (vibrationManager != null)
                  {
                     _vibrator = vibrationManager.Call<AndroidJavaObject>("getDefaultVibrator");
                  }
               }
               catch (Exception exception)
               {
                  Debug.LogWarning($"[HapticManager] VibrationManager unavailable, falling back to Vibrator: {exception.Message}");
               }
            }

            // Fallback for older Android (and if VibrationManager unavailable).
            if (_vibrator == null)
            {
               string vibratorService = contextClass.GetStatic<string>("VIBRATOR_SERVICE");
               _vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", vibratorService);
            }

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
         try
         {
            if (IsSupported && _vibrator != null)
            {
               if (!Patterns.TryGetValue(type, out HapticPattern pattern))
               {
                  pattern = Patterns[HapticFeedbackType.Selection];
               }

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
               return;
            }
         }
         catch (Exception exception)
         {
            Debug.LogWarning($"[HapticManager] Android vibration failed: {exception.Message}. Fallback to Handheld.Vibrate.");
         }

         Handheld.Vibrate();
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
