using System;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.Intro
{
    public class IntroOverlayView : MonoBehaviour, IIntroView
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;

        [Header("Background")]
        [SerializeField] private Image _backgroundImage;

        [Header("Message Bubble")]
        [SerializeField] private MessageBubble _messageBubble;
        [SerializeField] private Vector2 _bubbleAnchorPosition = new Vector2(0f, -380f);

        [Header("Buttons")]
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _skipButton;

        [Header("Screen Tap")]
        [Tooltip("화면 전체를 덮는 버튼. 버튼이 아닌 빈 화면을 터치해도 다음 슬라이드로 넘어가게 한다.")]
        [SerializeField] private Button _screenButton;

        public event Action OnNextRequested;
        public event Action OnSkipRequested;

        private void Awake()
        {
            _nextButton?.onClick.AddListener(() =>
            {
                SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_ButtonClick, ignoreSuppress: true);
                OnNextRequested?.Invoke();
            });
            _skipButton?.onClick.AddListener(() =>
            {
                SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_ButtonClick, ignoreSuppress: true);
                OnSkipRequested?.Invoke();
            });
            _screenButton?.onClick.AddListener(() =>
            {
                SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_ButtonClick, ignoreSuppress: true);
                OnNextRequested?.Invoke();
            });
        }

        private void OnDestroy()
        {
            _nextButton?.onClick.RemoveAllListeners();
            _skipButton?.onClick.RemoveAllListeners();
            _screenButton?.onClick.RemoveAllListeners();
        }

        public void Show()
        {
            if (_root != null)
                _root.SetActive(true);
        }

        public void Hide()
        {
            _messageBubble?.Hide();
            if (_root != null)
                _root.SetActive(false);
        }

        public void ShowSlide(IntroSlideData slide, int index, int total)
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.sprite = slide.BackgroundImage;
                _backgroundImage.enabled = slide.BackgroundImage != null;
            }

            _messageBubble?.Show(slide.GetText(), _bubbleAnchorPosition, slide.CharacterPortrait, slide.GetSpeakerName());

            bool isLastSlide = index >= total - 1;
            if (_skipButton != null)
                _skipButton.gameObject.SetActive(!isLastSlide);
        }
    }
}
