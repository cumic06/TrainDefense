using TrainDefense.Game.Datas;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 황금 몬스터 — 현재 맵에서 제일 약한 몬스터를 금색으로 바꿔 여러 마리 소환한다. 평소처럼 기차로 다가오고, 처치하면 큰 골드를 준다.
    /// magnitude = 첫 역 기준 한 마리 골드. 역이 지날수록 TrackEventGoldReward 공식으로 오른다.
    /// 소환 후 되돌릴 상태가 없다. 살아남은 개체는 다른 몬스터와 함께 역 도착 때 정리된다.
    /// </summary>
    public class GoldenMonsterTrackEventEffect : ITrackEventEffect
    {
        private const int MONSTER_COUNT = 4;
        private const float DEFAULT_BASE_GOLD = 25f;
        private const float HP_MULTIPLIER = 15f;
        private const float MOVE_SPEED_MULTIPLIER = 0.7f;
        private const float SIZE_MULTIPLIER = 1.6f;

        public void Begin(TrackEventData data)
        {
            MonsterSpawner spawner = MonsterSpawner.Instance;
            if (spawner == null)
                return;

            float baseGold = data != null && data.Magnitude > 0f ? data.Magnitude : DEFAULT_BASE_GOLD;
            int goldReward = TrackEventGoldReward.Get(baseGold);

            for (int i = 0; i < MONSTER_COUNT; i++)
            {
                Monster monster = spawner.SpawnWeakestMonsterNow();
                if (monster == null)
                    return;

                monster.ApplyGolden(HP_MULTIPLIER, MOVE_SPEED_MULTIPLIER, SIZE_MULTIPLIER, goldReward);
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
