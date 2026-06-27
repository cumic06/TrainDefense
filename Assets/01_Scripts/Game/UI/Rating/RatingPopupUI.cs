using System.Collections;
using TMPro;
using TrainDefense.Localize;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    // 평점 팝업. 별을 탭하면 1~N개가 채워지고, 기준 점수(storeOpenMinRating) 이상이면
    // 스토어 리뷰 페이지로 이동한다. 미만이면 감사 메시지를 보여준 뒤 닫는다.
    public class RatingPopupUI : MonoBehaviour
    {
        #region Variables
        private const string TitleKey = "UI_RatingTitle";
        private const string TitleFallback = "이 게임을 평가해주세요!";
        private const string ThanksKey = "UI_RatingThanks";

        private static readonly Color EmptyStarColor = new(0.4f, 0.4f, 0.45f, 1f);
        private static readonly Color FilledStarColor = new(1f, 0.78f, 0.2f, 1f);

        private int _selectedRating;
        private bool _isSubmitted;
        #endregion

        #region Fields
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button[] starButtons;
        [SerializeField] private Image[] starImages;
        [SerializeField] private Button closeButton;
        [SerializeField] private int storeOpenMinRating = 4;
        [SerializeField] private float submitDelay = 0.8f;
        [SerializeField] private float thanksDuration = 1f;
        #endregion

        #region LifeCycle
        private void OnEnable()
        {
            _SubscribeEvents();
        }

        private void OnDisable()
        {
            _UnsubscribeEvents();
        }

        private void Start()
        {
            _SetTitle(TitleKey, TitleFallback);
            _RefreshStars();
        }
        #endregion

        #region Sub/UnSub
        private void _SubscribeEvents()
        {
            for (int i = 0; i < starButtons.Length; i++)
            {
                int rating = i + 1;
                starButtons[i].onClick.AddListener(() => _SelectRating(rating));
            }

            closeButton.onClick.AddListener(_Close);

            Localization.OnInitialized += _RefreshLocalizedTexts;
            Localization.OnLanguageChanged += _RefreshLocalizedTexts;
        }

        private void _UnsubscribeEvents()
        {
            foreach (Button starButton in starButtons)
            {
                starButton.onClick.RemoveAllListeners();
            }

            closeButton.onClick.RemoveAllListeners();

            Localization.OnInitialized -= _RefreshLocalizedTexts;
            Localization.OnLanguageChanged -= _RefreshLocalizedTexts;
        }
        #endregion

        private void _SelectRating(int rating)
        {
            if (_isSubmitted)
                return;

            _isSubmitted = true;
            _selectedRating = rating;
            _RefreshStars();

            StartCoroutine(_SubmitRoutine());
        }

        private IEnumerator _SubmitRoutine()
        {
            yield return new WaitForSecondsRealtime(submitDelay);

            RatingRecord.IsRated = true;

            if (_selectedRating >= storeOpenMinRating)
            {
                _OpenStorePage();
            }
            else
            {
                _SetTitle(ThanksKey, "소중한 의견 감사합니다!");

                yield return new WaitForSecondsRealtime(thanksDuration);
            }

            _Close();
        }

        private void _RefreshStars()
        {
            for (int i = 0; i < starImages.Length; i++)
            {
                starImages[i].color = i < _selectedRating ? FilledStarColor : EmptyStarColor;
            }
        }

        // 초기화 완료·언어 변경 시 타이틀을 현재 언어로 다시 적용한다(제출 후엔 코루틴이 곧 닫으므로 미적용).
        private void _RefreshLocalizedTexts()
        {
            if (!_isSubmitted)
                _SetTitle(TitleKey, TitleFallback);
        }

        private void _SetTitle(string localizeKey, string fallback)
        {
            titleText.text = LocalizeHelper.GetByKey(localizeKey, fallback);
        }

        private void _OpenStorePage()
        {
            Application.OpenURL("https://play.google.com/store/apps/details?id=" + Application.identifier);
        }

        private void _Close()
        {
            Destroy(gameObject);
        }
    }
}
