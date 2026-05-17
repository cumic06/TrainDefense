using System;
using UnityEngine;

namespace TrainDefense.Game.Intro
{
    [Serializable]
    public class IntroSlideData
    {
        [Header("Visual")]
        [SerializeField] private Sprite _backgroundImage;
        [SerializeField] private Sprite _characterPortrait;

        [Header("Text")]
        [SerializeField] private string _speakerName;
        [SerializeField] [TextArea(2, 5)] private string _text;
        [Tooltip("Localization key. 설정 시 _text 대신 사용됩니다 (Localization 시스템 구현 후 활성화).")]
        [SerializeField] private string _localizationKey;

        [Header("Audio")]
        [SerializeField] private AudioClip _bgmClip;
        [SerializeField] private AudioClip _voiceClip;

        [Header("Advance")]
        [Tooltip("0 = 수동 진행. 0 초과 = N초 후 자동 다음 슬라이드.")]
        [SerializeField] private float _autoAdvanceDuration;

        public Sprite BackgroundImage => _backgroundImage;
        public Sprite CharacterPortrait => _characterPortrait;
        public string SpeakerName => _speakerName;
        public AudioClip BgmClip => _bgmClip;
        public AudioClip VoiceClip => _voiceClip;
        public float AutoAdvanceDuration => _autoAdvanceDuration;
        public bool IsAutoAdvance => _autoAdvanceDuration > 0f;

        /// <summary>
        /// 표시할 텍스트를 반환합니다.
        /// Localization 시스템 구현 후 _localizationKey 우선 적용 예정.
        /// </summary>
        public string GetText()
        {
            // TODO: Localization 구현 시 아래 주석 해제
            // if (!string.IsNullOrEmpty(_localizationKey))
            //     return Localization.Get(_localizationKey);
            return _text;
        }
    }
}
