using System.Collections.Generic;
using Cumic;
using Cumic.Events;
using DG.Tweening;
using TMPro;
using TrainDefense.Game.Manager;
using TrainDefense.Localize;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 게임오버 결산 화면(기존 StageResultUI 대체).
    /// 죽으면 한 판 동안 거쳐 간 맵들(<see cref="StageManager.RunRecords"/>)을 상단 가로 스크롤 영역에
    /// 늘어놓고, 맵1 → 그 맵에서 번 점수 → 다음 맵으로 넘어가는 식으로 자동 재생한다.
    /// 자동 재생이 끝나면 하단 별도 패널에 총점을 카운트업하고, 가로 스크롤(ScrollRect)을 풀어
    /// 좌우로 자유롭게 다시 볼 수 있게 한다. 화면을 탭하면 자동 재생을 즉시 끝낸다.
    /// </summary>
    public class StageResultSlideUI : MonoBehaviour
    {
        #region Fields
        [Header("Refs")]
        [SerializeField]
        private GameObject background;
        [Tooltip("페이드 인용(선택). 없으면 그냥 즉시 표시.")]
        [SerializeField]
        private CanvasGroup canvasGroup;
        [Tooltip("자동 재생을 스킵하는 풀스크린 버튼.")]
        [SerializeField]
        private Button skipArea;
        [SerializeField]
        private ScrollRect scrollRect;
        [Tooltip("가로 슬롯들이 담기는 Content (HorizontalLayoutGroup + ContentSizeFitter).")]
        [SerializeField]
        private RectTransform content;
        [Tooltip("맵 한 칸 프리팹 (StageResultSlot).")]
        [SerializeField]
        private StageResultSlotUI slotPrefab;

        [Header("총점 (하단 별도 패널)")]
        [Tooltip("하단 총점 박스 GameObject (스크롤 영역과 분리).")]
        [SerializeField]
        private GameObject totalPanel;
        [SerializeField]
        private TMP_Text totalScoreText;

        [Header("로비 버튼")]
        [Tooltip("맵 점수 연출이 모두 끝난 뒤 활성화되는 로비 버튼. (GameObject stripped 회피를 위해 Transform으로 연결)")]
        [SerializeField]
        private RectTransform lobbyButton;

        [Header("Timing")]
        [SerializeField]
        private float fadeInDuration = 0.3f;
        [SerializeField]
        private float slideDuration = 0.45f;
        [SerializeField]
        private float scoreCountDuration = 0.6f;
        [SerializeField]
        private float holdInterval = 0.35f;
        #endregion

        private readonly List<StageResultSlotUI> _slots = new();
        private Sequence _sequence;
        private bool _isPlaying;

        // 카운트업 setter가 매 프레임 호출되므로 라벨/접미사는 ShowResult에서 1회만 해석해 캐시한다.
        private string _totalLabel = "총점";
        private string _totalSuffix = "점";

        #region LifeCycle
        private void Start()
        {
            _Hide();
            _SubscribeEvents();

            if (skipArea != null)
                skipArea.onClick.AddListener(_OnSkip);
        }

        private void OnDestroy()
        {
            _UnsubscribeEvents();

            if (skipArea != null)
                skipArea.onClick.RemoveListener(_OnSkip);

            _sequence?.Kill();
        }
        #endregion

        #region Sub/UnSub
        private void _SubscribeEvents()
        {
            GameEventSystem.Subscribe<EngageReadyEvent>(_OnEngageReady);
            GameEventSystem.Subscribe<EngageStartEvent>(_OnEngageStart);
            GameEventSystem.Subscribe<GameEndEvent>(_OnGameEnd);
        }

        private void _UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<EngageReadyEvent>(_OnEngageReady);
            GameEventSystem.Unsubscribe<EngageStartEvent>(_OnEngageStart);
            GameEventSystem.Unsubscribe<GameEndEvent>(_OnGameEnd);
        }
        #endregion

        private void _OnEngageReady(EngageReadyEvent _) => _Hide();
        private void _OnEngageStart(EngageStartEvent _) => _Hide();
        private void _OnGameEnd(GameEndEvent _) => ShowResult();

        private void _Hide()
        {
            _sequence?.Kill();
            _isPlaying = false;

            // 게임 중에는 결산 표시 내용(배경 + 스크롤뷰 + 총점패널)을 모두 끈다. 스크립트가 붙은 루트는
            // 켜둬야 GameEndEvent를 받아 다시 켤 수 있으므로 자식만 토글한다.
            if (background != null)
                background.SetActive(false);
            if (scrollRect != null)
                scrollRect.gameObject.SetActive(false);
            if (totalPanel != null)
                totalPanel.SetActive(false);
            if (lobbyButton != null)
                lobbyButton.gameObject.SetActive(false);
        }

        public void ShowResult()
        {
            // 게임오버 시점에 결산 표시 내용을 켠다.
            if (background != null)
                background.SetActive(true);
            if (scrollRect != null)
                scrollRect.gameObject.SetActive(true);
            if (totalPanel != null)
                totalPanel.SetActive(true);
            // 로비 버튼은 연출이 끝난 뒤(_OnSequenceComplete)에만 켜지도록 시작 시 숨긴다.
            if (lobbyButton != null)
                lobbyButton.gameObject.SetActive(false);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, fadeInDuration).SetUpdate(true);
            }

            _totalLabel = LocalizeHelper.GetByKey("result_total_label", "총점");
            _totalSuffix = LocalizeHelper.GetByKey("result_score_suffix", "점");
            _SetTotalScore(0);

            _BuildSlots();
            _PlaySequence();
        }

        // RunRecords로 상단 스크롤 영역의 맵 칸들을 채운다. 슬롯은 재사용 풀(부족하면 생성, 남으면 비활성).
        private void _BuildSlots()
        {
            var records = StageManager.Instance != null ? StageManager.Instance.FinalizeAndGetRecords() : null;
            int count = records?.Count ?? 0;

            while (_slots.Count < count)
            {
                var slot = Instantiate(slotPrefab, content);
                _slots.Add(slot);
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                if (i < count)
                {
                    _slots[i].gameObject.SetActive(true);
                    _slots[i].Bind(records[i], i + 1);
                }
                else
                {
                    _slots[i].gameObject.SetActive(false);
                }
            }
        }

        // 가로 레이아웃을 확정한 뒤 맵 칸을 차례로 중앙으로 넘기며 점수를 카운트업하고,
        // 마지막에 하단 총점 패널을 카운트업한다.
        private void _PlaySequence()
        {
            _sequence?.Kill();

            var records = StageManager.Instance != null ? StageManager.Instance.FinalizeAndGetRecords() : null;
            int count = records?.Count ?? 0;

            // 가로 레이아웃을 확정해 content/viewport 폭(정규화 스크롤 계산의 기준)을 먼저 잡는다.
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            // 자동 재생 동안에는 사용자 드래그 스크롤을 잠그고, 첫 칸을 중앙에 둔다.
            if (scrollRect != null)
            {
                scrollRect.horizontal = false;
                scrollRect.horizontalNormalizedPosition = _NormalizedFor(0, count);
            }
            _isPlaying = true;

            _sequence = DOTween.Sequence().SetUpdate(true);

            for (int i = 0; i < count; i++)
            {
                int idx = i;

                // 두 번째 칸부터 정규화 스크롤 위치를 그 칸으로 옮기며 넘기는 연출.
                if (idx > 0 && scrollRect != null)
                {
                    float target = _NormalizedFor(idx, count);
                    _sequence.Append(DOTween.To(() => scrollRect.horizontalNormalizedPosition,
                        value => scrollRect.horizontalNormalizedPosition = value, target, slideDuration)
                        .SetEase(Ease.OutCubic));
                }

                int targetScore = records[idx].ScoreEarned;
                int display = 0;
                var slot = _slots[idx];
                _sequence.Append(DOTween.To(() => display, value =>
                {
                    display = value;
                    slot.SetScore(display);
                }, targetScore, scoreCountDuration).SetEase(Ease.OutCubic));

                _sequence.AppendInterval(holdInterval);
            }

            // 마지막에 하단 총점 패널 카운트업.
            int total = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : _SumEarned(records);
            int totalDisplay = 0;
            _sequence.Append(DOTween.To(() => totalDisplay, value =>
            {
                totalDisplay = value;
                _SetTotalScore(totalDisplay);
            }, total, scoreCountDuration).SetEase(Ease.OutCubic));

            _sequence.OnComplete(_OnSequenceComplete);
        }

        private int _SumEarned(IReadOnlyList<StageRunRecord> records)
        {
            int sum = 0;
            if (records != null)
                for (int i = 0; i < records.Count; i++)
                    sum += records[i].ScoreEarned;
            return sum;
        }

        private void _SetTotalScore(int value)
        {
            if (totalScoreText == null)
                return;

            totalScoreText.text = $"{_totalLabel} {value.ToCommaString()}{_totalSuffix}";
        }

        // 슬롯 index를 화면 중앙에 두는 정규화 스크롤 위치(0~1).
        // Content 좌우 패딩을 (뷰포트폭-슬롯폭)/2로 맞춰뒀기 때문에 index/(total-1)이 그 칸을 정확히 중앙에 둔다.
        private float _NormalizedFor(int index, int total)
        {
            if (total <= 1) return 0f;
            return (float)index / (total - 1);
        }

        private void _OnSequenceComplete()
        {
            _isPlaying = false;

            // 자동 재생이 끝나면 가로로 좌우 자유 스크롤 허용 + 로비 버튼 노출.
            if (scrollRect != null)
                scrollRect.horizontal = true;
            if (lobbyButton != null)
                lobbyButton.gameObject.SetActive(true);
        }

        // 화면 탭 → 자동 재생을 끝까지 즉시 진행(콜백 포함)해 스크롤 모드로 전환.
        private void _OnSkip()
        {
            if (!_isPlaying)
                return;

            _sequence?.Complete(true);
        }
    }
}
