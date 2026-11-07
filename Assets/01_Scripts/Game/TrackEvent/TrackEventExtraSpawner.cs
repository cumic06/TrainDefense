using System;

namespace TrainDefense.Game.TrackEvent
{
    /// <summary>
    /// 평소 소환 속도의 rateMultiplier배만큼 몬스터를 더 소환하는 타이머. 몬스터 러시·험한 길이 같이 쓴다.
    /// 소환기 본래 주기는 건드리지 않고 위에 얹기만 하므로, 효과가 끝날 때 되돌릴 상태가 없다.
    /// </summary>
    public class TrackEventExtraSpawner
    {
        private readonly float _rateMultiplier;
        private readonly Func<MonsterSpawner, Monster> _spawnOne;
        private float _elapsedTime;

        /// <param name="rateMultiplier">평소 소환 속도 대비 추가 소환 속도. 1.5면 평소의 1.5배를 더 소환한다.</param>
        /// <param name="spawnOne">한 마리를 소환하는 방법(제일 약한 몬스터 / 평소 확률 몬스터).</param>
        public TrackEventExtraSpawner(float rateMultiplier, Func<MonsterSpawner, Monster> spawnOne)
        {
            _rateMultiplier = rateMultiplier;
            _spawnOne = spawnOne;
        }

        public void Tick(float deltaTime)
        {
            MonsterSpawner spawner = MonsterSpawner.Instance;
            if (spawner == null || _rateMultiplier <= 0f || spawner.BaseSpawnInterval <= 0f)
                return;

            // 한 주기에 SpawnCount마리를 _rateMultiplier배 빠르게 낸다 → 한 마리 간격 = 주기 ÷ (마리 수 × 배율).
            float spawnGap = spawner.BaseSpawnInterval / (spawner.SpawnCount * _rateMultiplier);
            _elapsedTime += deltaTime;

            while (_elapsedTime >= spawnGap)
            {
                _elapsedTime -= spawnGap;

                if (_spawnOne(spawner) == null)
                {
                    _elapsedTime = 0f;
                    return;
                }
            }
        }
    }
}
