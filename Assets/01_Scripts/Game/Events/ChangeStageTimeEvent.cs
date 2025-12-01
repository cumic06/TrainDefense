namespace TrainDefense.Game.Events
{
    public class ChangeStageTimeEvent
    {
        private float _stageTime;
        public float StageTime => _stageTime;

        public ChangeStageTimeEvent(float stageTime)
        {
            _stageTime = stageTime;
        }
    }
}