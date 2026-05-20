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
    }
}
