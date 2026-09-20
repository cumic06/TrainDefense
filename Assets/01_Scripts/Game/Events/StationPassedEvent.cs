namespace TrainDefense.Game.Events
{
    public class StationPassedEvent
    {
        /// <summary>이번 판에서 지나온 역 수(첫 역이 1). 메타 재화 역 보상이 이 번호로 계산된다.</summary>
        public int PassedStationCount { get; }

        /// <summary>
        /// 이번 도착이 종착역(클리어)인지.
        /// 클리어 정산(남은 골드 환전)은 <see cref="Cumic.Events.GameEndEvent"/>가 아니라 이 신호로 처리한다 —
        /// 결과창도 GameEndEvent를 구독하므로, 같은 이벤트에 얹으면 구독 등록 순서에 따라 환전이 화면에 안 잡힐 수 있다.
        /// 이 이벤트는 GameEndEvent보다 먼저 발행되어 순서가 보장된다.
        /// </summary>
        public bool IsFinalStation { get; }

        public StationPassedEvent(int passedStationCount, bool isFinalStation = false)
        {
            PassedStationCount = passedStationCount;
            IsFinalStation = isFinalStation;
        }
    }
}
