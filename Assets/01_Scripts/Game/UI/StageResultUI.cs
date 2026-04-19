using Cumic;
using Cumic.Events;
using DG.Tweening;
using TMPro;
using TrainDefense.Game.Manager;
using UnityEngine;

namespace TrainDefense.Game.UI
{
    public class StageResultUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private GameObject background;
        [SerializeField]
        private GameObject resultUI;
        [SerializeField]
        private GameObject clearResultUI;
        [SerializeField]
        private GameObject failResultUI;
        [SerializeField]
        private TMP_Text scoreText;
        [SerializeField]
        private float scoreTweenDuration = 1.0f;
        #endregion

        private Tween _scoreTween;

        private void Start()
        {
            HideResultUIs();

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
            HideResultUIs();
        }

        private void OnEngageStart(EngageStartEvent engageStartEvent)
        {
            HideResultUIs();
        }

        private void OnStageEnd(StageEndEvent stageEndEvent)
        {
            ShowResult(stageEndEvent.IsClear);
        }

        private void OnGameEnd(GameEndEvent gameEndEvent)
        {
            ShowResult(gameEndEvent.IsClear);
        }

        private void HideResultUIs()
        {
            if (background != null)
            {
                background.SetActive(false);
            }
            if (clearResultUI != null)
            {
                clearResultUI.SetActive(false);
            }
            if (failResultUI != null)
            {
                failResultUI.SetActive(false);
            }
        }

        public void ShowResult(bool isClear)
        {
            if (background != null)
            {
                background.SetActive(true);
            }

            if (resultUI != null)
            {
                resultUI.SetActive(true);
            }

            if (clearResultUI != null)
            {
                clearResultUI.SetActive(isClear);
            }
            if (failResultUI != null)
            {
                failResultUI.SetActive(!isClear);
            }

            _AnimateScore();
        }

        private void _AnimateScore()
        {
            if (scoreText == null || ScoreManager.Instance == null) return;

            _scoreTween?.Kill();

            int target = ScoreManager.Instance.CurrentScore;
            int display = 0;
            scoreText.text = $"Score: {display.ToCommaString()}";

            _scoreTween = DOTween.To(() => display, v =>
            {
                display = v;
                scoreText.text = $"Score: {display.ToCommaString()}";
            }, target, scoreTweenDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        }
    }
}