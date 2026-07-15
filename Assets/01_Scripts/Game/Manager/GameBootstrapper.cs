using Cumic;
using Cumic.Events;
using TrainDefense.Game;
using TrainDefense.Game.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.Manager
{
    public class GameBootstrapper : MonoBehaviour
    {
        private const string DatabaseManagerPrefabPath = "Prefabs/DatabaseManager";
        private const string SoundManagerPrefabPath = "Prefabs/SoundManager";
        private const string RatingPopupManagerPrefabPath = "Prefabs/RatingPopupManager";
        private const string AchievementToastPrefabPath = "Prefabs/UI/Achievement_Toast";

        // 씬과 무관하게 항상 최상단에 떠야 하는 알림 연출(업적 토스트/맵 이름 배너) 전용 캔버스.
        // DontDestroyOnLoad로 유지되므로 로비 재진입 시 중복 생성을 막기 위해 static으로 추적한다.
        private static GameObject _globalOverlayCanvas;

        private void Start()
        {
            GameEventSystem.Subscribe<LobbyEnterEvent>(OnLobbyEnter);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<LobbyEnterEvent>(OnLobbyEnter);
        }

        private void OnLobbyEnter(LobbyEnterEvent lobbyEnterEvent)
        {
            if (DatabaseManager.Instance == null)
            {
                Instantiate(Resources.Load<DatabaseManager>(DatabaseManagerPrefabPath));
            }

            if (SoundManager.Instance == null)
            {
                SoundManager soundManager = Instantiate(Resources.Load<SoundManager>(SoundManagerPrefabPath));
                soundManager.PlayBGMOnInit();
            }

            // 평점 팝업 매니저도 DatabaseManager/SoundManager와 동일하게 프리팹을 Resources.Load 후 부트스트랩한다.
            // (dontDestroyOnLoad=true라 씬 전환에도 유지되어 게임 씬의 패배 이벤트를 받고 로비에서 팝업을 띄운다)
            if (RatingPopupManager.Instance == null)
            {
                Instantiate(Resources.Load<RatingPopupManager>(RatingPopupManagerPrefabPath));
            }

            _EnsureGlobalOverlayUI();
        }

        // 업적 토스트/맵 이름 배너를 담는 전역 오버레이 캔버스를 1회 생성한다.
        // 씬 Canvas(sortingOrder 0)·팝업 Canvas(100~500)보다 높은 900이라 어떤 화면 위에서도 보인다.
        // 알림 연출 전용이므로 GraphicRaycaster는 붙이지 않는다(아래 UI 클릭 차단 방지).
        private void _EnsureGlobalOverlayUI()
        {
            if (_globalOverlayCanvas != null)
                return;

            _globalOverlayCanvas = new GameObject("GlobalOverlayCanvas");
            _globalOverlayCanvas.layer = LayerMask.NameToLayer("UI");
            DontDestroyOnLoad(_globalOverlayCanvas);

            var canvas = _globalOverlayCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;

            var scaler = _globalOverlayCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var toastPrefab = Resources.Load<GameObject>(AchievementToastPrefabPath);

            if (toastPrefab != null)
                Instantiate(toastPrefab, _globalOverlayCanvas.transform, false);
            else
                Debug.LogError($"[GameBootstrapper] 업적 토스트 프리팹 로드 실패: {AchievementToastPrefabPath}");

            var bannerGo = new GameObject("MapNameBanner", typeof(RectTransform));
            bannerGo.layer = LayerMask.NameToLayer("UI");
            bannerGo.transform.SetParent(_globalOverlayCanvas.transform, false);
            bannerGo.AddComponent<MapNameBannerUI>();
        }
    }
}
