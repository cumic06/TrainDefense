using Cumic;
using UnityEngine;

namespace TrainDefense.Game
{
   public class HapticManager : Singleton<HapticManager>
   {
      private IHapticService _service;
      private bool _cachedEnabled = true;

      public bool IsEnabled
      {
         get
         {
            if (UserDataManager.Instance != null)
            {
               return UserDataManager.Instance.IsHapticEnabled;
            }

            return _cachedEnabled;
         }
      }

      public bool IsSupported => _service != null && _service.IsSupported;

      protected override void Awake()
      {
         base.Awake();

         if (Instance != this)
         {
            return;
         }

         _service = CreateService();
         _cachedEnabled = UserDataManager.Instance == null || UserDataManager.Instance.IsHapticEnabled;
      }

      private void Start()
      {
         if (UserDataManager.Instance != null)
         {
            _cachedEnabled = UserDataManager.Instance.IsHapticEnabled;
         }
      }

      public void SetEnabled(bool enabled)
      {
         _cachedEnabled = enabled;

         if (UserDataManager.Instance != null)
         {
            UserDataManager.Instance.SetHapticEnabled(enabled);
         }
      }

      public void Play(HapticFeedbackType type)
      {
         if (!IsEnabled || _service == null || !_service.IsSupported)
         {
            return;
         }

         _service.Play(type);
      }

      private static IHapticService CreateService()
      {
#if UNITY_ANDROID && !UNITY_EDITOR
         return new AndroidHapticService();
#else
         return new NullHapticService();
#endif
      }
   }
}
