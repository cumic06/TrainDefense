using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;

namespace TrainDefense.Game.UI
{
   public class TriChoiceUI : MonoBehaviour
   {
      #region Fields
      [SerializeField]
      private TriChoiceSelectUI[] choiceSelectUIs;
      [SerializeField]
      private GameObject backgroundImage;

      [SerializeField]
      private float uiActiveDelay;
      [SerializeField]
      private ParticleSystem coinParticleSystem;

      [Header("Level Up Text")]
      [Tooltip("레벨업 텍스트 연출 전용 컴포넌트. 선택지 UI와 분리된 오브젝트에 부착")]
      [SerializeField]
      private LevelUpText levelUpText;
      [SerializeField]
      private float cardAppearDelay = 0.3f;

      [Header("Pause Button")]
      [Tooltip("첫 삼중택일(게임 진입 시 자동 선택지)이 떠 있는 동안 비활성화할 일시정지 버튼")]
      [SerializeField]
      private Button pauseButton;

      [Header("Coin Display")]
      [Tooltip("삼중택일 동안 표시할 보유 코인 UI(인게임 Group_Coin 복제본). 삼중택일이 뜰 때 활성화되고 카드 위에 정렬된다")]
      [SerializeField]
      private GameObject coinUI;
      #endregion

      // 삼중택일 팝업의 용도. 리롤은 현재 모드를 그대로 따른다.
      private enum ChoicePopupMode
      {
         FirstTrainPick, // 게임 진입 첫 포탑 무료 선택 (기존 GetChoices 경로)
         StatUpgrade,    // 레벨업 보상 — 스탯 업그레이드(110xxx) 전용
      }

      #region Variables
      private ChoicePopupMode _popupMode = ChoicePopupMode.FirstTrainPick;
      private int _choiceLeftCount;
      private bool _isSelecting = false;
      private int _popupRequestId = 0;
      private bool _pauseButtonLocked = false;
      private readonly List<(IChoiceOption option, int slotIndex)> _activeChoices = new();
      #endregion

      #region LifeCycle
      private static readonly int GlobalUnscaledTime = Shader.PropertyToID("_GlobalUnscaledTime");

      private void OnEnable() => Localize.Localization.OnLanguageChanged += _RefreshLanguage;
      private void OnDisable() => Localize.Localization.OnLanguageChanged -= _RefreshLanguage;

      private void Update() => Shader.SetGlobalFloat(GlobalUnscaledTime, Time.unscaledTime);

      private void Awake()
      {
         if (choiceSelectUIs.Length == 0)
         {
            choiceSelectUIs = GetComponentsInChildren<TriChoiceSelectUI>(true);
         }

         // 첫 기차 등장 연출(Timeline)이 재생되기 전, 씬 진입 즉시 일시정지 버튼을 막는다.
         // GameEnterEvent는 연출 도중 Signal로 발행되므로 그 시점에 잠그면 연출 초반에 버튼이 눌린다.
         // 해제는 첫 삼중택일이 완료될 때 이루어진다.
         _SetPauseButtonLocked(true);

         // 삼중택일이 떠 있지 않은 평상시엔 코인 UI를 꺼 둔다(인게임 HUD 코인과 중복 방지).
         _ShowCoinUI(false);
      }

      private void Start() => _SubscribeEvents();

      private void OnDestroy() => _UnsubscribeEvents();
      #endregion

      #region Sub/UnSub
      private void _SubscribeEvents()
      {
         GameEventSystem.Subscribe<GameEnterEvent>(_OnGameEnter);
         GameEventSystem.Subscribe<LevelUpEvent>(_OnLevelUp);
         GameEventSystem.Subscribe<StageEndEvent>(_OnStageEnd);
         GameEventSystem.Subscribe<GameEndEvent>(_OnGameEnd);
      }

      private void _UnsubscribeEvents()
      {
         GameEventSystem.Unsubscribe<GameEnterEvent>(_OnGameEnter);
         GameEventSystem.Unsubscribe<LevelUpEvent>(_OnLevelUp);
         GameEventSystem.Unsubscribe<StageEndEvent>(_OnStageEnd);
         GameEventSystem.Unsubscribe<GameEndEvent>(_OnGameEnd);
      }
      #endregion

      private void _RefreshLanguage()
      {
         if (!backgroundImage.activeSelf)
            return;

         foreach (var (option, slotIndex) in _activeChoices)
         {
            var info = TriChoiceManager.Instance?.GetChoiceUIInfo(option);

            if (info != null)
               choiceSelectUIs[slotIndex].SetData(option, info, this);
         }
      }

      private void _OnGameEnter(GameEnterEvent gameEnterEvent)
      {
         // 일시정지 버튼 잠금은 연출 전(Awake)에 이미 처리됨. 여기선 첫 삼중택일만 띄운다.
         _popupMode = ChoicePopupMode.FirstTrainPick;
         OnInspectionEnter(1, showLevelUpText: false);
      }

      // 첫 삼중택일(게임 진입 시 자동 선택지)이 떠 있는 동안만 일시정지 버튼을 잠근다.
      // 잠긴 상태에서만 해제하므로 레벨업 등 이후 선택지에서는 영향이 없다.
      private void _SetPauseButtonLocked(bool locked)
      {
         if (locked)
         {
            if (pauseButton != null)
               pauseButton.interactable = false;
            _pauseButtonLocked = true;
         }
         else
         {
            if (!_pauseButtonLocked)
               return;
            if (pauseButton != null)
               pauseButton.interactable = true;
            _pauseButtonLocked = false;
         }
      }

      private void _OnLevelUp(LevelUpEvent levelUpEvent)
      {
         _popupMode = ChoicePopupMode.StatUpgrade;
         OnInspectionEnter(levelUpEvent.LevelUpCount);
      }

      private void _OnStageEnd(StageEndEvent stageEndEvent)
      {
         gameObject.SetActive(false);
      }

      private void _OnGameEnd(GameEndEvent gameEndEvent)
      {
         gameObject.SetActive(false);
      }

      // 삼중택일 표시/숨김에 맞춰 보유 코인 UI를 켜고 끈다. 켤 때 카드 위에 그려지도록 정렬을 보장한다.
      private void _ShowCoinUI(bool show)
      {
         if (coinUI == null)
            return;

         if (show)
            _EnsureCoinUIAboveCards();

         coinUI.SetActive(show);
      }

      // 카드(TriChoiceSelectUI의 Canvas는 overrideSorting + sortingOrder=2)보다 위에 그려지도록
      // 코인 UI에 overrideSorting Canvas를 보장한다. (정렬용 컴포넌트만 부여 — UI 자체는 생성하지 않음)
      private void _EnsureCoinUIAboveCards()
      {
         var canvas = coinUI.GetComponent<Canvas>();

         if (canvas == null)
            canvas = coinUI.AddComponent<Canvas>();

         canvas.overrideSorting = true;
         canvas.sortingOrder = 10;
      }

      public void OnInspectionEnter(int count, bool showLevelUpText = true)
      {
         backgroundImage.SetActive(true);

         // 삼중택일에서는 코인을 쓸 수 없으므로 보유량을 띄우지 않는다(상점에서만 표시).
         _ShowCoinUI(false);

         int requestId = ++_popupRequestId;
         _OnChoiceUIPopup(count, requestId, showLevelUpText).Forget();
      }

      private async UniTask _OnChoiceUIPopup(int count, int requestId, bool showLevelUpText = true)
      {
         _choiceLeftCount = count;
         _isSelecting = false;
         _activeChoices.Clear();

         var triChoiceManager = TriChoiceManager.Instance;

         if (triChoiceManager == null)
         {
            Debug.LogError("TriChoiceManager not found");

            return;
         }

         List<ChoiceEntry> availableChoices = _popupMode switch
         {
            ChoicePopupMode.StatUpgrade => triChoiceManager.GetStatUpgradeChoices(choiceSelectUIs.Length),
            _ => triChoiceManager.GetChoices(choiceSelectUIs.Length),
         };

         if (_IsPopupOutdated(requestId))
            return;

         if (availableChoices.Count == 0)
         {
            TriChoiceSelectEvent eventData = new(null, 0);
            GameEventSystem.Publish(eventData);
            backgroundImage.SetActive(false);
            _ShowCoinUI(false);
            _SetPauseButtonLocked(false);
            Debug.LogWarning("No available choices found");

            return;
         }

         if (showLevelUpText)
         {
            if (levelUpText != null)
            {
               await levelUpText.PlayAsync(() => _IsPopupOutdated(requestId));
            }

            if (_IsPopupOutdated(requestId))
               return;

            if (cardAppearDelay > 0)
               await UniTask.Delay((int)(cardAppearDelay * 1000), ignoreTimeScale: true);
         }

         if (_IsPopupOutdated(requestId))
            return;

         int activatedCount = 0;

         for (int i = 0; i < choiceSelectUIs.Length; i++)
         {
            if (_IsPopupOutdated(requestId))
               return;

            var choiceSelectUI = choiceSelectUIs[i];
            if (choiceSelectUI == null)
               return;

            choiceSelectUI.SetSelected(false);

            if (i < availableChoices.Count)
            {
               IChoiceOption choiceOption = availableChoices[i].Option;
               bool? result = await _ActivateChoiceCardAsync(choiceSelectUI, choiceOption, i, requestId);

               if (result == null)
                  return;

               if (result == true)
                  activatedCount++;
            }
            else
            {
               choiceSelectUI.gameObject.SetActive(false);
            }
         }

         foreach (var (_, slotIndex) in _activeChoices)
            choiceSelectUIs[slotIndex].SetButtonInteractable(true);

         // 활성화된 카드가 하나도 없으면 사용자가 클릭할 대상이 없어 영구 pause 상태가 됨.
         // availableChoices가 비어있는 경우(상단 분기) 외에도 모든 항목이 unknown/null로 걸러진 케이스에서 발생 가능.
         if (activatedCount == 0)
         {
            Debug.LogWarning("TriChoiceUI: no choices activated, publishing fallback select event to release pause");
            TriChoiceSelectEvent fallback = new(null, 0);
            GameEventSystem.Publish(fallback);
            backgroundImage.SetActive(false);
            _ShowCoinUI(false);
            _SetPauseButtonLocked(false);
         }
      }

      // null=요청 취소됨, true=활성화 성공, false=InfoBuild 실패(스킵)
      private async UniTask<bool?> _ActivateChoiceCardAsync(TriChoiceSelectUI choiceSelectUI, IChoiceOption choiceOption, int slotIndex, int requestId)
      {
         if (choiceSelectUI == null)
            return null;

         ChoiceUIInfo choiceUIInfo = TriChoiceManager.Instance.GetChoiceUIInfo(choiceOption);

         if (choiceUIInfo == null)
         {
            Debug.LogError($"ChoiceOption [{choiceOption.Id}]: Failed to build ChoiceUIInfo");
            choiceSelectUI.gameObject.SetActive(false);

            return false;
         }

         choiceSelectUI.gameObject.SetActive(true);
         choiceSelectUI.transform.DOKill();
         choiceSelectUI.transform.localScale = Vector3.zero;

         choiceSelectUI.SetData(choiceOption, choiceUIInfo, this);
         _activeChoices.Add((choiceOption, slotIndex));
         // 카드가 전부 나온 뒤에 한꺼번에 켠다 (나오는 도중 클릭 방지 — 아직 안 켜진 카드에 접근하는 선택 처리도 함께 차단)
         choiceSelectUI.SetButtonInteractable(false);

         await choiceSelectUI.transform.DOScale(1, uiActiveDelay).SetEase(Ease.OutBack).OnComplete(() =>
         {
            choiceSelectUI.transform.localScale = Vector3.one;
         }).SetUpdate(true);

         if (_IsPopupOutdated(requestId))
            return null;

         return true;
      }

      private bool _IsPopupOutdated(int requestId) => requestId != _popupRequestId;

      public async UniTaskVoid OnChoiceSelected(IChoiceOption choiceOption)
      {
         if (_isSelecting)
            return;

         _isSelecting = true;
         _popupRequestId++;
         _choiceLeftCount--;

         await _HideAllCardsAsync();

         TriChoiceSelectEvent eventData = new(choiceOption, _choiceLeftCount);
         GameEventSystem.Publish(eventData);

         _isSelecting = false;

         if (_choiceLeftCount > 0)
         {
            OnInspectionEnter(_choiceLeftCount);

            return;
         }

         if (coinParticleSystem != null)
         {
            coinParticleSystem.gameObject.SetActive(false);
         }

         backgroundImage.SetActive(false);
         _ShowCoinUI(false);
         _SetPauseButtonLocked(false);
      }

      private async UniTask _HideAllCardsAsync()
      {
         var tasks = new List<UniTask>();

         foreach (var choiceSelectUI in choiceSelectUIs)
         {
            choiceSelectUI.transform.DOKill();
            choiceSelectUI.transform.localScale = Vector3.one;
            choiceSelectUI.SetButtonInteractable(false);
            choiceSelectUI.SetSelected(true);
            choiceSelectUI.SetNewText(false);

            tasks.Add(choiceSelectUI.transform.DOScale(0, uiActiveDelay)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                   choiceSelectUI.transform.localScale = Vector3.zero;
                })
                .ToUniTask());
         }

         await UniTask.WhenAll(tasks);
      }
   }
}
