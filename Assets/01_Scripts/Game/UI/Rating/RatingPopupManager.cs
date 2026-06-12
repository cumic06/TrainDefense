using System.Collections.Generic;
using Cumic.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrainDefense.Game.UI
{
    // 평점 팝업 트리거. 씬 와이어링 없이 동작하도록 RuntimeInitializeOnLoadMethod로 부트스트랩하고,
    // 패배(GameEndEvent.IsClear == false)를 기록해뒀다가 로비 씬 진입 시
    // 조건 목록을 모두 통과하면 팝업을 띄운다. 표시 조건 확장은 _conditions에 추가한다.
    public static class RatingPopupManager
    {
        private const string LobbySceneName = "01_LobbyScene";
        private const string PopupResourceName = "Popup_Rating";

        private static readonly List<IRatingPopupCondition> _conditions = new()
        {
            new NotRatedCondition(),
            new NewDefeatCondition(),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void _Initialize()
        {
            GameEventSystem.Subscribe<GameEndEvent>(_OnGameEnd);
            SceneManager.sceneLoaded += _OnSceneLoaded;
        }

        private static void _OnGameEnd(GameEndEvent gameEndEvent)
        {
            if (gameEndEvent.IsClear)
                return;

            RatingRecord.AddDefeat();
        }

        private static void _OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != LobbySceneName)
                return;

            if (!_IsAllConditionsSatisfied())
                return;

            _ShowPopup();
        }

        private static bool _IsAllConditionsSatisfied()
        {
            foreach (IRatingPopupCondition condition in _conditions)
            {
                if (!condition.IsSatisfied())
                    return false;
            }

            return true;
        }

        private static void _ShowPopup()
        {
            GameObject popupPrefab = Resources.Load<GameObject>(PopupResourceName);

            if (popupPrefab == null)
            {
                Debug.LogError($"[RatingPopup] Resources에서 {PopupResourceName} 프리팹을 찾을 수 없습니다.");

                return;
            }

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();

            if (canvas == null)
            {
                Debug.LogError("[RatingPopup] 로비 씬에서 Canvas를 찾을 수 없습니다.");

                return;
            }

            Object.Instantiate(popupPrefab, canvas.transform);
            RatingRecord.MarkShown();
        }
    }
}
