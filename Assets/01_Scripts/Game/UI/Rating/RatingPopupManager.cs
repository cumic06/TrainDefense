using System.Collections.Generic;
using Cumic;
using Cumic.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrainDefense.Game.UI
{
    // 평점 팝업 트리거. 씬에 배치된 매니저(dontDestroyOnLoad=true)가 패배(GameEndEvent.IsClear == false)를
    // 기록해뒀다가 로비 씬 진입 시 조건 목록을 모두 통과하면 팝업을 띄운다. 표시 조건 확장은 _conditions에 추가한다.
    // (RuntimeInitializeOnLoadMethod 부트스트랩 대신 씬 배치 매니저로 동작 — 첫 씬에 배치하고 dontDestroyOnLoad를 켤 것)
    public class RatingPopupManager : Singleton<RatingPopupManager>
    {
        private const string LobbySceneName = "01_LobbyScene";
        private const string PopupResourceName = "Popup_Rating";

        private readonly List<IRatingPopupCondition> _conditions = new()
        {
            new NotRatedCondition(),
            new NewDefeatCondition(),
        };

        protected override void Awake()
        {
            base.Awake();

            // 중복 인스턴스는 base.Awake에서 파괴되므로, 살아남은 인스턴스만 이벤트를 구독한다.
            if (Instance != this)
                return;

            GameEventSystem.Subscribe<GameEndEvent>(_OnGameEnd);
            SceneManager.sceneLoaded += _OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            GameEventSystem.Unsubscribe<GameEndEvent>(_OnGameEnd);
            SceneManager.sceneLoaded -= _OnSceneLoaded;
        }

        private void _OnGameEnd(GameEndEvent gameEndEvent)
        {
            if (gameEndEvent.IsClear)
                return;

            RatingRecord.AddDefeat();
        }

        private void _OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != LobbySceneName)
                return;

            if (!_IsAllConditionsSatisfied())
                return;

            _ShowPopup();
        }

        private bool _IsAllConditionsSatisfied()
        {
            foreach (IRatingPopupCondition condition in _conditions)
            {
                if (!condition.IsSatisfied())
                    return false;
            }

            return true;
        }

        private void _ShowPopup()
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

            // 루트 캔버스 자식으로 붙여 전체 화면을 채우게 하고, 맨 위로 올린다.
            // (프리팹 자체 Canvas의 overrideSorting/sortingOrder가 다른 UI 위 렌더링을 보장 — 클릭 차단 방지)
            GameObject popup = Instantiate(popupPrefab, canvas.rootCanvas.transform);
            popup.transform.SetAsLastSibling();

            RatingRecord.MarkShown();
        }
    }
}
