using System;
using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Febucci.TextAnimatorForUnity;
using Febucci.TextAnimatorForUnity.TextMeshPro;

namespace TrainDefense.Game.UI
{
   /// <summary>
   /// 레벨업 알림 텍스트의 등장/퇴장 연출 전용 컴포넌트.
   /// TextAnimator(글자별 등장 효과 + 무지개)와 타이프라이터로 인디게임풍 팝 연출을 1회 재생한다.
   /// 레벨업 UI는 일시정지(timeScale=0) 중에 표시되므로 unscaled 시간으로 직접 구동한다.
   /// </summary>
   public class LevelUpText : MonoBehaviour
   {
      #region Fields
      [SerializeField]
      private TextMeshProUGUI text;

      [Header("Typewriter")]
      [Tooltip("글자당 타이핑 타이밍 에셋. 에디터에서 만들어 할당한다(미할당 시 글자별 타이핑 없이 즉시 표시)")]
      [SerializeField]
      private TypingDelaysByCharacter typingTimings;

      [Header("Effect")]
      [Tooltip("글자에 입힐 TextAnimator 태그(등장 효과 + 지속 효과). 기본: 통통 튀는 등장 + 무지개")]
      [SerializeField]
      private string effectTags = "<bounce><rainb>";

      [Header("Pop Scale")]
      [Tooltip("등장 시작 스케일(0~1)")]
      [SerializeField]
      private float popInScale = 0.6f;
      [Tooltip("등장 팝 스케일 시간(초)")]
      [SerializeField]
      private float popInDuration = 0.45f;
      [Tooltip("타이핑 완료 후 글자를 보여주는 유지 시간(초)")]
      [SerializeField]
      private float holdDuration = 0.5f;
      [Tooltip("퇴장 팝 스케일 시간(초)")]
      [SerializeField]
      private float popOutDuration = 0.22f;
      #endregion

      #region Variables
      private TextAnimator_TMP _textAnimator;
      private TypewriterComponent _typewriter;
      private string _baseText;
      private bool _initialized;
      #endregion

      // 이 오브젝트는 씬에서 "비활성"으로 배치한다.
      // Awake에서 SetActive(false)를 두면, PlayAsync의 첫 SetActive(true)가 (생애 최초)
      // Awake를 실행시켜 곧바로 자신을 꺼버리므로 의도적으로 Awake를 두지 않는다.
      // (TextAnimator/Typewriter도 첫 PlayAsync에서 비활성 상태일 때 추가해 OnEnable NullRef를 피한다)
      private void _Initialize()
      {
         if (_initialized)
            return;

         if (text == null)
            text = GetComponent<TextMeshProUGUI>();

         _baseText = text != null ? text.text : string.Empty;

         // 에디터에서 부착돼 있으면 그대로 쓰고, 없으면 런타임에 추가한다.
         // PlayAsync 진입 시 오브젝트가 비활성 상태라 AddComponent 시 OnEnable이 발생하지 않아 안전하다.
         if (!TryGetComponent(out _textAnimator))
            _textAnimator = gameObject.AddComponent<TextAnimator_TMP>();
         // 일시정지(timeScale=0) 중에도 재생되도록 스크립트 루프(Update의 Animate)로 직접 구동한다.
         _textAnimator.animationLoop = AnimationLoop.Script;

         if (!TryGetComponent(out _typewriter))
            _typewriter = gameObject.AddComponent<TypewriterComponent>();
         if (_typewriter.localSettings == null)
            _typewriter.localSettings = new UnityTypewriterSettings();
         _typewriter.localSettings.useTypeWriter = true;

         if (typingTimings != null)
            _typewriter.TimingSettings = typingTimings;
         else if (_typewriter.TimingSettings == null)
            Debug.LogWarning($"{nameof(LevelUpText)}: 타이핑 타이밍 에셋이 없어 글자별 타이핑이 적용되지 않습니다. 인스펙터의 Typing Timings에 에셋을 할당하세요.", this);

         _initialized = true;
      }

      private void Update()
      {
         // 일시정지 중에도 타이핑/효과가 진행되도록 unscaled로 직접 구동한다.
         if (_textAnimator != null)
            _textAnimator.Animate(Time.unscaledDeltaTime);
      }

      /// <summary>
      /// 등장 → 유지 → 퇴장 순으로 연출을 1회 재생한다.
      /// </summary>
      /// <param name="isCancelled">중간에 취소되었는지 검사하는 함수(true면 즉시 정리하고 종료)</param>
      public async UniTask PlayAsync(Func<bool> isCancelled = null)
      {
         _Initialize();

         var tr = transform;
         tr.DOKill();
         gameObject.SetActive(true);

         // 작게 시작해 OutBack으로 통통 튀어오르듯 등장한다.
         tr.localScale = Vector3.one * popInScale;
         tr.DOScale(1f, popInDuration).SetEase(Ease.OutBack).SetUpdate(true);

         // 등장 효과 + 지속 효과 태그를 입혀 글자별로 타이핑한다.
         string decorated = $"{effectTags}{_baseText}";

         var typingDone = new UniTaskCompletionSource();
         void OnShowed() => typingDone.TrySetResult();
         _typewriter.onTextShowed.AddListener(OnShowed);

         _typewriter.ShowText(decorated);
         _typewriter.StartShowingText(true);

         // 타이핑 완료 대기. 이벤트 누락에 대비해 타임아웃 폴백을 둔다.
         float perChar = typingTimings != null ? typingTimings.waitForNormalChars : 0.06f;
         int timeoutMs = (int)(Mathf.Max(_baseText.Length, 1) * perChar * 1000) + 3000;
         await UniTask.WhenAny(typingDone.Task, UniTask.Delay(timeoutMs, ignoreTimeScale: true));
         _typewriter.onTextShowed.RemoveListener(OnShowed);

         if (_IsCancelled(isCancelled))
         {
            _Cleanup();

            return;
         }

         // 다 보인 뒤 잠깐 유지해 무지개 흔들림을 감상시킨다.
         await UniTask.Delay((int)(holdDuration * 1000), ignoreTimeScale: true);

         if (_IsCancelled(isCancelled))
         {
            _Cleanup();

            return;
         }

         // 살짝 들어갔다 사라지는 팝 아웃.
         await tr.DOScale(0f, popOutDuration).SetEase(Ease.InBack).SetUpdate(true);

         _Cleanup();
      }

      private static bool _IsCancelled(Func<bool> isCancelled) => isCancelled != null && isCancelled();

      private void _Cleanup()
      {
         transform.DOKill();
         transform.localScale = Vector3.one;
         gameObject.SetActive(false);
      }
   }
}
