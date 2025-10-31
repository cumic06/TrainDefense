namespace TrainDefense.Game.Datas
{
    public interface IChoiceOption
    {
        string Id { get; }
        void Initialize(DB db);
        ChoiceUIInfo GetUIInfo();
        bool IsValid();
        void Execute();
    }
}