using UnityEngine;

namespace TrainDefense.Game.UI.SkillTree
{
    /// <summary>
    /// DESIGN.md 확정 색 토큰 — 스킬트리 UI는 여기서만 색을 가져다 쓴다 (새 색 발명 금지).
    /// accent는 "습득/점등된 선로" 전용 — 장식·강조·버튼에 흘리면 시그니처가 죽는다.
    /// </summary>
    public static class SkillTreePalette
    {
        public static readonly Color Accent = new Color32(0xE4, 0x7A, 0x3C, 0xFF);         // 습득/점등 전용 오렌지
        public static readonly Color Mastered = new Color32(0xFF, 0xDE, 0x33, 0xFF);       // 만렙 골드
        public static readonly Color SurfaceSunken = new Color32(0x0F, 0x13, 0x1C, 0xFF);  // 트리 캔버스 배경
        public static readonly Color SurfaceRaised = new Color32(0x26, 0x2F, 0x40, 0xFF);  // 노드 배경
        public static readonly Color SurfaceLine = new Color32(0x34, 0x3F, 0x52, 0xFF);    // 미점등 선로/헤어라인
        public static readonly Color OnSurface = new Color32(0xD9, 0xD9, 0xE6, 0xFF);      // 본문 텍스트
        public static readonly Color OnSurfaceMuted = new Color32(0x99, 0x9A, 0xA6, 0xFF); // 잠김/비활성
        public static readonly Color OnAccent = new Color32(0xF8, 0xE0, 0xBD, 0xFF);       // accent 면 위 텍스트
        public static readonly Color DangerText = new Color32(0xE0, 0x66, 0x66, 0xFF);     // 비용 부족 텍스트 (⚠️ #B34040은 대비 2.78:1 미달 — 텍스트 금지)
    }
}
