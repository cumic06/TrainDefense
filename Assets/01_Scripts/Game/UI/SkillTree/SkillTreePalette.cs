using UnityEngine;

namespace TrainDefense.Game.UI.SkillTree
{
    /// <summary>
    /// DESIGN.md 확정 색 토큰 — 스킬트리 UI는 여기서만 색을 가져다 쓴다 (새 색 발명 금지).
    /// accent는 "습득/점등된 선로" 전용 — 장식·강조·버튼에 흘리면 시그니처가 죽는다.
    /// </summary>
    public static class SkillTreePalette
    {
        // 2026-08-22 팩 톤(GameMadang 나무판+크림) 전환 — 의미는 유지, 값만 크림 바탕용으로 교체 (accent 오렌지·용도 규칙은 그대로)
        public static readonly Color Accent = new Color32(0xE4, 0x7A, 0x3C, 0xFF);         // 습득/점등 전용 오렌지
        public static readonly Color Mastered = new Color32(0xFF, 0xCC, 0x40, 0xFF);       // 만렙 골드 (GOLD_SELECT 토큰과 동일)
        public static readonly Color SurfaceSunken = new Color32(0xEC, 0xE0, 0xC4, 0xFF);  // 트리 캔버스 배경 (크림 박스)
        // 09-11: 노드 면을 크림 계열로. 탄 #D6B88C는 크림 판 위 카드(포탑·도감 슬롯)용이라 어두운 갈색 트리 판 위에서는
        // 갈색·크림 어느 쪽에도 속하지 않는 세 번째 색이 되어 겉돌았다(사용자: "배경 색이 너무 안 어울려").
        // 팝업 문법 = 갈색 테두리 + 크림 내용면 → 가능 노드는 상세 카드와 같은 크림, 잠김은 크림·트리 판(#7A5D3A) 50% 블렌드.
        public static readonly Color SurfaceRaised = new Color32(0xDC, 0xC8, 0xA9, 0xFF);  // 습득 가능 노드 배경 (상세 카드 GM_CreamBox 실측 크림)
        public static readonly Color SurfaceLine = new Color32(0xAB, 0x92, 0x71, 0xFF);    // 잠김 노드 / 미점등 선로 (흐려진 크림)
        public static readonly Color OnSurface = new Color32(0x3A, 0x28, 0x1E, 0xFF);      // 본문 텍스트 (INK)
        public static readonly Color OnSurfaceMuted = new Color32(0x64, 0x4E, 0x3C, 0xFF); // 잠김/비활성 (INK2)
        // 09-30: 습득·만렙 노드도 면은 크림 — 칸 전체 오렌지·골드는 판 위 세 번째·네 번째 색이 되어 겉돌았다(사용자: "색감이 좀 이상해").
        // 습득은 선로 점등 + 이 글자색, 만렙은 골드 테두리로 알린다. 크림 위 4.6:1 (accent 오렌지 글자는 1.8:1이라 불가)
        public static readonly Color AcquiredText = new Color32(0x8E, 0x3B, 0x12, 0xFF);   // 습득·만렙 노드 레벨 글자 (짙은 오렌지)
        public static readonly Color DangerText = new Color32(0xC8, 0x38, 0x38, 0xFF);     // 비용 부족 텍스트 (팩 빨강, 크림 위 4.6:1)
        public static readonly Color Selected = Color.white;                                // 선택 노드 테두리 글로우 (팩 GM_SlotBoxRing과 같은 흰색)
    }
}
