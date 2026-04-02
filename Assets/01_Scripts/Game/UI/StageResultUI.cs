using Cumic.Events;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game.UI
{
    public class StageResultUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private GameObject clearResultUI;
        [SerializeField]
        private GameObject failResultUI;
        #endregion

        private void Start()
        {
            GameEventSystem.Subscribe<EngageReadyEvent>(OnEngageReady);
            GameEventSystem.Subscribe<EngageStartEvent>(OnEngageStart);
            GameEventSystem.Subscribe<StageEndEvent>(OnStageEnd);
            GameEventSystem.Subscribe<GameEndEvent>(OnGameEnd);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<EngageReadyEvent>(OnEngageReady);
            GameEventSystem.Unsubscribe<EngageStartEvent>(OnEngageStart);
            GameEventSystem.Unsubscribe<StageEndEvent>(OnStageEnd);
            GameEventSystem.Unsubscribe<GameEndEvent>(OnGameEnd);
        }

        private void OnEngageReady(EngageReadyEvent engageReadyEvent)
        {
            gameObject.SetActive(false);
        }

        private void OnEngageStart(EngageStartEvent engageStartEvent)
        {
            gameObject.SetActive(false);
        }

        private void OnStageEnd(StageEndEvent stageEndEvent)
        {
            gameObject.SetActive(true);
            ShowResult(stageEndEvent.IsClear);
        }

        private void OnGameEnd(GameEndEvent gameEndEvent)
        {
            gameObject.SetActive(true);
            ShowResult(gameEndEvent.IsClear);
        }

        public void ShowResult(bool isClear)
        {
            if (isClear)
            {
                if (clearResultUI != null)
                {
                    clearResultUI.SetActive(true);
                }
                if (failResultUI != null)
                {
                    failResultUI.SetActive(false);
                }
            }
            else
            {
                if (failResultUI != null)
                {
                    failResultUI.SetActive(true);
                }
                if (clearResultUI != null)
                {
                    clearResultUI.SetActive(false);
                }
            }
        }
    }
}