namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 역 도착 시 전투 중 밀린 레벨업을 고르기 시작한다. 멈춘 전투 화면 위에 카드가 뜨고, 다 고르면 상점으로 넘어간다.
    /// </summary>
    public class StationLevelUpStartEvent
    {
        private int _levelUpCount;

        public int LevelUpCount => _levelUpCount;

        public StationLevelUpStartEvent(int levelUpCount)
        {
            _levelUpCount = levelUpCount;
        }
    }
}
