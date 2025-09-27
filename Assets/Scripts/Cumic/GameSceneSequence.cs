using Cysharp.Threading.Tasks;
using UnityEngine;
using Cumic.Checker;

namespace Cumic.Sequence
{
    public class GameSceneSequence : MonoBehaviour
    {
        private ISceneSequencer _currentSceneSequencer;
        private UserDataManager _userDataManager;

        private void Start()
        {
            DontDestroyOnLoad(gameObject);

            AuthSceneSequence();
        }

        #region AuthSceneSequence
        private void AuthSceneSequence()
        {
            _currentSceneSequencer = new AuthSceneSequencer(this, new VersionChecker(), new AuthChecker());
            _currentSceneSequencer.OnCompleted += OnAuthSceneCompleted;
            _currentSceneSequencer.Run().Forget();
        }

        private void OnAuthSceneCompleted(bool isCompleted)
        {
            _currentSceneSequencer.OnCompleted -= OnAuthSceneCompleted;
            _currentSceneSequencer.Dispose();

            if (isCompleted)
            {
                Debug.Log("AuthSceneCompleted");

                SceneController.NextScene();

                LobbySceneSequence();
                
                if (_userDataManager == null)
                {
                    UserDataManager userDataManager = new GameObject("UserDataManager").AddComponent<UserDataManager>();
                    _userDataManager = userDataManager;
                }
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