namespace TrainDefense.Game.Events
{
    public class GameOverStartEvent
    {
        public float Duration { get; }

        public GameOverStartEvent(float duration)
        {
            Duration = duration;
        }
    }
}
