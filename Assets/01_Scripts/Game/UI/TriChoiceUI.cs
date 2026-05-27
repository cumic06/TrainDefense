using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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
      [SerializeField]
      private GameObject levelUpTextObject;
      [SerializeField]
      private float cardAppearDelay = 0.3f;

      [Header("Reroll")]
      [SerializeField]
      private Button rerollButton;
      #endregion

      #region Variables
      private int _choiceLeftCount;
      private bool _isSelecting = false;
      private int _popupRequestId = 0;
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

         if (rerollButton != null)
         {
            rerollButton.onClick.AddListener(_OnRerollButtonClick);
         }

         if (levelUpTextObject != null)
            levelUpTextObject.SetActive(false);
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
         OnInspectionEnter(1, showLevelUpText: false);
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
            Debug.LogWarning("No available choices found");

            return;
         }

         if (showLevelUpText)
         {
            await _ShowLevelUpTextAsync();

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
         }
      }

      // null=요청 취소됨, true=활성화 성공, false=InfoBuild 실패(스킵)
      private async UniTask<bool?> _ActivateChoiceCardAsync(TriChoiceSelectUI choiceSelectUI, IChoiceOption choiceOption, int slotIndex, int requestId)
      {
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

      private async UniTask _ShowLevelUpTextAsync()
      {
         if (levelUpTextObject == null)
            return;

         levelUpTextObject.SetActive(true);
         levelUpTextObject.transform.localScale = Vector3.one;

         var animations = levelUpTextObject.GetComponents<DOTweenAnimation>();
         float maxDuration = 0f;

         foreach (var anim in animations)
         {
            anim.RewindThenRecreateTweenAndPlay();
            float d = anim.delay + anim.duration;
            if (d > maxDuration)
               maxDuration = d;
         }

         if (maxDuration > 0)
            await UniTask.Delay((int)(maxDuration * 1000), ignoreTimeScale: true);

         levelUpTextObject.SetActive(false);
      }

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

         if (triChoiceManager != null)
         {
            triChoiceManager.ClearSelectedChoiceData();
         }

         // 현재 선택지 UI를 숨기고 새로운 선택지로 다시 표시
         foreach (var choiceSelectUI in choiceSelectUIs)
         {
            choiceSelectUI.transform.DOKill();
            choiceSelectUI.gameObject.SetActive(false);
         }

         int requestId = ++_popupRequestId;
         _OnChoiceUIPopup(_choiceLeftCount, requestId, showLevelUpText: false).Forget();
      }
   }
}
