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
        public static readonly Color SurfaceRaised = new Color32(0xD6, 0xB8, 0x8C, 0xFF);  // 노드 배경 (중간 탄 카드 — 포탑·도감 슬롯과 동일)
        public static readonly Color SurfaceLine = new Color32(0xA0, 0x90, 0x7C, 0xFF);    // 미점등 선로/잠김 노드 (탈채도 탄)
        public static readonly Color OnSurface = new Color32(0x3A, 0x28, 0x1E, 0xFF);      // 본문 텍스트 (INK)
        public static readonly Color OnSurfaceMuted = new Color32(0x64, 0x4E, 0x3C, 0xFF); // 잠김/비활성 (INK2)
        public static readonly Color OnAccent = new Color32(0xF8, 0xE0, 0xBD, 0xFF);       // accent 면 위 텍스트 (크림)
        public static readonly Color DangerText = new Color32(0xC8, 0x38, 0x38, 0xFF);     // 비용 부족 텍스트 (팩 빨강, 크림 위 4.6:1)
    }
}
