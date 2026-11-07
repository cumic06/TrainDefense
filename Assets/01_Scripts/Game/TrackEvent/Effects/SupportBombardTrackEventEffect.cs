using System.Collections.Generic;
using TrainDefense.Game.Datas;
using UnityEngine;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 지원 폭격 — 지속 시간(데이터 duration) 동안 SHELL_INTERVAL초마다 포탄 1발을 화면 안 몬스터 하나에 떨어뜨린다
    /// (화면에 몬스터가 없으면 화면 무작위 위치). 발동하자마자 첫 발이 떨어진다.
    /// 포탄은 몬스터만 때리고 기차는 맞지 않는다(PlayerBombardManager와 동일). 피해량은 플레이어 포격 기준에 magnitude를 곱한다(0 이하면 1배).
    /// </summary>
    public class SupportBombardTrackEventEffect : ITrackEventEffect
    {
        private const float SHELL_INTERVAL = 1.5f;
        // 화면 가장자리에 걸쳐 잘리지 않도록 뷰포트 안쪽으로 들이는 여백 비율.
        private const float VIEWPORT_MARGIN = 0.1f;

        private static readonly List<Vector2> _targetPositions = new();

        private float _damageMultiplier = 1f;
        private float _timeUntilNextShell;

        public void Begin(TrackEventData data)
        {
            _damageMultiplier = data != null && data.Magnitude > 0f ? data.Magnitude : 1f;
            _timeUntilNextShell = 0f;
        }

        public void Tick(float deltaTime)
        {
            _timeUntilNextShell -= deltaTime;

            if (_timeUntilNextShell > 0f)
                return;

            _timeUntilNextShell += SHELL_INTERVAL;
            _FireOneShell();
        }

        public void End(bool isInterrupted)
        {
        }

        private void _FireOneShell()
        {
            PlayerBombardManager bombard = PlayerBombardManager.Instance;
            Camera mainCamera = Camera.main;
            if (bombard == null || mainCamera == null)
                return;

            bombard.FireAt(_PickTarget(mainCamera), _damageMultiplier);
        }

        // 화면 안(여백 제외)의 살아 있는 몬스터 중 하나. 없으면 화면 무작위 위치.
        private static Vector2 _PickTarget(Camera mainCamera)
        {
            _CollectVisibleMonsterPositions(mainCamera);

            if (_targetPositions.Count > 0)
            {
                Vector2 target = _targetPositions[Random.Range(0, _targetPositions.Count)];
                _targetPositions.Clear();
                return target;
            }

            Vector3 viewport = new Vector3(
                Random.Range(VIEWPORT_MARGIN, 1f - VIEWPORT_MARGIN),
                Random.Range(VIEWPORT_MARGIN, 1f - VIEWPORT_MARGIN),
                Mathf.Abs(mainCamera.transform.position.z));
            Vector3 world = mainCamera.ViewportToWorldPoint(viewport);

            return new Vector2(world.x, world.y);
        }

        private static void _CollectVisibleMonsterPositions(Camera mainCamera)
        {
            _targetPositions.Clear();

            MonsterSpawner spawner = MonsterSpawner.Instance;
            if (spawner == null)
                return;

            foreach (Monster monster in spawner.SpawnedMonsters)
            {
                if (monster == null || monster.IsDead)
                    continue;

                Vector3 viewport = mainCamera.WorldToViewportPoint(monster.transform.position);
                if (viewport.x < VIEWPORT_MARGIN || viewport.x > 1f - VIEWPORT_MARGIN || viewport.y < VIEWPORT_MARGIN || viewport.y > 1f - VIEWPORT_MARGIN)
                    continue;

                _targetPositions.Add(monster.transform.position);
            }
        }
    }
}
