using TrainDefense.Game.Datas;

namespace TrainDefense.Game.Events
{
    public class TriChoiceSelectEvent
    {
        private ChoiceOption _choiceOption;
        private int _choiceLeftCount;

        public ChoiceOption ChoiceOption => _choiceOption;
        public int ChoiceLeftCount => _choiceLeftCount;

        public TriChoiceSelectEvent(ChoiceOption choiceOption, int choiceLeftCount)
        {
            _choiceOption = choiceOption;
            _choiceLeftCount = choiceLeftCount;
        }
    }
}