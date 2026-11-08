using System;

namespace Cumic.Sequence
{
    [Flags]
    public enum OverlayPhase
    {
        None      = 0,
        Tutorial  = 1,
        LevelUp   = 2,
        MenuPause = 4,
        Option    = 8,
        TrackEventChoice = 16,   // 선로 선택 이벤트 2지선다 (TrackEventManager가 띄우고 내린다)
        InfiniteModeChoice = 32, // 종착역 도착 후 무한모드 진행 여부 팝업 (StageManager가 띄우고 내린다)
    }
}
