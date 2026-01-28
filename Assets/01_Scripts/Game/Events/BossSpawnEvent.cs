using UnityEngine;

namespace TrainDefense.Game.Events
{
    /// <summary>
    /// 보스 소환 이벤트: 보스를 소환하고 몬스터 스폰 속도를 빠르게 변경
    /// </summary>
    public class BossSpawnEvent
    {
        public string BossMonsterId { get; }
        public float SpawnSpeedMultiplier { get; }

        public BossSpawnEvent(string bossMonsterId, float spawnSpeedMultiplier = 0.7f)
        {
            BossMonsterId = bossMonsterId;
            SpawnSpeedMultiplier = spawnSpeedMultiplier;
            Debug.Log("보스 소환 이벤트 발생");
        }
    }
}
