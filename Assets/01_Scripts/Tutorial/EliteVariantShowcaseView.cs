using System.Collections.Generic;
using TrainDefense.Game.Datas;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 엘리트 튜토리얼에서 같은 몬스터의 엘리트 종류(불꽃 색)를 카드 한 줄로 보여 주는 UI.
    /// 실제 몬스터를 소환하지 않고, 처음 나타난 엘리트의 지금 모습(몸·불꽃 스프라이트)을 옮겨 그린 뒤 불꽃만 종류 색으로 칠한다.
    /// 입력은 받지 않는다(모든 Image raycastTarget = false) — 탭은 튜토리얼 오버레이가 처리한다.
    /// </summary>
    public class EliteVariantShowcaseView : MonoBehaviour
    {
        #region Variables

        private static readonly Vector2 CARD_SIZE = new Vector2(240f, 280f);
        private const float CARD_SPACING = 40f;
        private const float CARD_INNER_PADDING = 24f;
        private static readonly Color CARD_FALLBACK_COLOR = new Color(0.12f, 0.1f, 0.1f, 0.92f);

        #endregion

        #region Fields

        private RectTransform _row;
        private readonly Dictionary<EliteType, RectTransform> _cards = new();

        #endregion

        public RectTransform Row => _row;

        /// <summary>
        /// 카드 줄을 만든다. rowCenter는 부모(튜토리얼 오버레이) 기준 위치.
        /// </summary>
        public static EliteVariantShowcaseView Create(Monster source, IReadOnlyList<EliteVariant> variants,
            Sprite cardBackground, Vector2 rowCenter)
        {
            var root = new GameObject("EliteVariantShowcase", typeof(RectTransform));
            var view = root.AddComponent<EliteVariantShowcaseView>();
            view._Build(source, variants, cardBackground, rowCenter);

            return view;
        }

        /// <summary>해당 종류 카드. 없으면 null.</summary>
        public RectTransform GetCard(EliteType type)
        {
            return _cards.TryGetValue(type, out RectTransform card) ? card : null;
        }

        private void _Build(Monster source, IReadOnlyList<EliteVariant> variants, Sprite cardBackground, Vector2 rowCenter)
        {
            _row = (RectTransform)transform;
            _SetCentered(_row);

            int count = variants.Count;
            float rowWidth = count * CARD_SIZE.x + Mathf.Max(0, count - 1) * CARD_SPACING;
            _row.sizeDelta = new Vector2(rowWidth, CARD_SIZE.y);
            _row.anchoredPosition = rowCenter;

            for (int i = 0; i < count; i++)
            {
                RectTransform card = _CreateCard(source, variants[i], cardBackground);
                card.anchoredPosition = new Vector2(-rowWidth * 0.5f + CARD_SIZE.x * 0.5f + i * (CARD_SIZE.x + CARD_SPACING), 0f);
                _cards[variants[i].type] = card;
            }
        }

        private RectTransform _CreateCard(Monster source, EliteVariant variant, Sprite cardBackground)
        {
            var card = new GameObject($"Card_{variant.type}", typeof(RectTransform), typeof(Image));
            var cardRect = (RectTransform)card.transform;
            cardRect.SetParent(_row, false);
            _SetCentered(cardRect);
            cardRect.sizeDelta = CARD_SIZE;

            var background = card.GetComponent<Image>();
            background.raycastTarget = false;

            if (cardBackground != null)
            {
                background.sprite = cardBackground;
                background.type = Image.Type.Sliced;
            }
            else
            {
                background.color = CARD_FALLBACK_COLOR;
            }

            _DrawMonster(cardRect, source, variant.tintColor);

            return cardRect;
        }

        // 몸과 불꽃의 월드상 크기·위치 비율을 그대로 살려 카드 안쪽 영역에 맞춰 넣는다.
        // 몸 스프라이트는 투명 여백이 커서(예: 해골 192px 중 가운데 1/3), 실제로 보이는 부분(메시 정점 범위)을 기준으로 맞춘다.
        // 게임 화면과 같게 불꽃(UnitEffect 레이어)을 먼저, 몸(Unit 레이어)을 그 위에 그린다.
        private void _DrawMonster(RectTransform card, Monster source, Color flameColor)
        {
            SpriteRenderer body = source != null ? source.ModelRenderer : null;
            SpriteRenderer flame = source != null ? source.EliteEffectRenderer : null;

            if (body == null || body.sprite == null)
                return;

            bool hasFlame = flame != null && flame.sprite != null;
            Rect visible = _GetWorldVisibleRect(body);

            if (hasFlame)
            {
                Rect flameVisible = _GetWorldVisibleRect(flame);
                visible = Rect.MinMaxRect(Mathf.Min(visible.xMin, flameVisible.xMin), Mathf.Min(visible.yMin, flameVisible.yMin),
                    Mathf.Max(visible.xMax, flameVisible.xMax), Mathf.Max(visible.yMax, flameVisible.yMax));
            }

            Vector2 inner = CARD_SIZE - Vector2.one * CARD_INNER_PADDING * 2f;
            float scale = Mathf.Min(inner.x / Mathf.Max(visible.width, 0.01f), inner.y / Mathf.Max(visible.height, 0.01f));

            if (hasFlame)
                _CreateSpriteImage(card, "Flame", flame, _GetWorldSpriteRect(flame), visible.center, scale, flameColor);

            _CreateSpriteImage(card, "Body", body, _GetWorldSpriteRect(body), visible.center, scale, Color.white);
        }

        // 스프라이트 메시 정점 범위(투명 여백 제외, 실제로 그려지는 부분)를 월드 좌표로 구한다.
        private static Rect _GetWorldVisibleRect(SpriteRenderer spriteRenderer)
        {
            Vector2[] vertices = spriteRenderer.sprite.vertices;

            if (vertices == null || vertices.Length == 0)
                return _GetWorldSpriteRect(spriteRenderer);

            Vector2 min = vertices[0];
            Vector2 max = vertices[0];

            foreach (Vector2 vertex in vertices)
            {
                min = Vector2.Min(min, vertex);
                max = Vector2.Max(max, vertex);
            }

            Vector2 lossy = spriteRenderer.transform.lossyScale;
            Vector2 position = spriteRenderer.transform.position;
            Vector2 a = position + new Vector2(min.x * lossy.x, min.y * lossy.y);
            Vector2 b = position + new Vector2(max.x * lossy.x, max.y * lossy.y);

            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private static void _CreateSpriteImage(RectTransform card, string name, SpriteRenderer source, Rect worldRect,
            Vector2 worldCenter, float scale, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(card, false);
            _SetCentered(rect);
            rect.sizeDelta = worldRect.size * scale;
            rect.anchoredPosition = (worldRect.center - worldCenter) * scale;

            // 몬스터가 왼쪽을 보고 있으면(음수 스케일·flipX) 카드에서도 뒤집는다.
            bool isFlipped = (source.transform.lossyScale.x < 0f) != source.flipX;
            rect.localScale = new Vector3(isFlipped ? -1f : 1f, 1f, 1f);

            var image = go.GetComponent<Image>();
            image.sprite = source.sprite;
            image.color = color;
            image.raycastTarget = false;
        }

        // 스프라이트 사각형(투명 여백 포함, Image가 그리는 범위와 같음)을 월드 좌표로 구한다.
        private static Rect _GetWorldSpriteRect(SpriteRenderer spriteRenderer)
        {
            Sprite sprite = spriteRenderer.sprite;
            Vector2 lossy = spriteRenderer.transform.lossyScale;
            Vector2 size = sprite.rect.size / sprite.pixelsPerUnit;
            Vector2 worldSize = new Vector2(size.x * Mathf.Abs(lossy.x), size.y * Mathf.Abs(lossy.y));
            Vector2 centerFromPivot = (sprite.rect.size * 0.5f - sprite.pivot) / sprite.pixelsPerUnit;
            Vector2 worldCenter = (Vector2)spriteRenderer.transform.position
                + new Vector2(centerFromPivot.x * lossy.x, centerFromPivot.y * lossy.y);

            return new Rect(worldCenter - worldSize * 0.5f, worldSize);
        }

        private static void _SetCentered(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
