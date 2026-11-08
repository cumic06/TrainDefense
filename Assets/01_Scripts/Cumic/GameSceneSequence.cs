using Cysharp.Threading.Tasks;
using UnityEngine;
using Cumic.Checker;
using Cumic.Events;
using Cumic.UI;
using TrainDefense;
using TrainDefense.Game;

namespace Cumic.Sequence
{
   public class GameSceneSequence : MonoBehaviour
   {
      private ISceneSequencer _currentSceneSequencer;
      private UserDataManager _userDataManager;

      private void Start()
      {
         DontDestroyOnLoad(gameObject);

#if !UNITY_EDITOR
         Application.targetFrameRate = 60;
#endif

         OAuthSceneSequence();
      }

      #region OAuthSceneSequence
      private void OAuthSceneSequence()
      {
         _currentSceneSequencer = new OAuthSceneSequencer(this, new VersionChecker(), new PrivacyAgreementChecker(), new AuthChecker());
         _currentSceneSequencer.OnCompleted += OnOAuthSceneCompleted;
         _currentSceneSequencer.Run().Forget();
      }

      private void OnOAuthSceneCompleted(bool isCompleted)
      {
         _currentSceneSequencer.OnCompleted -= OnOAuthSceneCompleted;
         _currentSceneSequencer.Dispose();

         if (isCompleted)
         {
            Debug.Log("OAuthSceneCompleted");

            SceneController.NextScene(false);

            LobbySceneSequence();

            if (_userDataManager == null)
            {
               UserDataManager userDataManager = new GameObject("UserDataManager").AddComponent<UserDataManager>();
               _userDataManager = userDataManager;
            }

            if (DatabaseManager.Instance == null)
            {
               Instantiate(Resources.Load<DatabaseManager>("Prefabs/DatabaseManager"));
            }

            if (SoundManager.Instance == null)
            {
               SoundManager soundManager = Instantiate(Resources.Load<SoundManager>("Prefabs/SoundManager"));
               soundManager.PlayBGMOnInit();
            }

            GameEventSystem.Publish(new LobbyEnterEvent());
         }
      }
      #endregion

      #region LobbySceneSequence
      private void LobbySceneSequence()
      {
         _currentSceneSequencer = new LobbySceneSequencer(this);
         _currentSceneSequencer.OnCompleted += OnLobbySceneCompleted;
         _currentSceneSequencer.Run().Forget();
      }

      private void OnLobbySceneCompleted(bool isCompleted)
      {
         _currentSceneSequencer.OnCompleted -= OnLobbySceneCompleted;
         _currentSceneSequencer.Dispose();
      }
      #endregion

      public void PopupUI(string uiName)
      {
         if (UIManager.Instance == null)
         {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            canvas.gameObject.AddComponent<UIManager>();
         }
         UIManager.Instance.ShowPopup(uiName);
      }
   }
}
