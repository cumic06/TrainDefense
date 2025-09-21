using UnityEngine;

namespace TrainDefense.Game.Events
{
    public class ChangeStageTimeEvent
    {
        private float _stageInspectionTime;
        public float StageInspectionTime => _stageInspectionTime;

        public ChangeStageTimeEvent(float stageInspectionTime)
        {
            _stageInspectionTime = stageInspectionTime;
        }
    }
}