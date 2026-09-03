using System.Collections;
using Cumic.Events;
using TMPro;
using TrainDefense.Game.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
   public class TrainInfoSlotUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
   {
      #region Verialbes
      #region Field
      [SerializeField]
      private Image healthImage;
      [SerializeField]
      private Image iconImage;

      [SerializeField]
      private Image trainLevelImage;
      [SerializeField]
      private TextMeshProUGUI trainLevelText;

      [SerializeField]
      [Tooltip("기차 사망 시 활성화되는 X 표시 오브젝트")]
      private GameObject deadMark;

      [SerializeField]
      private TrainDetailPopupUI detailPopup;
      #endregion

      private Train _train;
      // 엘리트 교체 시 슬롯에 부여하는 고정 이름. 튜토리얼이 GameObject.Find로 엘리트 슬롯을 찾기 위함.
      public const string EliteSlotObjectName = "EliteTrainInfoSlotUI";
      private const float LongPressDuration = 0.25f;
      // 체력바 단계 색 — GameMadang 팩 팔레트 기준(형광 Color.green/red 대신). 높음=GM_CheckV 체크 녹색, 중간=GM_ButtonSimpleYellow 면색, 낮음=GM_ButtonSimpleRed 면색
      private static readonly Color HealthColorHigh = new Color(0.47058824f, 0.72156863f, 0.1882353f);
      private static readonly Color HealthColorMiddle = new Color(0.84705882f, 0.59607843f, 0.1882353f);
      private static readonly Color HealthColorMiddleLow = new Color(0.81568627f, 0.40784314f, 0.15686275f);
      private static readonly Color HealthColorLow = new Color(0.78431374f, 0.21960784f, 0.21960784f);
      private Coroutine _longPressCoroutine;
      private bool _longPressFired;
      #endregion

      private void Start()
      {
         _SubscribeEvents();
         _RefreshHealthUI();
      }

      private void OnDestroy()
      {
         if (_longPressCoroutine != null)
         {
            StopCoroutine(_longPressCoroutine);
            _longPressCoroutine = null;
         }

         // 누르는 도중 슬롯이 파괴되면 사거리 표시도 정리.
         if (_longPressFired)
            TrainRangeIndicator.HideActive();

         _UnsubscribeEvents();
      }

      public void OnPointerDown(PointerEventData eventData)
      {
         _longPressFired = false;
         _longPressCoroutine = StartCoroutine(_LongPressRoutine());
      }

      public void OnPointerUp(PointerEventData eventData)
      {
         if (_longPressCoroutine != null)
         {
            StopCoroutine(_longPressCoroutine);
            _longPressCoroutine = null;
         }

         if (_longPressFired)
         {
            detailPopup?.Hide();
            TrainRangeIndicator.Instance.Hide();
         }
      }

      private IEnumerator _LongPressRoutine()
      {
         yield return new WaitForSecondsRealtime(LongPressDuration);
         _longPressFired = true;
         detailPopup?.Show(_train);
         // 롱프레스 동안 포탑의 실제 사거리를 게임 화면에 원형으로 표시.
         TrainRangeIndicator.Instance.Show(_train);
      }

      #region Event
      private void _SubscribeEvents()
      {
         GameEventSystem.Subscribe<HitEvent>(_SetHp);
         GameEventSystem.Subscribe<TrainDeadEvent>(_SetDead);
         GameEventSystem.Subscribe<TrainLevelUpEvent>(_SetLevelUp);
         GameEventSystem.Subscribe<ReplaceTrainEvent>(_OnReplaceTrain);
      }

      private void _UnsubscribeEvents()
      {
         GameEventSystem.Unsubscribe<HitEvent>(_SetHp);
         GameEventSystem.Unsubscribe<TrainDeadEvent>(_SetDead);
         GameEventSystem.Unsubscribe<TrainLevelUpEvent>(_SetLevelUp);
         GameEventSystem.Unsubscribe<ReplaceTrainEvent>(_OnReplaceTrain);
      }
      #endregion

      public void Init(Train train)
      {
         _train = train;
         trainLevelImage.gameObject.SetActive(false);

         if (deadMark != null)
         {
            deadMark.SetActive(false);
         }

         _RefreshHealthUI();
      }

      public void SetIcon(Sprite icon)
      {
         iconImage.sprite = icon;
      }

      private void _RefreshHealthUI()
      {
         if (healthImage == null)
            return;

         float ratio = _train != null ? _train.CurrentHpRatio : 1f;
         healthImage.fillAmount = ratio;
         healthImage.color = _GetHealthColor(ratio);
      }

      private static Color _GetHealthColor(float ratio)
      {
         if (ratio <= 0.25f)
            return HealthColorLow;
         if (ratio <= 0.5f)
            return HealthColorMiddleLow;
         if (ratio <= 0.7f)
            return HealthColorMiddle;
         return HealthColorHigh;
      }

      private void _SetHp(HitEvent hitEvent)
      {
         if (_train != hitEvent.Damageable as Train)
            return;

         float ratio = hitEvent.CurrentHpRatio;

         if (healthImage != null)
         {
            healthImage.fillAmount = ratio;
            healthImage.color = _GetHealthColor(ratio);
         }

         // 부활(체력 복원) 시 사망 X 표시 해제
         if (deadMark != null && ratio > 0f)
         {
            deadMark.SetActive(false);
         }
      }

      private void _SetLevelUp(TrainLevelUpEvent trainLevelUpEvent)
      {
         if (_train != trainLevelUpEvent.Train)
            return;

         trainLevelImage.gameObject.SetActive(true);
         trainLevelText.text = $"{trainLevelUpEvent.Level}";
      }

      private void _SetDead(TrainDeadEvent trainDeadEvent)
      {
         if (_train != trainDeadEvent.Train)
            return;

         if (healthImage != null)
         {
            healthImage.color = Color.gray;
         }

         if (deadMark != null)
         {
            deadMark.SetActive(true);
         }
      }

      private void _OnReplaceTrain(ReplaceTrainEvent replaceTrainEvent)
      {
         if (_train != replaceTrainEvent.OldTrain)
            return;

         _train = replaceTrainEvent.NewTrain;

         if (replaceTrainEvent.NewIcon != null)
         {
            SetIcon(replaceTrainEvent.NewIcon);
         }

         if (deadMark != null)
         {
            deadMark.SetActive(false);
         }

         // 엘리트 교체된 슬롯은 튜토리얼이 강조 대상으로 찾을 수 있도록 고정 이름을 부여한다.
         gameObject.name = EliteSlotObjectName;

         trainLevelImage.gameObject.SetActive(true);
         trainLevelText.text = "E";
         _RefreshHealthUI();
      }
   }
}
