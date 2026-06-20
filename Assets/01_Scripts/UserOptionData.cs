namespace TrainDefense
{
   public class UserOptionData
   {
      public bool IsHapticEnabled { get; set; } = true;
      public float BgmVolume { get; set; } = 0.5f;
      public float SfxVolume { get; set; } = 0.5f;
      public bool IsBgmMuted { get; set; } = false;
      public bool IsSfxMuted { get; set; } = false;
      public bool IsCameraShakeEnabled { get; set; } = true;
      public int ColorblindType { get; set; } = 0;
   }
}
