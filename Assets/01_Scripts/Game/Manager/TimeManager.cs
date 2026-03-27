using UnityEngine;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Events;

namespace TrainDefense.Game
{
    public class TimeManager : Singleton<TimeManager>
    {
        private void Start()
        {
            Resume();
            GameEventSystem.Subscribe<GameEnterEvent>(OnGameEnter);
            GameEventSystem.Subscribe<EngageReadyEvent>(OnEngageReady);
            GameEventSystem.Subscribe<EngageStartEvent>(OnEngageStart);
            GameEventSystem.Subscribe<LevelUpEvent>(OnLevelUp);
            GameEventSystem.Subscribe<InspectionStartEvent>(OnInspectionStart);
            GameEventSystem.Subscribe<StageEndEvent>(OnStageEnd);
            GameEventSystem.Subscribe<GameEndEvent>(OnGameEnd);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
            GameEventSystem.Unsubscribe<EngageReadyEvent>(OnEngageReady);
            GameEventSystem.Unsubscribe<EngageStartEvent>(OnEngageStart);
            GameEventSystem.Unsubscribe<LevelUpEvent>(OnLevelUp);
            GameEventSystem.Unsubscribe<InspectionStartEvent>(OnInspectionStart);
            GameEventSystem.Unsubscribe<StageEndEvent>(OnStageEnd);
            GameEventSystem.Unsubscribe<GameEndEvent>(OnGameEnd);
        }

        private void OnGameEnter(GameEnterEvent gameEnterEvent)
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
        
        private void OnStageEnd(StageEndEvent stageEndEvent)
        {
            Pause();
        }

        private void OnGameEnd(GameEndEvent gameEndEvent)
        {
            Pause();
        }

        private void OnLevelUp(LevelUpEvent levelUpEvent)
        {
            Pause();
        }

        private void OnInspectionStart(InspectionStartEvent inspectionStartEvent)
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