using System;
using UnityEngine;

namespace TrainDefense.Game
{
   internal readonly struct HapticPattern
   {
      public readonly long DurationMs;
      public readonly int Amplitude;

      public HapticPattern(long durationMs, int amplitude)
      {
         DurationMs = Math.Max(1L, durationMs);
         Amplitude = Mathf.Clamp(amplitude, 1, 255);
      }
   }
}
