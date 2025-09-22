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
            GameEventSystem.Subscribe<StageEndEvent>(OnStageEnd);
            GameEventSystem.Subscribe<EngageReadyEvent>(OnEngageReady);
            GameEventSystem.Subscribe<EngageStartEvent>(OnEngageStart);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<GameEndEvent>(OnGameEnd);
            GameEventSystem.Unsubscribe<StageEndEvent>(OnStageEnd);
            GameEventSystem.Unsubscribe<EngageReadyEvent>(OnEngageReady);
            GameEventSystem.Unsubscribe<EngageStartEvent>(OnEngageStart);
        }

        private void OnStageEnd(StageEndEvent stageEndEvent)
        {
            Pause();
        }

        private void OnGameEnd(GameEndEvent gameEndEvent)
        {
            Pause();
        }

        private void OnEngageReady(EngageReadyEvent engageReadyEvent)
        {
            Pause();
        }

        private void OnEngageStart(EngageStartEvent engageStartEvent)
        {
            Resume();
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