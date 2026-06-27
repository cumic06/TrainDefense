using Cumic;
using Cumic.Events;
using TrainDefense.Game;
using TrainDefense.Game.UI;
using UnityEngine;

namespace TrainDefense.Game.Manager
{
    public class GameBootstrapper : MonoBehaviour
    {
        private const string DatabaseManagerPrefabPath = "Prefabs/DatabaseManager";
        private const string SoundManagerPrefabPath = "Prefabs/SoundManager";
        private const string RatingPopupManagerPrefabPath = "Prefabs/RatingPopupManager";

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
        }
    }
}
