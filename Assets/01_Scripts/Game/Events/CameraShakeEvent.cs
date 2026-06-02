namespace TrainDefense.Game.Events
{
    public class CameraShakeEvent
    {
        public float Intensity { get; }
        public float Duration { get; }

        public CameraShakeEvent(float intensity, float duration)
        {
            Intensity = intensity;
            Duration = duration;
        }
    }
}
