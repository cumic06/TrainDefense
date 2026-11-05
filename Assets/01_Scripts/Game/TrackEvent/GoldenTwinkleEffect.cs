using UnityEngine;

namespace TrainDefense.Game.TrackEvent
{
    /// <summary>
    /// 황금 몬스터 주위에서 번갈아 깜박이는 별빛 4개. 몬스터가 처음 황금이 될 때 자식으로 한 번 만들고 켜고 끄며 재사용한다.
    /// 그림은 팩의 별빛 스프라이트이고, 정렬은 몬스터 모델보다 항상 앞이다.
    /// </summary>
    public class GoldenTwinkleEffect : MonoBehaviour
    {
        private const float MAX_STAR_SCALE = 0.42f;
        private const float TWINKLE_SPEED = 7.5f;
        private const float STAR_PHASE_STEP = 2.25f;
        private const int SORTING_ORDER_OFFSET = 5;

        private static readonly Vector2[] StarPositions =
        {
            new Vector2(-0.75f, 1.2f),
            new Vector2(0.8f, 0.95f),
            new Vector2(-0.55f, 0.05f),
            new Vector2(0.65f, -0.15f),
        };

        private SpriteRenderer _modelRenderer;
        private SpriteRenderer[] _starRenderers;
        private float _phaseOffset;

        public static GoldenTwinkleEffect Create(Transform parent, SpriteRenderer modelRenderer)
        {
            var effectObject = new GameObject("GoldenTwinkleEffect");
            effectObject.transform.SetParent(parent, false);

            var effect = effectObject.AddComponent<GoldenTwinkleEffect>();
            effect._modelRenderer = modelRenderer;
            effect._CreateStars();

            return effect;
        }

        private void OnEnable()
        {
            _phaseOffset = Random.value * Mathf.PI * 2f;
        }

        private void LateUpdate()
        {
            if (_starRenderers == null)
                return;

            for (int i = 0; i < _starRenderers.Length; i++)
            {
                SpriteRenderer star = _starRenderers[i];
                float wave = Mathf.Max(0f, Mathf.Sin(Time.time * TWINKLE_SPEED + _phaseOffset + i * STAR_PHASE_STEP));
                star.transform.localScale = Vector3.one * (MAX_STAR_SCALE * wave);

                if (_modelRenderer != null)
                {
                    star.sortingLayerID = _modelRenderer.sortingLayerID;
                    star.sortingOrder = _modelRenderer.sortingOrder + SORTING_ORDER_OFFSET;
                }
            }
        }

        private void _CreateStars()
        {
            _starRenderers = new SpriteRenderer[StarPositions.Length];

            for (int i = 0; i < StarPositions.Length; i++)
            {
                var starObject = new GameObject("Star");
                starObject.transform.SetParent(transform, false);
                starObject.transform.localPosition = StarPositions[i];

                SpriteRenderer starRenderer = starObject.AddComponent<SpriteRenderer>();
                starRenderer.sprite = SupplyCrate.GetStarSprite();
                _starRenderers[i] = starRenderer;
            }
        }
    }
}
