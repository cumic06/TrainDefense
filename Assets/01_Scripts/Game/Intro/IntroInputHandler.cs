using System;
using UnityEngine;

namespace TrainDefense.Game.Intro
{
    /// <summary>
    /// PC 전용: ESC 홀드로 전체 스킵 감지.
    /// 모바일 전체 스킵 버튼은 IIntroView.OnSkipRequested 경로로 처리.
    /// </summary>
    public class IntroInputHandler : MonoBehaviour
    {
        public event Action OnFullSkipRequested;

        // 0~1 범위의 홀드 진행도 (프로그레스 바 UI 연결용)
        public float HoldProgress => _holdDuration > 0f ? Mathf.Clamp01(_holdTimer / _holdDuration) : 0f;

        private float _holdTimer;
        private float _holdDuration;
        private bool _isEnabled;

        public void Enable(float holdDuration)
        {
            _holdDuration = holdDuration;
            _holdTimer = 0f;
            _isEnabled = true;
        }

        public void Disable()
        {
            _isEnabled = false;
            _holdTimer = 0f;
        }

        private void Update()
        {
            if (!_isEnabled) return;

            if (Input.GetKey(KeyCode.Escape))
            {
                _holdTimer += Time.unscaledDeltaTime;
                if (_holdTimer >= _holdDuration)
                {
                    Disable();
                    OnFullSkipRequested?.Invoke();
                }
            }
            else
            {
                _holdTimer = 0f;
            }
        }
    }
}
