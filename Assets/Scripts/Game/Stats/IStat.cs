namespace TrainDefense.Game.Stats
{
    public interface IStat
    {
        StatType Type { get; }
        float Value { get; set; }
        void Combine(IStat other);
    }
}