using Cumic.Events;
using Sirenix.OdinInspector;
using TrainDefense.Game.UI;
using UnityEngine;

namespace Cumic.Sequence
{
    public class InGameSequence : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        [BoxGroup("Engage Ready")]
        private TriChoiceUI triChoiceUI;

        [SerializeField]
        [BoxGroup("Engage Start")]
        private GameObject _engageStartUI;

        [SerializeField]
        [BoxGroup("Stage End")]
        private StageResultUI _stageResultUI;

        [SerializeField]
        [BoxGroup("Game End")]
        private GameObject _gameEndUI;
        #endregion

        private void Start()
        {
            GameEventSystem.Subscribe<EngageReadyEvent>(EngageReady);
            GameEventSystem.Subscribe<EngageStartEvent>(EngageStart);
            GameEventSystem.Subscribe<StageEndEvent>(StageEnd);
            GameEventSystem.Subscribe<GameEndEvent>(GameEnd);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<EngageReadyEvent>(EngageReady);
            GameEventSystem.Unsubscribe<EngageStartEvent>(EngageStart);
            GameEventSystem.Unsubscribe<StageEndEvent>(StageEnd);
            GameEventSystem.Unsubscribe<GameEndEvent>(GameEnd);
        }

        private void EngageReady(EngageReadyEvent engageReadyEvent)
        {
            if (triChoiceUI != null)
            {
                triChoiceUI.gameObject.SetActive(true);
                triChoiceUI.OnInspectionEnter();
            }
            if (_engageStartUI != null)
            {
                _engageStartUI.SetActive(false);
            }
            if (_stageResultUI != null)
            {
                _stageResultUI.gameObject.SetActive(false);
            }
            if (_gameEndUI != null)
            {
                _gameEndUI.SetActive(false);
            }
        }

        private void EngageStart(EngageStartEvent engageStartEvent)
        {
            if (_engageStartUI != null)
            {
                _engageStartUI.SetActive(true);
            }
            if (triChoiceUI != null)
            {
                triChoiceUI.gameObject.SetActive(false);
            }
            if (_stageResultUI != null)
            {
                _stageResultUI.gameObject.SetActive(false);
            }
            if (_gameEndUI != null)
            {
                _gameEndUI.SetActive(false);
            }
        }

        private void StageEnd(StageEndEvent stageEndEvent)
        {
            if (_stageResultUI != null)
            {
                _stageResultUI.gameObject.SetActive(true);
                _stageResultUI.ShowResult(stageEndEvent.IsClear);
            }
            if (triChoiceUI != null)
            {
                triChoiceUI.gameObject.SetActive(false);
            }
            if (_engageStartUI != null)
            {
                _engageStartUI.SetActive(false);
            }
            if (_gameEndUI != null)
            {
                _gameEndUI.SetActive(false);
            }
        }

        private void GameEnd(GameEndEvent gameEndEvent)
        {
            if (_gameEndUI != null)
            {
                _gameEndUI.SetActive(true);
            }
            if (triChoiceUI != null)
            {
                triChoiceUI.gameObject.SetActive(false);
            }
            if (_engageStartUI != null)
            {
                _engageStartUI.SetActive(false);
            }
            if (_stageResultUI != null)
            {
                _stageResultUI.gameObject.SetActive(false);
            }
        }
    }
}