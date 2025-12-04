namespace TrainDefense.Game.Datas
{
    public interface IChoiceOption
    {
        string Id { get; }
        bool IsValid();
        void Execute();
    }
}