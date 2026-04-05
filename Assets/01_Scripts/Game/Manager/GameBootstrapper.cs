using Cumic;
using Cumic.Events;
using TrainDefense.Game;
using UnityEngine;

namespace TrainDefense.Game.Manager
{
    public class GameBootstrapper : MonoBehaviour
    {
        private const string DatabaseManagerPrefabPath = "Prefabs/DatabaseManager";
        private const string SoundManagerPrefabPath = "Prefabs/SoundManager";

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
        }
    }
}
