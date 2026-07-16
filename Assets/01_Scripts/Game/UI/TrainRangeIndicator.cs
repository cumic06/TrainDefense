using UnityEngine;

namespace TrainDefense.Game.UI
{
   /// <summary>
   /// 포탑의 실제 사거리를 월드 공간에 원형 라인으로 표시한다.
   /// 프리팹 없이 런타임에 LineRenderer로 생성되며, 표시되는 동안 대상 포탑 위치를 따라간다.
   /// TrainInfoSlotUI 롱프레스 동안 Show/Hide로 제어한다.
   /// </summary>
   public class TrainRangeIndicator : MonoBehaviour
   {
      private const int SegmentCount = 72;
      private const float LineWidth = 0.1f;

      private static TrainRangeIndicator _instance;

      public static TrainRangeIndicator Instance
      {
         get
         {
            if (_instance == null)
            {
               var go = new GameObject(nameof(TrainRangeIndicator));
               _instance = go.AddComponent<TrainRangeIndicator>();
               _instance._Build();
            }

            return _instance;
         }
      }

      // 원 내부 채움 알파. 사거리가 길어져 테두리가 화면 밖으로 나가도 범위가 보이도록 은은하게 칠한다.
      private const float FillAlpha = 0.15f;

      private LineRenderer _line;
      private SpriteRenderer _fill;
      private float _fillSpriteDiameter = 1f;
      private Train _target;
      private float _radius;

      private void _Build()
      {
         _line = gameObject.AddComponent<LineRenderer>();
         _line.useWorldSpace = true;
         _line.loop = true;
         _line.positionCount = SegmentCount;
         _line.widthMultiplier = LineWidth;
         _line.numCapVertices = 2;
         _line.textureMode = LineTextureMode.Stretch;
         // 스프라이트 기반 2D 프로젝트라 Sprites/Default 셰이더는 항상 빌드에 포함됨.
         _line.material = new Material(Shader.Find("Sprites/Default"));
         var color = new Color(0.3f, 0.85f, 1f, 0.85f);
         _line.startColor = color;
         _line.endColor = color;
         // 배경 Rail(Map 레이어)에 가려지지 않도록 전용 Range 레이어에 배치.
         _line.sortingLayerName = "Range";
         _line.sortingOrder = 500;

         // 원 내부 채움 (SoftCircle 스프라이트를 반투명 틴트, 반지름은 스케일로)
         _fill = new GameObject("Fill").AddComponent<SpriteRenderer>();
         _fill.transform.SetParent(transform, false);
         _fill.sprite = Resources.Load<Sprite>("Sprites/SoftCircle");
         _fill.color = new Color(color.r, color.g, color.b, FillAlpha);
         _fill.sortingLayerName = "Range";
         _fill.sortingOrder = 499; // 테두리 라인 바로 아래
         if (_fill.sprite != null)
            _fillSpriteDiameter = _fill.sprite.bounds.size.x;

         gameObject.SetActive(false);
      }

      /// <summary>대상 포탑의 현재(업그레이드 반영) 사거리를 원으로 표시.</summary>
      public void Show(Train train)
      {
         if (train == null || train.IsDead)
         {
            Hide();
            return;
         }

         _target = train;
         _radius = Mathf.Max(0f, train.RangeIndicatorRadius);

         if (_radius <= 0f)
         {
            Hide();
            return;
         }

         gameObject.SetActive(true);
         _Redraw();
      }

      public void Hide()
      {
         _target = null;
         gameObject.SetActive(false);
      }

      /// <summary>인스턴스가 이미 있을 때만 숨김. 종료/파괴 중 불필요한 생성 방지.</summary>
      public static void HideActive()
      {
         if (_instance != null)
            _instance.Hide();
      }

      private void LateUpdate()
      {
         // 표시 중 포탑이 이동/사망할 수 있으므로 매 프레임 추적.
         if (_target == null || _target.IsDead)
         {
            Hide();
            return;
         }

         _Redraw();
      }

      private void _Redraw()
      {
         Vector3 center = _target.transform.position;
         _fill.transform.position = center;
         _fill.transform.localScale = Vector3.one * (_radius * 2f / _fillSpriteDiameter);
         for (int i = 0; i < SegmentCount; i++)
         {
            float angle = (float)i / SegmentCount * Mathf.PI * 2f;
            _line.SetPosition(i, new Vector3(
               center.x + Mathf.Cos(angle) * _radius,
               center.y + Mathf.Sin(angle) * _radius,
               0f));
         }
      }
   }
}
