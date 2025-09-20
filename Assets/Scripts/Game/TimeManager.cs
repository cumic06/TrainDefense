using Cumic;
using Cumic.Events;
using UnityEngine;

namespace TrainDefense.Game
{
    public class TimeManager : Singleton<TimeManager>
    {
        private void Start()
        {
            Resume();
            GameEventSystem.Subscribe<GameEndEvent>(OnGameEnd);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<GameEndEvent>(OnGameEnd);
        }

        private void OnGameEnd(GameEndEvent gameEndEvent)
        {
            Pause();
        }

        public void Pause()
        {
            Time.timeScale = 0;
        }

        public void Resume()
        {
            Time.timeScale = 1;
        }
    }
}