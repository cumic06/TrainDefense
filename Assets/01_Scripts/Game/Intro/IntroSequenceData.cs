using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.Intro
{
    /// <summary>
    /// 게임 최초 실행 시 재생되는 인트로 컷씬 시퀀스 데이터.
    /// 완료 여부는 UserDataManager.TutorialSaveData.IsSequenceCompleted(SequenceId)로 확인합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "IntroSequence", menuName = "TrainDefense/Intro/Intro Sequence")]
    public class IntroSequenceData : ScriptableObject
    {
        [Tooltip("이 시퀀스의 고유 ID. TutorialSaveData에서 완료 여부를 추적하는 데 사용됩니다.")]
        [SerializeField] private string _sequenceId;
        [SerializeField] private List<IntroSlideData> _slides = new();
        [SerializeField] private IntroSkipConfig _skipConfig = new();

        public string SequenceId => _sequenceId;
        public IReadOnlyList<IntroSlideData> Slides => _slides;
        public IntroSkipConfig SkipConfig => _skipConfig;
    }

    [Serializable]
    public class IntroSkipConfig
    {
        [Tooltip("전체 스킵 기능 활성화 여부")]
        [SerializeField] private bool _canSkip = true;
        [Tooltip("PC: ESC 키를 이 시간(초)만큼 누르면 전체 스킵됩니다.")]
        [SerializeField] private float _pcHoldDuration = 2f;

        public bool CanSkip => _canSkip;
        public float PcHoldDuration => _pcHoldDuration;
    }
}
