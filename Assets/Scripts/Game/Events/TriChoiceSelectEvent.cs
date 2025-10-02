using TrainDefense.Game.Datas;

namespace TrainDefense.Game.Events
{
    public class TriChoiceSelectEvent
    {
        private ChoiceOption _choiceOption;

        public ChoiceOption ChoiceOption => _choiceOption;

        public TriChoiceSelectEvent(ChoiceOption choiceOption)
        {
            _choiceOption = choiceOption;
        }
    }
}