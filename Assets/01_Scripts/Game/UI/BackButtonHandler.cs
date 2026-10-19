using Cumic.Sequence;
using TrainDefense.Game.Intro;
using TrainDefense.Game.Tutorial;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 안드로이드 뒤로가기(= Escape 키) 처리.
    /// 열린 팝업이 있으면 맨 위 팝업의 닫기 버튼을 누른 것과 같게, 전투 중이면 일시정지를 열고, 로비에서는 종료 버튼과 같게 끝낸다.
    /// 튜토리얼·인트로 중에는 아무것도 하지 않는다(인트로는 Escape 길게 누르기로 스스로 스킵한다).
    /// 씬에 배치하지 않고 실행 시 스스로 생긴다.
    /// </summary>
    public class BackButtonHandler : MonoBehaviour
    {
        private const string LOBBY_SCENE_NAME = "01_LobbyScene";
        private const string GAME_SCENE_NAME = "02_GameScene";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void _Bootstrap()
        {
            var handlerObject = new GameObject(nameof(BackButtonHandler));
            DontDestroyOnLoad(handlerObject);
            handlerObject.AddComponent<BackButtonHandler>();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            _HandleBack();
        }

        private void _HandleBack()
        {
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsActive)
                return;

            if (IntroManager.Instance != null && IntroManager.Instance.gameObject.activeInHierarchy)
                return;

            if (_CloseTopPopup())
                return;

            string sceneName = SceneManager.GetActiveScene().name;

            if (sceneName == LOBBY_SCENE_NAME)
            {
                Application.Quit();
                return;
            }

            if (sceneName == GAME_SCENE_NAME)
                _OpenPauseIfInBattle();
        }

        // 맨 위 팝업의 닫기(또는 취소) 버튼을 누른 것과 같게 닫는다. 닫기 버튼이 없는 팝업은 건드리지 않는다.
        private static bool _CloseTopPopup()
        {
            GameObject top = PopupTween.TopOpenRoot;

            if (top == null)
                return false;

            foreach (Button button in top.GetComponentsInChildren<Button>(true))
            {
                string buttonName = button.name.ToLowerInvariant();

                if (!buttonName.Contains("close") && !buttonName.Contains("cancel"))
                    continue;

                if (!button.interactable)
                    continue;

                button.onClick.Invoke();
                return true;
            }

            return false;
        }

        private static void _OpenPauseIfInBattle()
        {
            if (InGameSequence.Instance == null)
                return;

            if (InGameSequence.Instance.CurrentBase != BasePhase.Engage || InGameSequence.Instance.CurrentOverlays != OverlayPhase.None)
                return;

            var pauseUI = FindFirstObjectByType<PauseUI>(FindObjectsInactive.Include);

            if (pauseUI != null)
                pauseUI.OnClickPauseButton();
        }
    }
}
