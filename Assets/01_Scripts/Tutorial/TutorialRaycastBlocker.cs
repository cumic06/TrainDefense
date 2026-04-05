using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼 오버레이의 레이캐스트 블로커.
    /// cutout 영역 내부는 터치를 통과시키고, 외부는 차단합니다.
    /// Graphic(Image 등)이 있는 GameObject에 부착해야 합니다.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class TutorialRaycastBlocker : MonoBehaviour, ICanvasRaycastFilter
    {
        private RectTransform _cutoutRect;
        private bool _blockAll = true;

        /// <summary>
        /// cutout 영역을 설정합니다. null이면 전체 차단.
        /// </summary>
        public void SetCutoutRect(RectTransform cutoutRect)
        {
            _cutoutRect = cutoutRect;
            _blockAll = cutoutRect == null;
        }

        /// <summary>
        /// 전체 차단 모드를 설정합니다 (cutout 없이 모든 터치 차단).
        /// </summary>
        public void SetBlockAll(bool blockAll)
        {
            _blockAll = blockAll;
            if (blockAll) _cutoutRect = null;
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            // 전체 차단 모드: 이 Graphic이 모든 레이캐스트를 잡음
            if (_blockAll || _cutoutRect == null)
                return true;

            // cutout 영역 내부면 false (이 Graphic을 무시 → 뒤의 요소가 받음)
            // cutout 영역 외부면 true (이 Graphic이 잡음 → 뒤의 요소 차단)
            return !RectTransformUtility.RectangleContainsScreenPoint(
                _cutoutRect, screenPoint, eventCamera);
        }
    }
}
