using System.Collections.Generic;
using Cumic;
using Cumic.Events;
using DG.Tweening;
using TMPro;
using TrainDefense.Game.Manager;
using TrainDefense.Localize;
using UnityEngine;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 게임오버 결산 화면.
    /// 승패 제목 → 총점(카운트업 1회) → 통계 줄(도달 역·레벨·처치·획득 재화) → 로비 버튼의 세로 한 판.
    /// 맵별 점수 슬라이드(StageResultSlot)는 10-19에 폐기했다.
    /// </summary>
    public class StageResultSlideUI : MonoBehaviour
    {
        #region Fields
        [Header("Refs")]
        [Tooltip("전체화면 딤.")]
        [SerializeField]
        private GameObject background;
        [Tooltip("페이드 인용(선택). 없으면 그냥 즉시 표시.")]
        [SerializeField]
        private CanvasGroup canvasGroup;
        [Tooltip("결산 판(제목·총점·통계·로비 버튼을 담는 패널).")]
        [SerializeField]
        private GameObject panel;
        [SerializeField]
        private TMP_Text titleText;
        [SerializeField]
        private TMP_Text totalScoreText;

        [Header("통계 줄 — 라벨 문구는 코드가 채운다")]
        [SerializeField]
        private TMP_Text stationLabelText;
        [SerializeField]
        private TMP_Text stationValueText;
        [SerializeField]
        private TMP_Text levelLabelText;
        [SerializeField]
        private TMP_Text levelValueText;
        [SerializeField]
        private TMP_Text normalKillLabelText;
        [SerializeField]
        private TMP_Text normalKillValueText;
        [SerializeField]
        private TMP_Text eliteKillLabelText;
        [SerializeField]
        private TMP_Text eliteKillValueText;
        [SerializeField]
        private TMP_Text earnedLabelText;
        [Tooltip("이번 판 획득 엘리트 재화. 무엇을 얻었는지는 옆의 아이콘이 알려준다.")]
        [SerializeField]
        private TMP_Text earnedValueText;
        [Header("해금 줄 — 이번 판에 새로 해금된 개조가 있을 때만 켠다")]
        [SerializeField]
        private GameObject unlockedRow;
        [SerializeField]
        private TMP_Text unlockedLabelText;
        [SerializeField]
        private TMP_Text unlockedValueText;

        [Header("로비 버튼")]
        [Tooltip("총점 카운트업이 끝난 뒤 활성화되는 로비 버튼. (GameObject stripped 회피를 위해 Transform으로 연결)")]
        [SerializeField]
        private RectTransform lobbyButton;

        [Header("Timing")]
        [SerializeField]
        private float fadeInDuration = 0.3f;
        [SerializeField]
        private float scoreCountDuration = 0.6f;
        #endregion

        // 해금 이름이 "해금" 라벨 글자에 붙지 않도록 띄우는 최소 간격.
        private const float UNLOCKED_LABEL_GAP = 24f;

        private Tween _countUpTween;

        // 카운트업 setter가 매 프레임 호출되므로 접미사는 ShowResult에서 1회만 해석해 캐시한다.
        private string _scoreSuffix = "점";

        // 해금 이름을 줄일 때 되돌아갈 프리팹 글자 크기.
        private float _unlockedValueFontSize;

        #region LifeCycle
        private void Start()
        {
            if (unlockedValueText != null)
                _unlockedValueFontSize = unlockedValueText.fontSize;

            _SubscribeEvents();
            _Hide();
        }

        private void OnDestroy()
        {
            _UnsubscribeEvents();
            _countUpTween?.Kill();
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
        private void _OnGameEnd(GameEndEvent gameEndEvent) => ShowResult(gameEndEvent.IsClear);

        private void _Hide()
        {
            _countUpTween?.Kill();

            // 게임 중에는 결산 표시 내용을 모두 끈다. 스크립트가 붙은 루트는
            // 켜둬야 GameEndEvent를 받아 다시 켤 수 있으므로 자식만 토글한다.
            if (background != null)
                background.SetActive(false);
            if (panel != null)
                panel.SetActive(false);
            if (lobbyButton != null)
                lobbyButton.gameObject.SetActive(false);
        }

        public void ShowResult(bool isClear)
        {
            if (background != null)
                background.SetActive(true);
            if (panel != null)
                panel.SetActive(true);
            // 로비 버튼은 총점 카운트업이 끝난 뒤에만 켜지도록 시작 시 숨긴다.
            if (lobbyButton != null)
                lobbyButton.gameObject.SetActive(false);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, fadeInDuration).SetUpdate(true);
            }

            _scoreSuffix = LocalizeHelper.GetByKey("result_score_suffix", "점");

            _SetText(titleText, LocalizeHelper.GetByKey(isClear ? "result_title_clear" : "result_title_defeat", isClear ? "클리어" : "패배"));
            _FillStats();
            _PlayTotalCountUp();
        }

        private void _FillStats()
        {
            int stationCount = StageManager.Instance != null ? StageManager.Instance.TotalInspectionPassedCount : 0;
            int level = UserDataManager.Instance != null ? UserDataManager.Instance.CurrentLevel : 0;
            int normalKill = ScoreManager.Instance != null ? ScoreManager.Instance.NormalKillCount : 0;
            int eliteKill = ScoreManager.Instance != null ? ScoreManager.Instance.EliteKillCount : 0;
            int earned = PermanentUpgradeManager.Instance != null ? PermanentUpgradeManager.Instance.RunEliteCoinEarned : 0;

            _SetText(stationLabelText, LocalizeHelper.GetByKey("result_stat_station", "도달 역"));
            _SetText(stationValueText, stationCount.ToCommaString());
            _SetText(levelLabelText, LocalizeHelper.GetByKey("result_stat_level", "레벨"));
            _SetText(levelValueText, level.ToCommaString());
            _SetText(normalKillLabelText, LocalizeHelper.GetByKey("result_kill_normal", "일반 몬스터"));
            _SetText(normalKillValueText, normalKill.ToCommaString());
            _SetText(eliteKillLabelText, LocalizeHelper.GetByKey("result_kill_elite", "엘리트 몬스터"));
            _SetText(eliteKillValueText, eliteKill.ToCommaString());
            _SetText(earnedLabelText, LocalizeHelper.GetByKey("result_stat_earned", "획득"));
            _SetText(earnedValueText, $"+{earned.ToCommaString()}");

            _FillUnlocked();
        }

        // 이번 판에 새로 해금된 개조 이름을 한 줄로. 없으면 줄 자체를 숨긴다(UserDataManager가 GameEndEvent에서 먼저 판정해 둔다).
        private void _FillUnlocked()
        {
            var unlockedNames = new List<string>();
            var userDataManager = UserDataManager.Instance;
            var skillDataDB = DatabaseManager.Instance != null ? DatabaseManager.Instance.GetTrainSkillDataDB() : null;

            if (userDataManager != null && skillDataDB != null)
            {
                foreach (string remodelId in userDataManager.LastRunUnlockedRemodelIds)
                {
                    foreach (var remodel in skillDataDB.TrainPassiveSkillDataList)
                    {
                        if (remodel == null || remodel.Id != remodelId) continue;

                        unlockedNames.Add(remodel.Name);
                        break;
                    }
                }
            }

            if (unlockedRow != null)
                unlockedRow.SetActive(unlockedNames.Count > 0);

            _SetText(unlockedLabelText, LocalizeHelper.GetByKey("result_unlocked", "해금"));
            _SetText(unlockedValueText, _BuildUnlockedText(unlockedNames));
            _FitUnlockedValueFontSize();
        }

        // 이름 하나만 남겨도 안 들어가는 언어(라벨 글자가 긴 경우)는 들어갈 만큼 글자를 줄인다.
        private void _FitUnlockedValueFontSize()
        {
            if (unlockedValueText == null || unlockedLabelText == null)
                return;

            unlockedValueText.fontSize = _unlockedValueFontSize;

            float preferredWidth = unlockedValueText.GetPreferredValues(unlockedValueText.text).x;
            float availableWidth = _GetUnlockedValueWidth();

            if (preferredWidth > availableWidth && availableWidth > 0f)
                unlockedValueText.fontSize = _unlockedValueFontSize * availableWidth / preferredWidth;
        }

        // 이름이 줄에 다 안 들어가면 앞에서부터 들어가는 만큼만 적고 나머지는 "외 N개"로 붙인다. 이름은 최소 한 개 적는다.
        private string _BuildUnlockedText(List<string> unlockedNames)
        {
            string allNamesText = string.Join(", ", unlockedNames);

            if (unlockedValueText == null || unlockedLabelText == null || unlockedNames.Count <= 1)
                return allNamesText;

            float availableWidth = _GetUnlockedValueWidth();

            if (unlockedValueText.GetPreferredValues(allNamesText).x <= availableWidth)
                return allNamesText;

            string moreFormat = LocalizeHelper.GetByKey("result_unlocked_more", "외 {0}개");

            for (int shownCount = unlockedNames.Count - 1; shownCount > 1; shownCount--)
            {
                string shownText = _FormatUnlockedText(unlockedNames, shownCount, moreFormat);

                if (unlockedValueText.GetPreferredValues(shownText).x <= availableWidth)
                    return shownText;
            }

            return _FormatUnlockedText(unlockedNames, 1, moreFormat);
        }

        private static string _FormatUnlockedText(List<string> unlockedNames, int shownCount, string moreFormat)
        {
            string shownNames = string.Join(", ", unlockedNames.GetRange(0, shownCount));

            return $"{shownNames} {string.Format(moreFormat, unlockedNames.Count - shownCount)}";
        }

        // 라벨과 값은 같은 줄(부모)에 있다. 값 오른쪽 끝부터 라벨 글자 끝까지가 이름을 적을 수 있는 폭이다.
        private float _GetUnlockedValueWidth()
        {
            var labelRect = unlockedLabelText.rectTransform;
            var valueRect = unlockedValueText.rectTransform;
            float labelTextRight = labelRect.localPosition.x + labelRect.rect.xMin + unlockedLabelText.GetPreferredValues(unlockedLabelText.text).x;
            float valueRight = valueRect.localPosition.x + valueRect.rect.xMax;

            return valueRight - labelTextRight - UNLOCKED_LABEL_GAP;
        }

        // 총점을 0부터 한 번 카운트업하고, 끝나면 로비 버튼을 켠다.
        private void _PlayTotalCountUp()
        {
            _countUpTween?.Kill();

            int total = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
            int display = 0;
            _SetTotalScore(0);

            _countUpTween = DOTween.To(() => display, value =>
                {
                    display = value;
                    _SetTotalScore(display);
                }, total, scoreCountDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .OnComplete(_OnCountUpComplete);
        }

        private void _OnCountUpComplete()
        {
            if (lobbyButton != null)
                lobbyButton.gameObject.SetActive(true);
        }

        private void _SetTotalScore(int value)
        {
            _SetText(totalScoreText, $"{value.ToCommaString()} {_scoreSuffix}");
        }

        private static void _SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }
    }
}
