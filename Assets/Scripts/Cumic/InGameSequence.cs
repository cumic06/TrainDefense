using Cumic.Events;
using Sirenix.OdinInspector;
using TrainDefense.Game;
using TrainDefense.Game.UI;
using UnityEngine;

namespace Cumic
{
    public class InGameSequence : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        [BoxGroup("Engage Ready")]
        private TriChoiceUI engageReadyUI;

        [SerializeField]
        [BoxGroup("Engage Start")]
        private GameObject engageStartUI;
        [SerializeField]
        [BoxGroup("Engage Start")]
        private MonsterSpawner monsterSpawner;
        [SerializeField]
        [BoxGroup("Engage Start")]
        private TimeManager timeManager;

        [SerializeField]
        [BoxGroup("Stage End")]
        private StageResultUI stageResultUI;

        [SerializeField]
        [BoxGroup("Game End")]
        private GameObject gameEndUI;
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
            timeManager?.Pause();
            monsterSpawner?.StopSpawnMonster();
            engageReadyUI?.OnInspectionEnter();

            if (engageStartUI != null)
            {
                engageStartUI.SetActive(false);
            }
            if (stageResultUI != null)
            {
                stageResultUI.gameObject.SetActive(false);
            }
            if (gameEndUI != null)
            {
                gameEndUI.SetActive(false);
            }
        }

        private void EngageStart(EngageStartEvent engageStartEvent)
        {
            timeManager?.Resume();
            monsterSpawner?.StartSpawnMonster();

            if (engageStartUI != null)
            {
                engageStartUI.SetActive(true);
            }
            if (stageResultUI != null)
            {
                stageResultUI.gameObject.SetActive(false);
            }
            if (gameEndUI != null)
            {
                gameEndUI.SetActive(false);
            }
        }

        private void StageEnd(StageEndEvent stageEndEvent)
        {
            if (stageResultUI != null)
            {
                stageResultUI.gameObject.SetActive(true);
                stageResultUI.ShowResult(stageEndEvent.IsClear);
            }
            if (engageStartUI != null)
            {
                engageStartUI.SetActive(false);
            }
            if (gameEndUI != null)
            {
                gameEndUI.SetActive(false);
            }
        }

        private void GameEnd(GameEndEvent gameEndEvent)
        {
            if (gameEndUI != null)
            {
                gameEndUI.SetActive(true);
            }
            if (engageStartUI != null)
            {
                engageStartUI.SetActive(false);
            }
            if (stageResultUI != null)
            {
                stageResultUI.gameObject.SetActive(false);
            }
        }
    }
}