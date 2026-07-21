using UnityEngine;

namespace TrainDefense.Game
{
   /// <summary>
   /// 대상 아래에 표시되는 월드 체력바 공통 로직.
   /// BarFill의 X 스케일을 비율만큼 줄이고 위치를 보정해 왼쪽 끝이 고정된 채 줄어들게 한다.
   /// </summary>
   public class HealthBar : MonoBehaviour
   {
      [SerializeField]
      protected SpriteRenderer fillRenderer;

      private Vector3 _fullScale;
      private float _fullWidth;

      private void Awake()
      {
         _fullScale = fillRenderer.transform.localScale;
         _fullWidth = fillRenderer.sprite.bounds.size.x * _fullScale.x;
      }

      public void SetRatio(float ratio)
      {
         if (fillRenderer == null)
            return;

         ratio = Mathf.Clamp01(ratio);

         Transform fillTransform = fillRenderer.transform;
         Vector3 scale = _fullScale;
         scale.x *= ratio;
         fillTransform.localScale = scale;

         Vector3 position = fillTransform.localPosition;
         position.x = -_fullWidth * (1f - ratio) * 0.5f;
         fillTransform.localPosition = position;
      }
   }
}
