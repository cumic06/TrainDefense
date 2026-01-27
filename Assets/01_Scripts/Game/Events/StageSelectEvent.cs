using TrainDefense.Game.Datas;

namespace TrainDefense.Game.Events
{
    public class StageSelectEvent
    {
        public StageData SelectedStageData { get; }

        public StageSelectEvent(StageData selectedStageData)
        {
            SelectedStageData = selectedStageData;
        }
    }
}
