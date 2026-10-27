using Cumic.Events;
using Cumic.Sequence;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 보너스 강화 — 경험치·레벨 변화 없이 레벨업 3지선다를 1회 즉시 띄운다. 즉발형(권장 duration 0).
    /// 대기 중 레벨업 횟수만 1 올리고 LevelUpEvent를 발행하면 TriChoiceUI가 실제 레벨업과 같은 경로(오버레이 LevelUp 진입·일시정지)로 창을 연다.
    /// 이미 레벨업 창이 떠 있으면 횟수만 쌓아 두고, 창이 닫힌 뒤 역 도착 때 기존 경로(StationLevelUpStartEvent)로 처리된다.
    /// 되돌릴 상태가 없다.
    /// </summary>
    public class FreeUpgradeTrackEventEffect : ITrackEventEffect
    {
        public void Begin(TrackEventData data)
        {
            var userDataManager = UserDataManager.Instance;

            if (userDataManager == null)
                return;

            userDataManager.AddPendingLevelUps(1);

            var sequence = InGameSequence.Instance;

            if (sequence != null && (sequence.CurrentOverlays & OverlayPhase.LevelUp) != OverlayPhase.None)
                return;

            GameEventSystem.Publish(new LevelUpEvent(1));
        }

        public void Tick(float deltaTime)
        {
        }

        public void End(bool isInterrupted)
        {
        }
    }
}
