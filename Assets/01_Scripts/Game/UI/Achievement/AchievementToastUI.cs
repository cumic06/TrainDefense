using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Cumic.Achievement;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Localize;

namespace TrainDefense.Game.UI.Achievement
{
    /// <summary>
    /// 업적 달성 시 화면에 잠깐 뜨는 토스트. AchievementUnlockedEvent를 구독해
    /// "업적 달성! {제목}" 문구와 사운드를 재생한다. 한 번에 여러 개가 달성되면 큐로 순차 표시한다.
    /// 인게임 Canvas 아래에 배치하며, 평소엔 CanvasGroup.alpha=0으로 숨어 있다.
    /// </summary>
    public class AchievementToastUI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private float holdDuration = 2f;
        #endregion

        private readonly Queue<string> _queue = new();
        private Coroutine _playRoutine;

        private void Awake()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            // 언어별 폰트(라틴/아랍/태국 등) 교체 + RTL 방향 처리. 한국어는 기본 폰트(DNFBitBitv2) 유지.
            if (messageText != null)
                LocalizeFontSwitcher.Register(messageText);
        }

        private void OnDestroy()
        {
            if (messageText != null)
                LocalizeFontSwitcher.Unregister(messageText);
        }

        private void OnEnable()
        {
            GameEventSystem.Subscribe<AchievementUnlockedEvent>(_OnUnlocked);
        }

        private void OnDisable()
        {
            GameEventSystem.Unsubscribe<AchievementUnlockedEvent>(_OnUnlocked);
        }

        private void _OnUnlocked(AchievementUnlockedEvent e)
        {
            if (e == null || e.Data == null)
                return;

            string title = LocalizeHelper.GetByKey($"Achievement_{e.Data.Id}_Title", e.Data.Title);
            string message = string.Format(LocalizeHelper.GetByKey("Achievement_Toast", "업적 달성! {0}"), title);
            _queue.Enqueue(message);

            // UI 사운드이므로 일시 억제(삼중택일·일시정지 등)를 무시하고 재생한다.
            SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_AchievementUnlock, ignoreSuppress: true);

            if (_playRoutine == null)
                _playRoutine = StartCoroutine(_PlayQueue());
        }

        private IEnumerator _PlayQueue()
        {
            while (_queue.Count > 0)
            {
                string message = _queue.Dequeue();

                if (messageText != null)
                    messageText.text = message;

                yield return _Fade(0f, 1f, fadeDuration);
                yield return new WaitForSecondsRealtime(holdDuration);
                yield return _Fade(1f, 0f, fadeDuration);
            }

            _playRoutine = null;
        }

        private IEnumerator _Fade(float from, float to, float duration)
        {
            if (canvasGroup == null || duration <= 0f)
            {
                if (canvasGroup != null)
                    canvasGroup.alpha = to;

                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }

            canvasGroup.alpha = to;
        }
    }
}
