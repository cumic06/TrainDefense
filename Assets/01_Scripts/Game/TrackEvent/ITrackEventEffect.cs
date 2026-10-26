using TrainDefense.Game.Datas;

namespace TrainDefense.Game.TrackEvent
{
    /// <summary>
    /// 선로 이벤트 한 종류의 실제 효과. TrackEventManager가 발동 시 Begin, 매 FixedUpdate(전투 진행 중일 때만) Tick,
    /// 종료 시 End를 부른다. End는 지속 시간 만료와 강제 종료(역 도착·게임 종료) 모두에서 정확히 한 번 불린다.
    /// 효과가 바꾼 상태(스탯 배율·스폰 간격 등)는 End에서 반드시 되돌린다.
    /// </summary>
    public interface ITrackEventEffect
    {
        void Begin(TrackEventData data);

        void Tick(float deltaTime);

        void End(bool isInterrupted);
    }
}
