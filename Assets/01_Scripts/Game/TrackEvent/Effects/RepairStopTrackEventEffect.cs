using TrainDefense.Game.Datas;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 긴급 정비 — 메인 기차와 살아있는 포탑의 체력을 최대 체력 × magnitude 만큼 회복한다. 즉발형(권장 duration 0).
    /// Train.RestoreHpByRatio가 HitEvent를 발행해 체력 UI 갱신 경로를 그대로 탄다. 되돌릴 상태가 없다.
    /// </summary>
    public class RepairStopTrackEventEffect : ITrackEventEffect
    {
        private const float DEFAULT_HEAL_RATIO = 0.3f;

        public void Begin(TrackEventData data)
        {
            var trainManager = TrainManager.Instance;

            if (trainManager == null || trainManager.MainTrain == null)
                return;

            float ratio = data != null && data.Magnitude > 0f ? data.Magnitude : DEFAULT_HEAL_RATIO;
            var mainTrain = trainManager.MainTrain;

            mainTrain.RestoreHpByRatio(ratio);

            // 메인 기차는 CurrentAliveTrains에 들어있지 않으므로 포탑은 따로 순회한다. 회복 중 목록이 바뀌지 않도록 복사해 돈다.
            var aliveTrains = mainTrain.CurrentAliveTrains.ToArray();

            foreach (var train in aliveTrains)
            {
                if (train != null && train != mainTrain)
                    train.RestoreHpByRatio(ratio);
            }
        }

        public void Tick(float deltaTime)
        {
        }

        public void End(bool isInterrupted)
        {
        }
    }
}
