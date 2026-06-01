using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

      [Header("Reroll")]
      [SerializeField]
      private Button rerollButton;
      [Tooltip("리롤 버튼의 라벨 텍스트. 원본 텍스트 아래에 현재 리롤 비용을 함께 표기한다")]
      [SerializeField]
      private TextMeshProUGUI rerollLabelText;
      [Tooltip("코인이 부족할 때 비용 텍스트에 적용할 색")]
      [SerializeField]
      private Color rerollInsufficientColor = Color.red;
      [Tooltip("비용 표기(코인 아이콘+금액)를 라벨 기본 크기 대비 몇 배로 표기할지 (0.5 = 50%, 폰트 40 기준 20)")]
      [Range(0.3f, 1f)]
      [SerializeField]
      private float rerollCostFontScale = 0.5f;

      [Header("Pause Button")]
      [Tooltip("첫 삼중택일(게임 진입 시 자동 선택지)이 떠 있는 동안 비활성화할 일시정지 버튼")]
      [SerializeField]
      private Button pauseButton;
      #endregion

      #region Variables
      private int _choiceLeftCount;
      private bool _isSelecting = false;
      private int _popupRequestId = 0;
      private bool _pauseButtonLocked = false;
      private readonly List<(IChoiceOption option, int slotIndex)> _activeChoices = new();

      private string _rerollLabelPrefix;
      private Color _rerollLabelOriginalColor = Color.white;
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

         if (rerollButton != null)
         {
            rerollButton.onClick.AddListener(_OnRerollButtonClick);
         }

         // rerollLabelText 미연결 시 리롤 버튼 자식 텍스트를 자동으로 사용한다.
         if (rerollLabelText == null && rerollButton != null)
         {
            rerollLabelText = rerollButton.GetComponentInChildren<TextMeshProUGUI>(true);
         }

         // 비용을 라벨에 덧붙이기 전, 씬에 설정된 원본 라벨 텍스트와 색을 보존한다.
         if (rerollLabelText != null)
         {
            _rerollLabelPrefix = rerollLabelText.text;
            _rerollLabelOriginalColor = rerollLabelText.color;

            // <sprite name="Coin"> 태그를 그리려면 스프라이트 에셋이 필요하다.
            // 라벨 TMP에 미지정(상점 금액 TMP는 지정됨)이면 상점과 동일한 코인 에셋을 로드한다.
            if (rerollLabelText.spriteAsset == null)
            {
               var coinSpriteAsset = Resources.Load<TMP_SpriteAsset>("Sprite/Coin");

               if (coinSpriteAsset != null)
                  rerollLabelText.spriteAsset = coinSpriteAsset;
            }
         }

         // 첫 기차 등장 연출(Timeline)이 재생되기 전, 씬 진입 즉시 일시정지 버튼을 막는다.
         // GameEnterEvent는 연출 도중 Signal로 발행되므로 그 시점에 잠그면 연출 초반에 버튼이 눌린다.
         // 해제는 첫 삼중택일이 완료될 때 이루어진다.
         _SetPauseButtonLocked(true);
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

      public void OnInspectionEnter(int count, bool showLevelUpText = true)
      {
         backgroundImage.SetActive(true);

         // 삼중택일이 새로 열릴 때마다 리롤 비용을 기본값으로 초기화한다.
         // (리롤은 OnInspectionEnter를 거치지 않고 _OnChoiceUIPopup을 직접 호출하므로 비용이 유지·증가됨)
         _ResetRerollUI();

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

         List<ChoiceEntry> availableChoices = triChoiceManager.GetChoices(choiceSelectUIs.Length);

         if (_IsPopupOutdated(requestId))
            return;

         if (availableChoices.Count == 0)
         {
            TriChoiceSelectEvent eventData = new(null, 0);
            GameEventSystem.Publish(eventData);
            backgroundImage.SetActive(false);
            _SetPauseButtonLocked(false);
            Debug.LogWarning("No available choices found");

            return;
         }

         if (showLevelUpText)
         {
            if (levelUpText != null)
            {
               // 레벨업 텍스트 연출 동안에는 리롤 버튼 비활성화
               if (rerollButton != null)
                  rerollButton.gameObject.SetActive(false);

               await levelUpText.PlayAsync(() => _IsPopupOutdated(requestId));

               // 레벨업 텍스트 연출이 끝나면 리롤 버튼 다시 활성화
               if (rerollButton != null)
                  rerollButton.gameObject.SetActive(true);
            }

            if (_IsPopupOutdated(requestId))
               return;

            if (cardAppearDelay > 0)
               await UniTask.Delay((int)(cardAppearDelay * 1000), ignoreTimeScale: true);
         }

         if (_IsPopupOutdated(requestId))
            return;

         if (coinParticleSystem != null)
         {
            coinParticleSystem.gameObject.SetActive(true);
            coinParticleSystem.Play();
         }

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

         // 활성화된 카드가 하나도 없으면 사용자가 클릭할 대상이 없어 영구 pause 상태가 됨.
         // availableChoices가 비어있는 경우(상단 분기) 외에도 모든 항목이 unknown/null로 걸러진 케이스에서 발생 가능.
         if (activatedCount == 0)
         {
            Debug.LogWarning("TriChoiceUI: no choices activated, publishing fallback select event to release pause");
            TriChoiceSelectEvent fallback = new(null, 0);
            GameEventSystem.Publish(fallback);
            backgroundImage.SetActive(false);
            _SetPauseButtonLocked(false);
         }
      }

      // null=요청 취소됨, true=활성화 성공, false=InfoBuild 실패(스킵)
      private async UniTask<bool?> _ActivateChoiceCardAsync(TriChoiceSelectUI choiceSelectUI, IChoiceOption choiceOption, int slotIndex, int requestId)
      {
         if (choiceSelectUI == null)
            return null;

         var userDataManager = UserDataManager.Instance;

         if (userDataManager != null)
         {
            bool isFirstTime = userDataManager.IsFirstTimeSelected(choiceOption.Id);
            choiceSelectUI.SetNewText(isFirstTime);
         }

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
         choiceSelectUI.SetButtonInteractable(true);

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

      private void _OnRerollButtonClick()
      {
         if (_isSelecting)
            return;

         var triChoiceManager = TriChoiceManager.Instance;

         // 코인 차감·비용 증가는 매니저가 처리한다. 코인 부족 시엔 버튼 interactable이 이미 꺼져 있어
         // 클릭되지 않지만, 방어적으로 다시 확인하고 차단한다.
         if (triChoiceManager == null || !triChoiceManager.TryReroll())
            return;

         _RefreshRerollUI();
         triChoiceManager.ClearSelectedChoiceData();

         // 현재 선택지 UI를 숨기고 새로운 선택지로 다시 표시
         foreach (var choiceSelectUI in choiceSelectUIs)
         {
            choiceSelectUI.transform.DOKill();
            choiceSelectUI.gameObject.SetActive(false);
         }

         int requestId = ++_popupRequestId;
         _OnChoiceUIPopup(_choiceLeftCount, requestId, showLevelUpText: false).Forget();
      }

      // 삼중택일이 새로 열릴 때 호출: 매니저 비용을 기본값으로 초기화하고 리롤 UI를 갱신한다.
      private void _ResetRerollUI()
      {
         TriChoiceManager.Instance?.ResetRerollCost();
         _RefreshRerollUI();
      }

      // 현재 리롤 비용(매니저가 계산)을 라벨에 표기한다.
      // 코인이 부족하면 금액을 빨간색으로 표기하고 리롤 버튼을 비활성화한다.
      private void _RefreshRerollUI()
      {
         var manager = TriChoiceManager.Instance;
         int cost = manager != null ? manager.CurrentRerollCost : 0;
         bool canReroll = manager != null && manager.CanReroll();

         if (rerollLabelText != null)
         {
            int costSizePercent = Mathf.RoundToInt(rerollCostFontScale * 100f);
            rerollLabelText.text = $"{_rerollLabelPrefix}\n<size={costSizePercent}%><sprite name=\"Coin\"> {cost.ToCommaString()}$</size>";
            rerollLabelText.color = canReroll ? _rerollLabelOriginalColor : rerollInsufficientColor;
         }

         if (rerollButton != null)
            rerollButton.interactable = canReroll;
      }
   }
}
