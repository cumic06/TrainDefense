using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TrainDefense.Game.UI
{
   /// <summary>
   /// 버튼을 누르고 있는 동안 일정 간격(점점 빨라짐)으로 콜백을 반복 호출한다.
   /// 콜백이 false를 반환하면(더 이상 살 수 없음 등) 즉시 반복을 멈춘다.
   /// 대상 버튼의 GameObject에 런타임으로 AddComponent 하므로 인스펙터 와이어링이 필요 없다.
   /// </summary>
   public class ButtonHoldRepeater : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
   {
      #region Variables
      // 누른 직후 반복이 시작되기까지의 대기 시간
      private const float holdDelay = 0.35f;
      // 반복 시작 간격
      private const float startCooldown = 0.18f;
      // 가속의 하한 간격
      private const float minCooldown = 0.06f;
      // 매 반복마다 간격에 곱해지는 감속 계수(작을수록 빨리 가속)
      private const float cooldownDecay = 0.82f;

      private Func<bool> _onTrigger;
      private bool _isHolding;
      private float _holdElapsed;
      private float _cooldown;
      private float _currentCooldown;
      #endregion

      #region LifeCycle
      private void Update()
      {
         if (!_isHolding)
            return;

         _holdElapsed += Time.unscaledDeltaTime;

         if (_holdElapsed < holdDelay)
            return;

         _cooldown -= Time.unscaledDeltaTime;

         if (_cooldown > 0f)
            return;

         bool keepGoing = _Trigger();

         if (!keepGoing)
         {
            _StopHold();

            return;
         }

         _currentCooldown = Mathf.Max(minCooldown, _currentCooldown * cooldownDecay);
         _cooldown = _currentCooldown;
      }
      #endregion

      public void Init(Func<bool> onTrigger)
      {
         _onTrigger = onTrigger;
      }

      public void OnPointerDown(PointerEventData eventData)
      {
         // 누르는 즉시 1회 실행. 단발 탭도 이 경로로 처리된다.
         bool keepGoing = _Trigger();

         if (!keepGoing)
            return;

         _isHolding = true;
         _holdElapsed = 0f;
         _currentCooldown = startCooldown;
         _cooldown = 0f;
      }

      public void OnPointerUp(PointerEventData eventData)
      {
         _StopHold();
      }

      public void OnPointerExit(PointerEventData eventData)
      {
         _StopHold();
      }

      private bool _Trigger()
      {
         return _onTrigger != null && _onTrigger.Invoke();
      }

      private void _StopHold()
      {
         _isHolding = false;
      }
   }
}
