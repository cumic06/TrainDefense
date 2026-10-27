using UnityEngine;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Manager;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 황금 몬스터 — 발동 시 몬스터 1마리를 화면 안에 스폰해 기차 반대 방향으로 도망시킨다(Monster.BeginEscape).
    /// 화면 밖으로 나가면 보상 없이 사라지고, 처치하면 Magnitude 배의 골드를 준다.
    /// End(역 도착·게임 종료)에서 아직 도망 중이면 보상 없이 정리한다. MonsterSpawner 리스트에는 등록하지 않는다.
    /// </summary>
    public class GoldenMonsterTrackEventEffect : ITrackEventEffect
    {
        private const float DEFAULT_GOLD_MULTIPLIER = 10f;
        private const float SPAWN_VIEWPORT_MIN = 0.25f;
        private const float SPAWN_VIEWPORT_MAX = 0.75f;

        private Monster _goldenMonster;

        public void Begin(TrackEventData data)
        {
            _goldenMonster = null;

            if (DatabaseManager.Instance == null || ResourceManager.Instance == null)
                return;

            MonsterData monsterData = _PickMonsterData();
            if (monsterData == null)
                return;

            Vector3 spawnPos = _RandomScreenPos();
            Monster monster = ResourceManager.Instance.Spawn(monsterData.Prefab, spawnPos).GetComponent<Monster>();
            if (monster == null)
                return;

            monster.Initialize(monsterData);

            float goldMultiplier = data != null && data.Magnitude > 0f ? data.Magnitude : DEFAULT_GOLD_MULTIPLIER;
            monster.BeginEscape(_EscapeDirection(spawnPos), goldMultiplier);
            _goldenMonster = monster;
        }

        public void Tick(float deltaTime)
        {
        }

        public void End(bool isInterrupted)
        {
            if (_goldenMonster != null && _goldenMonster.IsEscaping)
            {
                _goldenMonster.CancelEscape();
            }

            _goldenMonster = null;
        }

        private MonsterData _PickMonsterData()
        {
            MonsterData[] datas = DatabaseManager.Instance.GetMonsterDatas();
            if (datas == null || datas.Length == 0)
                return null;

            int start = Random.Range(0, datas.Length);
            for (int offset = 0; offset < datas.Length; offset++)
            {
                MonsterData candidate = datas[(start + offset) % datas.Length];
                if (candidate != null && candidate.Prefab != null)
                    return candidate;
            }

            return null;
        }

        private Vector3 _RandomScreenPos()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return Vector3.zero;

            float depth = Mathf.Abs(mainCamera.transform.position.z);
            Vector3 viewport = new Vector3(
                Random.Range(SPAWN_VIEWPORT_MIN, SPAWN_VIEWPORT_MAX),
                Random.Range(SPAWN_VIEWPORT_MIN, SPAWN_VIEWPORT_MAX),
                depth);
            Vector3 worldPos = mainCamera.ViewportToWorldPoint(viewport);
            worldPos.z = 0f;

            return worldPos;
        }

        // 가장 가까운 기차에서 멀어지는 방향. 기차를 못 찾으면 무작위 방향.
        private Vector3 _EscapeDirection(Vector3 spawnPos)
        {
            if (TrainManager.Instance != null)
            {
                Train nearTrain = TrainManager.Instance.GetNearTrain(spawnPos);
                if (nearTrain != null)
                {
                    Vector3 away = spawnPos - nearTrain.transform.position;
                    away.z = 0f;
                    if (away.sqrMagnitude > 0.0001f)
                        return away.normalized;
                }
            }

            return Random.insideUnitCircle.normalized;
        }
    }
}
