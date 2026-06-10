using Cumic;
using Cumic.Events;
using DG.Tweening;
using TMPro;
using TrainDefense.Game.Manager;
using TrainDefense.Localize;
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
        [Header("처치 수 연출 (없으면 스코어만 카운트업)")]
        [SerializeField]
        private TMP_Text normalKillText;
        [SerializeField]
        private TMP_Text eliteKillText;
        [SerializeField]
        private float killCountTweenDuration = 0.6f;
        [SerializeField]
        private float stageInterval = 0.35f;
        [SerializeField]
        private float scoreTweenDuration = 1.0f;
        #endregion

        private Sequence _resultSequence;

        #region LifeCycle
        private void Start()
        {
            HideResultUIs();

            _SubscribeEvents();
        }

        private void OnDestroy()
        {
            _UnsubscribeEvents();
        }
        #endregion

        #region Sub/UnSub
        private void _SubscribeEvents()
        {
            GameEventSystem.Subscribe<EngageReadyEvent>(OnEngageReady);
            GameEventSystem.Subscribe<EngageStartEvent>(OnEngageStart);
            GameEventSystem.Subscribe<GameEndEvent>(OnGameEnd);
        }

        private void _UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<EngageReadyEvent>(OnEngageReady);
            GameEventSystem.Unsubscribe<EngageStartEvent>(OnEngageStart);
            GameEventSystem.Unsubscribe<GameEndEvent>(OnGameEnd);
        }
        #endregion

        private void OnEngageReady(EngageReadyEvent engageReadyEvent)
        {
            HideResultUIs();
        }

        private void OnEngageStart(EngageStartEvent engageStartEvent)
        {
            HideResultUIs();
        }

        private void OnGameEnd(GameEndEvent gameEndEvent)
        {
            ShowResult(gameEndEvent.IsClear);
        }

        private void HideResultUIs()
        {
            _resultSequence?.Kill();

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

        // 일반 처치 수 → 엘리트 처치 수 → (일반 점수 + 엘리트 점수)를 더한 최종 스코어 순으로 카운트업한다.
        private void _AnimateScore()
        {
            if (ScoreManager.Instance == null)
                return;

            _resultSequence?.Kill();

            var scoreManager = ScoreManager.Instance;

            _ResetResultTexts();

            _resultSequence = DOTween.Sequence().SetUpdate(true);

            _AppendKillCountStage(normalKillText, "result_normal_kill", "일반 처치", scoreManager.NormalKillCount, scoreManager.NormalScore);
            _AppendKillCountStage(eliteKillText, "result_elite_kill", "엘리트 처치", scoreManager.EliteKillCount, scoreManager.EliteScore);
            _AppendScoreStage(scoreManager.CurrentScore);
        }

        private void _ResetResultTexts()
        {
            if (normalKillText != null)
            {
                normalKillText.text = string.Empty;
            }
            if (eliteKillText != null)
            {
                eliteKillText.text = string.Empty;
            }
            if (scoreText != null)
            {
                scoreText.text = $"Score: {0.ToCommaString()}";
            }
        }

        // 라벨/단위는 LocalizeHelper로 키 기반 조회(미등록 시 한글 폴백). 트윈 setter 안에서 매 프레임
        // 조회하지 않도록 시작 시 1회만 해석해 캐시한다.
        private void _AppendKillCountStage(TMP_Text targetText, string labelKey, string labelFallback, int killCount, int scoreContribution)
        {
            if (targetText == null)
                return;

            string label = LocalizeHelper.GetByKey(labelKey, labelFallback);
            string countSuffix = LocalizeHelper.GetByKey("result_kill_count_suffix", "마리");
            string scoreSuffix = LocalizeHelper.GetByKey("result_score_suffix", "점");

            int display = 0;
            targetText.text = $"{label}  {display.ToCommaString()}{countSuffix}";

            _resultSequence.Append(DOTween.To(() => display, value =>
            {
                display = value;
                targetText.text = $"{label}  {display.ToCommaString()}{countSuffix}";
            }, killCount, killCountTweenDuration).SetEase(Ease.OutCubic));

            _resultSequence.AppendCallback(() =>
            {
                targetText.text = $"{label}  {killCount.ToCommaString()}{countSuffix}   {scoreContribution.ToCommaString()}{scoreSuffix}";
            });
            _resultSequence.AppendInterval(stageInterval);
        }

        private void _AppendScoreStage(int targetScore)
        {
            if (scoreText == null)
                return;

            int display = 0;

            _resultSequence.Append(DOTween.To(() => display, value =>
            {
                display = value;
                scoreText.text = $"Score: {display.ToCommaString()}";
            }, targetScore, scoreTweenDuration).SetEase(Ease.OutCubic));
        }
    }
}
