using System.Collections.Generic;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.Manager;
using UnityEngine;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 보급 투하 — 반짝이 1개가 천천히 떠가고, 터치하면 골드를 준다.
    /// 지급 골드 = TrackEventGoldReward(BASE_GOLD × magnitude).
    /// 데이터 duration(0 이하면 DEFAULT_LIFETIME) 뒤 미획득 반짝이는 사라지고, End(역 도착 포함)에서 모두 정리한다.
    /// </summary>
    public class SupplyDropTrackEventEffect : ITrackEventEffect
    {
        private const float DEFAULT_LIFETIME = 8f;
        private const float BASE_GOLD = 30f;
        private const float DEFAULT_MAGNITUDE = 1f;
        private const float MOVE_SPEED = 2.6f;
        // 오른쪽으로 떠가도 화면 안에 남도록 왼쪽 절반에서 시작한다. 세로는 기차 줄, 위아래 HUD, 이벤트 배너(화면 높이 0.68~0.78)를 피한다.
        private const float VIEWPORT_X_MIN = 0.12f;
        private const float VIEWPORT_X_MAX = 0.55f;
        private const float UPPER_VIEWPORT_Y_MIN = 0.58f;
        private const float UPPER_VIEWPORT_Y_MAX = 0.64f;
        private const float LOWER_VIEWPORT_Y_MIN = 0.22f;
        private const float LOWER_VIEWPORT_Y_MAX = 0.38f;

        private readonly List<SupplyCrate> _crates = new List<SupplyCrate>();
        private float _magnitude = DEFAULT_MAGNITUDE;
        private float _remainingTime;
        private bool _isActive;

        public void Begin(TrackEventData data)
        {
            _magnitude = data != null && data.Magnitude > 0f ? data.Magnitude : DEFAULT_MAGNITUDE;
            _remainingTime = data != null && data.Duration > 0f ? data.Duration : DEFAULT_LIFETIME;
            _isActive = true;

            _SpawnCrate();
        }

        public void Tick(float deltaTime)
        {
            if (!_isActive)
            {
                return;
            }

            _remainingTime -= deltaTime;

            if (_remainingTime <= 0f)
            {
                _ClearCrates();
            }
        }

        public void End(bool isInterrupted)
        {
            _isActive = false;
            _ClearCrates();
        }

        private void _SpawnCrate()
        {
            SupplyCrate crate = SupplyCrate.Create(_PickSpawnPosition(), Vector3.right * MOVE_SPEED, _remainingTime);
            crate.Collected += _OnCrateCollected;
            _crates.Add(crate);
        }

        private Vector3 _PickSpawnPosition()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return Vector3.zero;
            }

            bool isUpper = Random.value < 0.5f;
            float viewportY = isUpper
                ? Random.Range(UPPER_VIEWPORT_Y_MIN, UPPER_VIEWPORT_Y_MAX)
                : Random.Range(LOWER_VIEWPORT_Y_MIN, LOWER_VIEWPORT_Y_MAX);
            float depth = Mathf.Abs(cam.transform.position.z);

            Vector3 world = cam.ViewportToWorldPoint(new Vector3(Random.Range(VIEWPORT_X_MIN, VIEWPORT_X_MAX), viewportY, depth));
            world.z = 0f;

            return world;
        }

        // 지운 뒤 터지는 연출은 반짝이가 스스로 하고 사라진다.
        private void _OnCrateCollected(SupplyCrate crate)
        {
            crate.Collected -= _OnCrateCollected;
            _crates.Remove(crate);
            _GrantGold();
        }

        private void _GrantGold()
        {
            var userData = UserDataManager.Instance;
            if (userData == null)
            {
                return;
            }

            int amount = TrackEventGoldReward.Get(BASE_GOLD * _magnitude);

            int before = userData.Coin;
            GameEventSystem.Publish(new ChangeCoinUIEvent(before, before + amount));
        }

        private void _ClearCrates()
        {
            for (int i = 0; i < _crates.Count; i++)
            {
                SupplyCrate crate = _crates[i];
                if (crate == null)
                {
                    continue;
                }

                crate.Collected -= _OnCrateCollected;
                Object.Destroy(crate.gameObject);
            }

            _crates.Clear();
        }
    }
}
