using System.Collections;
using Cumic.Events;
using Sirenix.OdinInspector;
using TrainDefense.Game;
using TrainDefense.Game.UI;
using UnityEngine;
using UnityEngine.Events;

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
            GameEventSystem.Subscribe<GameEnterEvent>(GameEnter);
            GameEventSystem.Subscribe<EngageReadyEvent>(EngageReady);
            GameEventSystem.Subscribe<EngageStartEvent>(EngageStart);
            GameEventSystem.Subscribe<StageEndEvent>(StageEnd);
            GameEventSystem.Subscribe<GameEndEvent>(GameEnd);

            StartCoroutine(GameEnterCoroutine());
        }

        private IEnumerator GameEnterCoroutine()
        {
            yield return null;
            GameEventSystem.Publish(new GameEnterEvent());
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<GameEnterEvent>(GameEnter);
            GameEventSystem.Unsubscribe<EngageReadyEvent>(EngageReady);
            GameEventSystem.Unsubscribe<EngageStartEvent>(EngageStart);
            GameEventSystem.Unsubscribe<StageEndEvent>(StageEnd);
            GameEventSystem.Unsubscribe<GameEndEvent>(GameEnd);
        }

        private void GameEnter(GameEnterEvent gameEnterEvent)
        {
            if (triChoiceUI != null)
            {
                triChoiceUI.OnInspectionEnter();
            }
        }

        private void EngageReady(EngageReadyEvent engageReadyEvent)
        {
            if (triChoiceUI != null)
            {
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
            TimeManager.Instance.Resume();

            if (_engageStartUI != null)
            {
                _engageStartUI.SetActive(true);
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