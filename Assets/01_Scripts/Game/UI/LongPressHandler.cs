using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TrainDefense.Game.UI
{
   /// <summary>
   /// 일정 시간 누르고 있으면 onLongPress, 롱프레스가 발동한 뒤 손을 떼면 onRelease를 부른다.
   /// 발동한 누름은 IsLongPressed가 다음 누름 전까지 true로 남아, 같은 누름의 버튼 클릭(구매 등)을 막는 데 쓴다.
   /// 대상 GameObject에 런타임으로 AddComponent 하므로 인스펙터 와이어링이 필요 없다.
   /// </summary>
   public class LongPressHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
   {
      #region Variables
      // 구매 버튼 위에서도 쓰므로 일반 탭(0.1~0.2초)이 롱프레스로 오인되지 않을 만큼 길게 둔다.
      private const float LONG_PRESS_DURATION = 0.4f;

      private Action<Vector2, Camera> _onLongPress;
      private Action _onRelease;
      private Coroutine _longPressCoroutine;
      private Vector2 _pressScreenPosition;
      private Camera _pressEventCamera;
      #endregion

      public bool IsLongPressed { get; private set; }

      // onLongPress = (누른 화면 좌표, 이벤트 카메라). onRelease = 발동한 롱프레스가 끝날 때(손 뗌·비활성).
      public void Initialize(Action<Vector2, Camera> onLongPress, Action onRelease)
      {
         _onLongPress = onLongPress;
         _onRelease = onRelease;
      }

      private void OnDisable()
      {
         _StopLongPress();

         // 누르는 도중 꺼지면(상점 닫힘·슬롯 비움) 손 뗌 이벤트가 오지 않으므로 여기서 닫는다.
         if (IsLongPressed)
         {
            IsLongPressed = false;
            _onRelease?.Invoke();
         }
      }

      public void OnPointerDown(PointerEventData eventData)
      {
         // 클릭 억제 플래그는 손 뗄 때가 아니라 다음 누름에서 푼다 — 손 뗌 직후에 오는 클릭을 막아야 하기 때문.
         IsLongPressed = false;
         _pressScreenPosition = eventData.position;
         _pressEventCamera = eventData.pressEventCamera;
         _StopLongPress();
         _longPressCoroutine = StartCoroutine(_LongPressRoutine());
      }

      public void OnPointerUp(PointerEventData eventData)
      {
         _StopLongPress();

         if (IsLongPressed)
            _onRelease?.Invoke();
      }

      private IEnumerator _LongPressRoutine()
      {
         yield return new WaitForSecondsRealtime(LONG_PRESS_DURATION);
         _longPressCoroutine = null;
         IsLongPressed = true;
         _onLongPress?.Invoke(_pressScreenPosition, _pressEventCamera);
      }

      private void _StopLongPress()
      {
         if (_longPressCoroutine == null)
            return;

         StopCoroutine(_longPressCoroutine);
         _longPressCoroutine = null;
      }
   }
}
