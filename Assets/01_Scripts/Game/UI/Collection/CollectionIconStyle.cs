using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI.Collection
{
    /// <summary>
    /// 도감 아이콘 배치. 몬스터는 실제 그림 경계(Tight 메시 정점) 기준으로 탭에서 가장 큰 몬스터가 칸을 채우는 배율 하나로 그려
    /// 인게임 상대 크기를 유지하고 바닥을 기준선에 맞춘다(UI Image는 피벗을 무시하므로 직접 계산). 트레인은 UI용 아이콘이라 칸에 그대로 채운다.
    /// </summary>
    public static class CollectionIconStyle
    {
        // 트레인 아이콘 칸 배율 (몬스터 칸 200/300 기준 150/250)
        private const float TrainIconScale = 0.75f;

        /// <param name="boxSize">프리팹 아이콘 칸 크기</param>
        /// <param name="baselineY">그림 바닥 기준선(프리팹 아이콘 anchoredPosition.y, 피벗 y=0)</param>
        /// <param name="tabMaxBody">현재 탭 몬스터 그림 경계의 최대 폭/높이(월드 유닛) — MaxBodySize</param>
        public static void Fit(Image image, CollectionEntry entry, Vector2 boxSize, float baselineY, Vector2 tabMaxBody)
        {
            if (image == null || entry == null || entry.Icon == null)
                return;

            RectTransform rectTransform = image.rectTransform;
            Sprite sprite = entry.Icon;
            Rect body = entry.IsMonster ? BodyBounds(sprite) : Rect.zero;

            if (body.width <= 0f || body.height <= 0f || tabMaxBody.x <= 0f || tabMaxBody.y <= 0f)
            {
                rectTransform.sizeDelta = boxSize * TrainIconScale;
                rectTransform.anchoredPosition = new Vector2(0f, baselineY);
                return;
            }

            float pixelsPerUnit = Mathf.Min(boxSize.x / tabMaxBody.x, boxSize.y / tabMaxBody.y);
            Vector2 size = sprite.rect.size / sprite.pixelsPerUnit * pixelsPerUnit;
            Vector2 pivot = sprite.pivot / sprite.rect.size;

            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = new Vector2(
                -((pivot.x - 0.5f) * size.x + body.center.x * pixelsPerUnit),
                baselineY - (pivot.y * size.y + body.yMin * pixelsPerUnit));
        }

        /// <summary>탭 몬스터들의 그림 경계 최대 폭/높이(월드 유닛).</summary>
        public static Vector2 MaxBodySize(IReadOnlyList<CollectionEntry> entries)
        {
            Vector2 max = Vector2.zero;
            for (int i = 0; entries != null && i < entries.Count; i++)
                if (entries[i].IsMonster && entries[i].Icon != null)
                    max = Vector2.Max(max, BodyBounds(entries[i].Icon).size);
            return max;
        }

        // Tight 메시 정점(피벗 기준 로컬 유닛)의 min/max = 실제 그림 경계
        private static Rect BodyBounds(Sprite sprite)
        {
            Vector2[] vertices = sprite.vertices;
            if (vertices == null || vertices.Length == 0)
                return Rect.zero;

            Vector2 min = vertices[0], max = vertices[0];
            for (int i = 1; i < vertices.Length; i++)
            {
                min = Vector2.Min(min, vertices[i]);
                max = Vector2.Max(max, vertices[i]);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
