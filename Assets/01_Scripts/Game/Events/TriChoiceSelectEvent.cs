using TrainDefense.Game.Datas;

namespace TrainDefense.Game.Events
{
    public class TriChoiceSelectEvent
    {
        private IChoiceOption _choiceOption;
        private int _choiceLeftCount;

        public IChoiceOption ChoiceOption => _choiceOption;
        public int ChoiceLeftCount => _choiceLeftCount;

        public TriChoiceSelectEvent(IChoiceOption choiceOption, int choiceLeftCount)
        {
            _choiceOption = choiceOption;
            _choiceLeftCount = choiceLeftCount;
        }
    }
}