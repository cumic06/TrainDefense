using TrainDefense.Game.Datas;
using UnityEngine;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 달아오른 엘리트(폭염 갈림길) — 역 도착까지 엘리트가 평소의 magnitude배로 나오도록 정규 엘리트 위에 더 소환하고,
    /// 그동안 포탑 공격 속도를 subMagnitude배로 올린다. 추가 엘리트 보상은 평소 엘리트와 같다(배율 없음).
    /// 정규 엘리트는 소환 eliteSpawnCycle번에 1마리 꼴(MonsterSpawner._ScheduleEliteSpawns)이므로 추가분 간격은
    /// eliteSpawnCycle × 평소 소환 주기 ÷ (magnitude − 1). 정규 엘리트 일정·예산은 건드리지 않아 End에서 되돌릴 상태가 없다.
    /// </summary>
    public class HeatedEliteTrackEventEffect : ITrackEventEffect
    {
        private const float DEFAULT_ELITE_MULTIPLIER = 2f;
        // 첫 추가 엘리트까지 간격의 절반만 기다린다 — 남은 구간 기대치가 0.5마리 이상이면 1마리가 나오는 반올림이 된다.
        private const float FIRST_GAP_RATIO = 0.5f;

        private float _eliteGap;
        private float _elapsedTime;
        private float _appliedAttackSpeedMultiplier = 1f;

        public static float GetEliteMultiplier(TrackEventData data)
        {
            return data != null && data.Magnitude > 1f ? data.Magnitude : DEFAULT_ELITE_MULTIPLIER;
        }

        public void Begin(TrackEventData data)
        {
            _eliteGap = _CalculateEliteGap(GetEliteMultiplier(data));
            _elapsedTime = _eliteGap * FIRST_GAP_RATIO;

            _appliedAttackSpeedMultiplier = data != null && data.SubMagnitude > 0f ? data.SubMagnitude : 1f;
            TrackEventModifiers.Multiply(TrackEventModifierType.TurretAttackSpeed, _appliedAttackSpeedMultiplier);
        }

        public void Tick(float deltaTime)
        {
            MonsterSpawner spawner = MonsterSpawner.Instance;

            if (spawner == null || _eliteGap <= 0f)
                return;

            _elapsedTime += deltaTime;

            while (_elapsedTime >= _eliteGap)
            {
                _elapsedTime -= _eliteGap;

                // 적격 몬스터가 없는 구간(맵 전환 직후 등)이면 이번 회차는 건너뛴다.
                if (spawner.SpawnEliteNow() == null)
                    return;
            }
        }

        public void End(bool isInterrupted)
        {
            _eliteGap = 0f;
            TrackEventModifiers.Divide(TrackEventModifierType.TurretAttackSpeed, _appliedAttackSpeedMultiplier);
        }

        private static float _CalculateEliteGap(float eliteMultiplier)
        {
            MonsterSpawner spawner = MonsterSpawner.Instance;
            EliteData eliteData = DatabaseManager.Instance != null ? DatabaseManager.Instance.GetEliteData() : null;

            if (spawner == null || eliteData == null || spawner.BaseSpawnInterval <= 0f || eliteMultiplier <= 1f)
                return 0f;

            return Mathf.Max(1, eliteData.eliteSpawnCycle) * spawner.BaseSpawnInterval / (eliteMultiplier - 1f);
        }
    }
}
