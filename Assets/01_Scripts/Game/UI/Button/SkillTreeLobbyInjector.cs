using UnityEngine;
using UnityEngine.SceneManagement;
using TrainDefense.Game.UI.Achievement;

namespace TrainDefense
{
    /// <summary>
    /// 로비 씬에 스킬트리 버튼을 코드로 주입한다 (씬 파일 수정 없이 배치 — 씬 와이어링 0).
    /// 우상단 버튼 스택(도감 -130 / 업적 -250)과 같은 부모 아래에 Btn_SkillTree 프리팹을 인스턴스화하며,
    /// 위치(-20, -370)는 프리팹 RectTransform에 구워져 있어 오버라이드가 필요 없다.
    /// </summary>
    public static class SkillTreeLobbyInjector
    {
        private const string LobbySceneName = "01_LobbyScene";
        private const string ButtonResourcePath = "Prefabs/UI/Btn_SkillTree";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void _Register()
        {
            SceneManager.sceneLoaded -= _OnSceneLoaded;
            SceneManager.sceneLoaded += _OnSceneLoaded;
            // 첫 씬이 이미 로비인 경우 sceneLoaded를 못 받으므로 즉시 1회 시도
            _TryInject(SceneManager.GetActiveScene());
        }

        private static void _OnSceneLoaded(Scene scene, LoadSceneMode mode) => _TryInject(scene);

        private static void _TryInject(Scene scene)
        {
            if (scene.name != LobbySceneName) return;
            if (Object.FindFirstObjectByType<SkillTreeButton>() != null) return;

            // 업적 버튼이 우상단 버튼 스택의 기준점 — 같은 부모에 붙어야 앵커 좌표가 맞는다
            var anchorButton = Object.FindFirstObjectByType<AchievementButton>();

            if (anchorButton == null)
            {
                Debug.LogWarning("[SkillTree] 로비에서 업적 버튼을 찾지 못해 스킬트리 버튼을 주입하지 못했습니다.");

                return;
            }

            GameObject buttonPrefab = Resources.Load<GameObject>(ButtonResourcePath);

            if (buttonPrefab == null)
            {
                Debug.LogError($"[SkillTree] Resources에서 {ButtonResourcePath} 프리팹을 찾을 수 없습니다.");

                return;
            }

            Object.Instantiate(buttonPrefab, anchorButton.transform.parent, worldPositionStays: false);
        }
    }
}
