using TrainDefense.Game.Datas;

namespace TrainDefense.Game.Events
{
    public class TriChoiceSelectEvent
    {
        private TriChoiceData _data;

        public TriChoiceData Data => _data;

        public TriChoiceSelectEvent(TriChoiceData data)
        {
            _data = data;
        }
    }
}