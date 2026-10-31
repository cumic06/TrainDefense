using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrainDefense.Game.UI.Difficulty
{
    /// <summary>
    /// 로비 시작 버튼을 "바로 출발"에서 "난이도 팝업 열기"로 바꾼다.
    /// 로비 씬을 편집하지 않는 것이 이 프로젝트의 관례라(<see cref="RunSave.RunContinueAutoEntry"/>와 같은 이유)
    /// 씬 로드 훅에서 시작 버튼의 씬 배선(ChangeSceneButton.OnClickNextScene)을 끄고 팝업 리스너를 붙인다.
    /// 출발 자체는 팝업의 카드가 같은 SceneController.NextScene으로 한다.
    /// </summary>
    public static class DifficultySelectEntry
    {
        private const string LOBBY_SCENE_NAME = "01_LobbyScene";
        private const string START_BUTTON_NAME = "StartButton";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void _Bootstrap()
        {
            // 도메인 리로드 없이 플레이를 반복해도 중복 구독되지 않게 한 번 떼고 붙인다.
            SceneManager.sceneLoaded -= _OnSceneLoaded;
            SceneManager.sceneLoaded += _OnSceneLoaded;
        }

        private static void _OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != LOBBY_SCENE_NAME)
                return;

            GameObject startButtonObject = GameObject.Find(START_BUTTON_NAME);

            if (startButtonObject == null || !startButtonObject.TryGetComponent(out Button startButton))
            {
                Debug.LogError($"[Difficulty] 로비에서 {START_BUTTON_NAME}을 찾지 못해 난이도 팝업을 붙이지 못했습니다. 시작은 바로 출발합니다.");

                return;
            }

            int persistentCount = startButton.onClick.GetPersistentEventCount();
            for (int i = 0; i < persistentCount; i++)
                startButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);

            Transform canvasRoot = startButton.GetComponentInParent<Canvas>().rootCanvas.transform;
            startButton.onClick.AddListener(() => DifficultySelectPopupUI.Open(canvasRoot));
        }
    }
}
