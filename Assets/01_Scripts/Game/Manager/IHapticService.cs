namespace TrainDefense.Game
{
   public interface IHapticService
   {
      bool IsSupported { get; }
      void Play(HapticFeedbackType type);
   }
}
