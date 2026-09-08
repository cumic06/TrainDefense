using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrainDefense.Game.RunSave
{
    /// <summary>
    /// 로비 씬이 로드될 때마다 "이어하기" 버튼 프리팹을 로비 Canvas 아래에 배치한다.
    /// 버튼의 모양·배치는 전부 프리팹(<c>Resources/Prefabs/UI/Btn_ContinueRun</c>)에 정적으로 정의돼 있고,
    /// 여기서는 Instantiate만 한다(런타임 UI 조립 금지).
    ///
    /// 로비 씬 자체를 편집하지 않는 것이 이 프로젝트의 관례라 주입 방식을 쓴다.
    /// <see cref="Events.LobbyEnterEvent"/>는 로비 씬이 실제로 로드되기 전에 발행되므로
    /// 그 이벤트로 붙이면 Canvas를 찾지 못한다 — 씬 로드 완료를 기준으로 삼는다.
    /// </summary>
    public static class RunContinueLobbyInjector
    {
        private const string LOBBY_SCENE_NAME = "01_LobbyScene";
        private const string BUTTON_PREFAB_PATH = "Prefabs/UI/Btn_ContinueRun";
        private const string LOBBY_UI_ROOT_NAME = "@UI";
        private const string LOBBY_CANVAS_NAME = "Canvas";

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

            if (Object.FindFirstObjectByType<RunContinueButton>(FindObjectsInactive.Include) != null)
                return;

            var buttonPrefab = Resources.Load<GameObject>(BUTTON_PREFAB_PATH);

            if (buttonPrefab == null)
            {
                Debug.LogError($"[RunSave] 이어하기 버튼 프리팹 로드 실패: {BUTTON_PREFAB_PATH}");

                return;
            }

            var canvasTransform = _FindLobbyCanvas(scene);

            if (canvasTransform == null)
            {
                Debug.LogWarning("[RunSave] 로비 Canvas를 찾지 못해 이어하기 버튼을 배치하지 못했습니다.");

                return;
            }

            Object.Instantiate(buttonPrefab, canvasTransform, false);
        }

        // 로비의 메인 UI 캔버스(@UI/Canvas)를 찾는다. 이름이 바뀌었으면 씬 안의 첫 Canvas로 폴백한다.
        private static Transform _FindLobbyCanvas(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != LOBBY_UI_ROOT_NAME)
                    continue;

                var canvas = root.transform.Find(LOBBY_CANVAS_NAME);

                if (canvas != null)
                    return canvas;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                var canvas = root.GetComponentInChildren<Canvas>(true);

                if (canvas != null)
                    return canvas.transform;
            }

            return null;
        }
    }
}
