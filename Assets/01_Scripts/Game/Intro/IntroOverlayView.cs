using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.Intro
{
    public class IntroOverlayView : MonoBehaviour, IIntroView
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;

        [Header("Visuals")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _characterPortrait;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI _speakerNameText;
        [SerializeField] private TextMeshProUGUI _dialogueText;

        [Header("Buttons")]
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _skipButton;

        public event Action OnNextRequested;
        public event Action OnSkipRequested;

        private void Awake()
        {
            _nextButton?.onClick.AddListener(() => OnNextRequested?.Invoke());
            _skipButton?.onClick.AddListener(() => OnSkipRequested?.Invoke());
        }

        private void OnDestroy()
        {
            _nextButton?.onClick.RemoveAllListeners();
            _skipButton?.onClick.RemoveAllListeners();
        }

        public void Show()
        {
            if (_root != null)
                _root.SetActive(true);
        }

        public void Hide()
        {
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

            bool hasPortrait = slide.CharacterPortrait != null;
            if (_characterPortrait != null)
            {
                _characterPortrait.sprite = slide.CharacterPortrait;
                _characterPortrait.enabled = hasPortrait;
            }

            if (_speakerNameText != null)
            {
                _speakerNameText.text = slide.SpeakerName;
                _speakerNameText.enabled = !string.IsNullOrEmpty(slide.SpeakerName);
            }

            if (_dialogueText != null)
                _dialogueText.text = slide.GetText();

            bool isLastSlide = index >= total - 1;
            if (_skipButton != null)
                _skipButton.gameObject.SetActive(!isLastSlide);
        }
    }
}
