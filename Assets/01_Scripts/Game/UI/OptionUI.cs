using System.Collections.Generic;
using TMPro;
using Cumic.Sequence;
using TrainDefense.Game;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Analytics;
using TrainDefense.Game.Intro;
using TrainDefense.Game.Manager;
using TrainDefense.Game.UI;
using TrainDefense.Localize;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense
{
   // 옵션 창의 프레임(열기/닫기·데이터 초기화)을 담당하는 메인 컨트롤러.
   // 각 영역 내부 컨트롤은 SoundOptionUI / AccessibilityOptionUI / LanguageOptionUI / PrivacyOptionUI 가 분담한다.
   public class OptionUI : MonoBehaviour
   {
      #region Fields
      [Header("데이터 초기화 / 튜토리얼")]
      [SerializeField]
      private Button deletePlayerPrefsButton;
      [SerializeField]
      private Button tutorialReplayButton;
      [SerializeField]
      private TMP_Text tutorialReplayText;

      #endregion

      private void Awake()
      {
         _SubscribeListeners();
      }

      private void OnEnable()
      {
         _RefreshTutorialReplayText();

         PopupTween.PlayShow(gameObject);
      }

      private void OnDestroy()
      {
         _UnSubscribeListeners();
      }

      private void _SubscribeListeners()
      {
         // deletePlayerPrefsButton(로비 초기화 버튼)은 씬 onClick에 OnDeletePlayerPrefsClicked가 이미 연결돼 있다.
         // 여기서 또 AddListener하면 옵션창이 열려 있는 동안 클릭 1회에 팝업이 두 장 뜬다.

         if (tutorialReplayButton != null)
            tutorialReplayButton.onClick.AddListener(_OnTutorialReplayClick);

         Localization.OnLanguageChanged += _RefreshTutorialReplayText;
         Localization.OnInitialized += _RefreshTutorialReplayText;
      }

      private void _UnSubscribeListeners()
      {
         // deletePlayerPrefsButton은 등록하지 않으므로 해제도 하지 않는다.
         // (RemoveAllListeners를 부르면 씬에 연결된 것 외의 런타임 리스너까지 지운다)

         if (tutorialReplayButton != null)
            tutorialReplayButton.onClick.RemoveListener(_OnTutorialReplayClick);

         Localization.OnLanguageChanged -= _RefreshTutorialReplayText;
         Localization.OnInitialized -= _RefreshTutorialReplayText;
      }

      public void OnDeletePlayerPrefsClicked()
      {
         // 무엇을 초기화할지 고르는 선택형 팝업을 띄운다. (실제 초기화는 팝업 내부에서 UserDataManager를 통해 수행)
         // 옵션창이 올라가 있는 캔버스에 붙인다. 넘기지 않으면 DontDestroyOnLoad 캔버스가 잡혀 씬을 넘어가도 팝업이 남는다.
         Canvas parentCanvas = GetComponentInParent<Canvas>();
         ResetSelectPopupUI.Show(parentCanvas != null ? parentCanvas.transform : null);
      }

      public void ShowOptionUI()
      {
         SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_WindowOpen, ignoreSuppress: true);

         gameObject.SetActive(true);

         if (InGameSequence.Instance != null)
            InGameSequence.Instance.PushOverlay(OverlayPhase.Option);
         else if (TimeManager.Instance != null)
            TimeManager.Instance.Pause();
      }

      public void HideOptionUI()
      {
         SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_WindowClose, ignoreSuppress: true);

         PopupTween.PlayHide(gameObject, () => gameObject.SetActive(false));

         if (InGameSequence.Instance != null)
            InGameSequence.Instance.PopOverlay(OverlayPhase.Option);
         else if (TimeManager.Instance != null)
            TimeManager.Instance.Resume();
      }

      // 튜토리얼·인트로 진행 기록을 초기화한다. 인트로가 현재 씬(로비 등)에 있으면 즉시 다시 재생하고,
      // 없으면 다음 게임 진입 시 튜토리얼·인트로가 처음부터 다시 표시된다.
      private void _OnTutorialReplayClick()
      {
         SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_ButtonClick, ignoreSuppress: true);

         if (UserDataManager.Instance != null)
            UserDataManager.Instance.ResetTutorialData();

         if (IntroManager.Instance != null)
            IntroManager.Instance.Replay();
      }

      // 튜토리얼 다시보기 라벨을 현재 언어로 갱신한다.
      private void _RefreshTutorialReplayText()
      {
         if (tutorialReplayText != null)
            tutorialReplayText.text = LocalizeHelper.GetByKey("UI_Option_Tutorial_Replay", "튜토리얼 다시 보기");
      }
   }
}
