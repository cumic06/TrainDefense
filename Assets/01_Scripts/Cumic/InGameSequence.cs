using Cumic.Events;
using Sirenix.OdinInspector;
using TrainDefense.Game;
using TrainDefense.Game.Datas;
using UnityEngine;

namespace Cumic.Sequence
{
    public class InGameSequence : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        [BoxGroup("Engage Start")]
        private GameObject _engageStartUI;
        #endregion

        private void Start()
        {
            SubscribeEvents();
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayBGM(SoundType.BGM_Stage);
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        #region Events
        private void SubscribeEvents()
        {
            GameEventSystem.Subscribe<GameEnterEvent>(GameEnter);
            GameEventSystem.Subscribe<EngageReadyEvent>(EngageReady);
            GameEventSystem.Subscribe<EngageStartEvent>(EngageStart);
            GameEventSystem.Subscribe<StageEndEvent>(StageEnd);
            GameEventSystem.Subscribe<GameEndEvent>(GameEnd);
        }

        private void UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<GameEnterEvent>(GameEnter);
            GameEventSystem.Unsubscribe<EngageReadyEvent>(EngageReady);
            GameEventSystem.Unsubscribe<EngageStartEvent>(EngageStart);
            GameEventSystem.Unsubscribe<StageEndEvent>(StageEnd);
            GameEventSystem.Unsubscribe<GameEndEvent>(GameEnd);
        }
        #endregion

        public void GameEnterHandler()
        {
            GameEventSystem.Publish(new GameEnterEvent());
        }

        private void GameEnter(GameEnterEvent gameEnterEvent)
        {
        }

        private void EngageReady(EngageReadyEvent engageReadyEvent)
        {
            if (_engageStartUI != null)
            {
                _engageStartUI.SetActive(false);
            }
        }

        private void EngageStart(EngageStartEvent engageStartEvent)
        {
            if (_engageStartUI != null)
            {
                _engageStartUI.SetActive(true);
            }
        }

        private void StageEnd(StageEndEvent stageEndEvent)
        {
            if (_engageStartUI != null)
            {
                _engageStartUI.SetActive(false);
            }
        }

        private void GameEnd(GameEndEvent gameEndEvent)
        {
            if (_engageStartUI != null)
            {
                _engageStartUI.SetActive(false);
            }
        }
    }
}
