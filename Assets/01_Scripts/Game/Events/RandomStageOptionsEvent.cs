using TrainDefense.Game.Datas;

namespace TrainDefense.Game.Events
{
    public class RandomStageOptionsEvent
    {
        public StageData StageData1 { get; }
        public StageData StageData2 { get; }

        public RandomStageOptionsEvent(StageData stageData1, StageData stageData2)
        {
            StageData1 = stageData1;
            StageData2 = stageData2;
        }
    }
}
