using TrainDefense.Game.Datas;

namespace TrainDefense.Game.Events
{
    public class TriChoiceSelectEvent
    {
        private TrainDefense.Game.Datas.IChoiceOption _choiceOption;
        private int _choiceLeftCount;

        public TrainDefense.Game.Datas.IChoiceOption ChoiceOption => _choiceOption;
        public int ChoiceLeftCount => _choiceLeftCount;

        public TriChoiceSelectEvent(TrainDefense.Game.Datas.IChoiceOption choiceOption, int choiceLeftCount)
        {
            _choiceOption = choiceOption;
            _choiceLeftCount = choiceLeftCount;
        }
    }
}