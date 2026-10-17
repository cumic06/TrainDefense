using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TrainDefense.Game.UI
{
   /// <summary>
   /// 드래그 시작·이동·끝을 콜백으로 넘긴다. LongPressHandler처럼 대상 GameObject에 런타임으로 AddComponent 하므로
   /// 인스펙터 와이어링이 필요 없다. 드래그 도중 꺼지면(상점 닫힘) 끝 이벤트가 오지 않으므로 onCancel로 정리한다.
   /// </summary>
   public class PointerDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
   {
      #region Variables
      private Action<PointerEventData> _onBeginDrag;
      private Action<PointerEventData> _onDrag;
      private Action<PointerEventData> _onEndDrag;
      private Action _onCancel;
      private bool _isDragging;
      #endregion

      public void Initialize(Action<PointerEventData> onBeginDrag, Action<PointerEventData> onDrag, Action<PointerEventData> onEndDrag, Action onCancel)
      {
         _onBeginDrag = onBeginDrag;
         _onDrag = onDrag;
         _onEndDrag = onEndDrag;
         _onCancel = onCancel;
      }

      private void OnDisable()
      {
         if (!_isDragging)
            return;

         _isDragging = false;
         _onCancel?.Invoke();
      }

      public void OnBeginDrag(PointerEventData eventData)
      {
         _isDragging = true;
         _onBeginDrag?.Invoke(eventData);
      }

      public void OnDrag(PointerEventData eventData)
      {
         if (!_isDragging)
            return;

         _onDrag?.Invoke(eventData);
      }

      public void OnEndDrag(PointerEventData eventData)
      {
         if (!_isDragging)
            return;

         _isDragging = false;
         _onEndDrag?.Invoke(eventData);
      }
   }
}
