using Cumic.Events;
using Sirenix.OdinInspector;
using TrainDefense.Game;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
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
        [BoxGroup("Engage Ready")]
        private ShopButtonUI shopButtonUI;

        [SerializeField]
        [BoxGroup("Engage Start")]
        private GameObject _engageStartUI;

        [SerializeField]
        [BoxGroup("Stage End")]
        private StageResultUI _stageResultUI;
        #endregion

        private bool firstTriChoice = false;

        private void Awake()
        {
            if (TimeManager.Instance == null) return;

            TimeManager.Instance.Pause();
        }

        private void Start()
        {
            SubscribeEvents();
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
            GameEventSystem.Subscribe<InspectionStartEvent>(OnInspectionStart);
            GameEventSystem.Subscribe<LevelUpEvent>(LevelUp);
            GameEventSystem.Subscribe<TriChoiceSelectEvent>(TriChoiceSelect);
            GameEventSystem.Subscribe<StageEndEvent>(StageEnd);
            GameEventSystem.Subscribe<GameEndEvent>(GameEnd);
        }


        private void UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<GameEnterEvent>(GameEnter);
            GameEventSystem.Unsubscribe<EngageReadyEvent>(EngageReady);
            GameEventSystem.Unsubscribe<EngageStartEvent>(EngageStart);
            GameEventSystem.Unsubscribe<InspectionStartEvent>(OnInspectionStart);
            GameEventSystem.Unsubscribe<LevelUpEvent>(LevelUp);
            GameEventSystem.Unsubscribe<TriChoiceSelectEvent>(TriChoiceSelect);
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
            if (triChoiceUI != null)
            {
                triChoiceUI.OnInspectionEnter(1);
            }
        }

        private void EngageReady(EngageReadyEvent engageReadyEvent)
        {
            if (_engageStartUI != null)
            {
                _engageStartUI.SetActive(false);
            }
            if (_stageResultUI != null)
            {
                _stageResultUI.gameObject.SetActive(false);
            }
        }

        private void LevelUp(LevelUpEvent levelUpEvent)
        {
            if (triChoiceUI != null)
            {
                triChoiceUI.OnInspectionEnter(levelUpEvent.LevelUpCount);
            }
        }

        private void OnInspectionStart(InspectionStartEvent inspectionStartEvent)
        {
            // 모든 적 유닛 제거
            MonsterSpawner spawner = FindFirstObjectByType<MonsterSpawner>();
            if (spawner != null)
            {
                spawner.DestroyAllMonsters();
            }

            if (shopButtonUI != null)
            {
                shopButtonUI.gameObject.SetActive(true);
                shopButtonUI.OnShopOpen();
            }
        }

        private void TriChoiceSelect(TriChoiceSelectEvent triChoiceSelectEvent)
        {
            if (triChoiceSelectEvent.ChoiceLeftCount == 0)
            {
                GameEventSystem.Publish(new EngageStartEvent());
            }

            if (!firstTriChoice)
            {
                firstTriChoice = true;
                MonsterSpawner.Instance.StartSpawnMonster();
                return;
            }
        }

        private void EngageStart(EngageStartEvent engageStartEvent)
        {
            TimeManager.Instance.Resume();

            if (_engageStartUI != null)
            {
                _engageStartUI.SetActive(true);
            }
            if (shopButtonUI != null)
            {
                shopButtonUI.gameObject.SetActive(false);
            }
            if (_stageResultUI != null)
            {
                _stageResultUI.gameObject.SetActive(false);
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
            if (shopButtonUI != null)
            {
                shopButtonUI.gameObject.SetActive(false);
            }
        }

        private void GameEnd(GameEndEvent gameEndEvent)
        {
            if (_stageResultUI != null)
            {
                _stageResultUI.gameObject.SetActive(true);
                _stageResultUI.ShowResult(gameEndEvent.IsClear);
            }
            if (_engageStartUI != null)
            {
                _engageStartUI.SetActive(false);
            }
            if (shopButtonUI != null)
            {
                shopButtonUI.gameObject.SetActive(false);
            }
        }
    }
}