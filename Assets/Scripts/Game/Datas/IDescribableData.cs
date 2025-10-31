namespace TrainDefense.Game.Datas
{
    public interface IDescribableData : IData
    {
        string Name { get; }
        string Description { get; }
    }
}