using System;
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
using UnityEngine.Events;
using UnityEngine.UI;

namespace TrainDefense
{
   // 옵션 창의 프레임(탭 전환·열기/닫기·데이터 초기화)을 담당하는 메인 컨트롤러.
   // 각 탭 내부 컨트롤은 SoundOptionUI / LanguageOptionUI / PrivacyOptionUI / AccessibilityOptionUI 가 분담한다.
   public class OptionUI : MonoBehaviour
   {
      public enum OptionTab
      {
         Sound,
         Language,
         Privacy,
         Etc
      }

      // 탭 버튼 하나와 그 탭이 보여줄 패널의 짝.
      [Serializable]
      private class OptionTabEntry
      {
         public OptionTab Tab;
         public Button Button;
         public TMP_Text Label;
         public GameObject Panel;

         // 해제용으로 보관하는 클릭 핸들러 (직렬화하지 않음)
         [NonSerialized]
         public UnityAction ClickAction;
      }

      #region Variables
      private static readonly Color TAB_SELECTED_COLOR = Color.white;
      private static readonly Color TAB_NORMAL_COLOR = new Color(0.65f, 0.65f, 0.65f, 1f);

      private OptionTab _currentTab = OptionTab.Sound;
      #endregion

      #region Fields
      [Header("탭 (사운드 / 언어 / 개인정보 / 기타)")]
      [SerializeField]
      private List<OptionTabEntry> tabs = new List<OptionTabEntry>();

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
         // 창을 열 때마다 첫 탭(사운드)부터 보여준다.
         _ShowTab(OptionTab.Sound);
         _RefreshTexts();

         PopupTween.PlayShow(gameObject);
         AnalyticsManager.LogScreenView("option");
      }

      private void OnDestroy()
      {
         _UnSubscribeListeners();
      }

      private void _SubscribeListeners()
      {
         // deletePlayerPrefsButton(로비 초기화 버튼)은 씬 onClick에 OnDeletePlayerPrefsClicked가 이미 연결돼 있다.
         // 여기서 또 AddListener하면 옵션창이 열려 있는 동안 클릭 1회에 팝업이 두 장 뜬다.

         foreach (OptionTabEntry entry in tabs)
         {
            if (entry == null || entry.Button == null)
               continue;

            OptionTab tab = entry.Tab;
            entry.ClickAction = () => _OnTabClick(tab);
            entry.Button.onClick.AddListener(entry.ClickAction);
         }

         if (tutorialReplayButton != null)
            tutorialReplayButton.onClick.AddListener(_OnTutorialReplayClick);

         Localization.OnLanguageChanged += _RefreshTexts;
         Localization.OnInitialized += _RefreshTexts;
      }

      private void _UnSubscribeListeners()
      {
         // deletePlayerPrefsButton은 등록하지 않으므로 해제도 하지 않는다.
         // (RemoveAllListeners를 부르면 씬에 연결된 것 외의 런타임 리스너까지 지운다)

         foreach (OptionTabEntry entry in tabs)
         {
            if (entry == null || entry.Button == null || entry.ClickAction == null)
               continue;

            entry.Button.onClick.RemoveListener(entry.ClickAction);
            entry.ClickAction = null;
         }

         if (tutorialReplayButton != null)
            tutorialReplayButton.onClick.RemoveListener(_OnTutorialReplayClick);

         Localization.OnLanguageChanged -= _RefreshTexts;
         Localization.OnInitialized -= _RefreshTexts;
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

      // 튜토리얼·인트로 진행 기록을 초기화한다. 인트로가 현재 씬(로비 등)에 있으면 옵션창을 닫고 즉시 다시 재생하고,
      // 없으면 다음 게임 진입 시 튜토리얼·인트로가 처음부터 다시 표시된다.
      private void _OnTutorialReplayClick()
      {
         SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_ButtonClick, ignoreSuppress: true);

         if (UserDataManager.Instance != null)
            UserDataManager.Instance.ResetTutorialData();

         if (IntroManager.Instance == null)
            return;

         // 옵션창이 인트로를 가리지 않게 먼저 닫는다. 닫으면서 재개된 시간은 인트로가 시작하며 다시 멈춘다.
         HideOptionUI();
         IntroManager.Instance.Replay();
      }

      private void _OnTabClick(OptionTab tab)
      {
         if (tab == _currentTab)
            return;

         SoundManager.Instance?.PlaySFX(SoundType.SFX_UI_ButtonClick, ignoreSuppress: true);
         _ShowTab(tab);
      }

      // 선택한 탭의 패널만 켜고, 탭 버튼 색으로 현재 탭을 표시한다.
      private void _ShowTab(OptionTab tab)
      {
         _currentTab = tab;

         foreach (OptionTabEntry entry in tabs)
         {
            if (entry == null)
               continue;

            bool isSelected = entry.Tab == tab;

            if (entry.Panel != null)
               entry.Panel.SetActive(isSelected);

            if (entry.Button != null && entry.Button.image != null)
               entry.Button.image.color = isSelected ? TAB_SELECTED_COLOR : TAB_NORMAL_COLOR;
         }
      }

      // 탭 라벨과 튜토리얼 다시보기 라벨을 현재 언어로 갱신한다.
      private void _RefreshTexts()
      {
         foreach (OptionTabEntry entry in tabs)
         {
            if (entry == null || entry.Label == null)
               continue;

            entry.Label.text = _GetTabTitle(entry.Tab);
         }

         if (tutorialReplayText != null)
            tutorialReplayText.text = LocalizeHelper.GetByKey("UI_Option_Tutorial_Replay", "튜토리얼 다시 보기");
      }

      private static string _GetTabTitle(OptionTab tab)
      {
         switch (tab)
         {
            case OptionTab.Sound:
               return LocalizeHelper.GetByKey("UI_Option_Tab_Sound", "사운드");
            case OptionTab.Language:
               return LocalizeHelper.GetByKey("UI_Option_Tab_Language", "언어");
            case OptionTab.Privacy:
               return LocalizeHelper.GetByKey("UI_Option_Tab_Privacy", "개인정보");
            default:
               return LocalizeHelper.GetByKey("UI_Option_Tab_Etc", "기타");
         }
      }
   }
}
