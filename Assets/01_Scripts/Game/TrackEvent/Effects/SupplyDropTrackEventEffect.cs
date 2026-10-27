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
    /// 보급 투하 — 기차 주변에 보급 상자 1~3개를 놓고, 터치하면 골드를 준다.
    /// 지급 골드 = BASE_GOLD × StageManager.GetGoldScale() × magnitude (상자 하나당).
    /// 데이터 duration(0 이하면 DEFAULT_LIFETIME) 뒤 미획득 상자는 사라지고, End(역 도착 포함)에서 남은 상자를 모두 정리한다.
    /// 상자는 런타임 생성(SupplyCrate.Create). 프리팹으로 바꾸려면 _SpawnCrate의 Create 호출만 교체한다.
    /// </summary>
    public class SupplyDropTrackEventEffect : ITrackEventEffect
    {
        private const int MIN_CRATE_COUNT = 1;
        private const int MAX_CRATE_COUNT = 3;
        private const float DEFAULT_LIFETIME = 8f;
        private const float BASE_GOLD = 30f;
        private const float DEFAULT_MAGNITUDE = 1f;
        private const float VIEWPORT_MIN = 0.15f;
        private const float VIEWPORT_MAX = 0.85f;
        private const float TRAIN_SPREAD_X = 6f;
        private const float TRAIN_SPREAD_Y = 3f;

        private readonly List<SupplyCrate> _crates = new List<SupplyCrate>();
        private float _magnitude = DEFAULT_MAGNITUDE;
        private float _remainingTime;
        private bool _isActive;

        public void Begin(TrackEventData data)
        {
            _magnitude = data != null && data.Magnitude > 0f ? data.Magnitude : DEFAULT_MAGNITUDE;
            _remainingTime = data != null && data.Duration > 0f ? data.Duration : DEFAULT_LIFETIME;
            _isActive = true;

            int count = Random.Range(MIN_CRATE_COUNT, MAX_CRATE_COUNT + 1);
            for (int i = 0; i < count; i++)
            {
                _SpawnCrate();
            }
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
            SupplyCrate crate = SupplyCrate.Create(_PickSpawnPosition());
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

            Vector3 center = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
            MainTrain train = Object.FindFirstObjectByType<MainTrain>();
            if (train != null)
            {
                center = train.transform.position;
            }

            Vector3 candidate = center + new Vector3(
                Random.Range(-TRAIN_SPREAD_X, TRAIN_SPREAD_X),
                Random.Range(-TRAIN_SPREAD_Y, TRAIN_SPREAD_Y),
                0f);

            // 화면 안(가장자리 여백 유지)으로 클램프
            Vector3 viewport = cam.WorldToViewportPoint(candidate);
            viewport.x = Mathf.Clamp(viewport.x, VIEWPORT_MIN, VIEWPORT_MAX);
            viewport.y = Mathf.Clamp(viewport.y, VIEWPORT_MIN, VIEWPORT_MAX);

            Vector3 world = cam.ViewportToWorldPoint(viewport);
            world.z = 0f;

            return world;
        }

        private void _OnCrateCollected(SupplyCrate crate)
        {
            _crates.Remove(crate);
            _GrantGold();

            if (crate != null)
            {
                Object.Destroy(crate.gameObject);
            }
        }

        private void _GrantGold()
        {
            var userData = UserDataManager.Instance;
            if (userData == null)
            {
                return;
            }

            float goldScale = StageManager.Instance != null ? StageManager.Instance.GetGoldScale() : 1f;
            int amount = Mathf.Max(1, Mathf.RoundToInt(BASE_GOLD * goldScale * _magnitude));

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
