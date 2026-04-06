namespace TrainDefense
{
   public class UserOptionData
   {
      public bool IsHapticEnabled { get; set; } = true;
      public float BgmVolume { get; set; } = 1f;
      public float SfxVolume { get; set; } = 1f;
      public bool IsBgmMuted { get; set; } = false;
      public bool IsSfxMuted { get; set; } = false;
   }
}
