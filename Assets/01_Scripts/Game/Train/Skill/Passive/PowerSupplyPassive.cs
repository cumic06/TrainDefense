using System.Collections.Generic;

namespace TrainDefense.Game
{
    /// <summary>
    /// 편성에서 바로 앞·뒤 칸 포탑의 공격 속도를 올린다. (전기 전력 공급) 칸 교환·추가·사망을 따라가도록 주기적으로 이웃을 다시 찾는다.
    /// DSL: "PowerSupply:percent"
    /// 예: "PowerSupply:15" → 양옆 포탑 공격 속도 +15%
    /// </summary>
    public class PowerSupplyPassive : TrainPassiveSkill
    {
        // 이웃을 다시 찾는 간격(초). 칸 교환 직후 잠깐 늦게 반영돼도 체감되지 않는 정도.
        private const float NEIGHBOR_REFRESH_INTERVAL = 0.25f;

        private float _percent;
        private float _refreshElapsedTime = NEIGHBOR_REFRESH_INTERVAL;
        private readonly List<Train> _poweredTrains = new();
        private readonly List<Train> _neighbors = new();

        public static PowerSupplyPassive From(string[] parts)
        {
            if (parts.Length < 2) return null;
            if (!TryParseFloat(parts[1], out float percent) || percent <= 0f) return null;

            return new PowerSupplyPassive { _percent = percent };
        }

        public override void Unsubscribe()
        {
            _ReleaseAll();
        }

        public override void Tick(float deltaTime)
        {
            if (Owner == null) return;

            if (Owner.IsDead)
            {
                _ReleaseAll();

                return;
            }

            _refreshElapsedTime += deltaTime;
            if (_refreshElapsedTime < NEIGHBOR_REFRESH_INTERVAL) return;

            _refreshElapsedTime = 0f;
            _RefreshNeighbors();
        }

        private void _RefreshNeighbors()
        {
            _neighbors.Clear();

            var mainTrain = TrainManager.Instance != null ? TrainManager.Instance.MainTrain : null;
            if (mainTrain != null)
            {
                List<Train> trains = mainTrain.CurrentTrains;
                int index = trains.IndexOf(Owner);
                if (index >= 0)
                {
                    _AddNeighbor(trains, index - 1);
                    _AddNeighbor(trains, index + 1);
                }
            }

            // 이웃에서 빠진 칸은 버프 해제, 새 이웃은 버프 부여.
            for (int i = _poweredTrains.Count - 1; i >= 0; i--)
            {
                Train powered = _poweredTrains[i];
                if (_neighbors.Contains(powered)) continue;

                if (powered != null)
                    powered.RemoveAttackSpeedBonus(Owner);

                _poweredTrains.RemoveAt(i);
            }

            foreach (Train neighbor in _neighbors)
            {
                neighbor.SetAttackSpeedBonus(Owner, _percent);

                if (!_poweredTrains.Contains(neighbor))
                    _poweredTrains.Add(neighbor);
            }
        }

        private void _AddNeighbor(List<Train> trains, int index)
        {
            if (index < 0 || index >= trains.Count) return;

            Train train = trains[index];
            if (train == null || train == Owner || train.IsMainTrain) return;

            _neighbors.Add(train);
        }

        private void _ReleaseAll()
        {
            foreach (Train powered in _poweredTrains)
            {
                if (powered != null)
                    powered.RemoveAttackSpeedBonus(Owner);
            }

            _poweredTrains.Clear();
            _refreshElapsedTime = NEIGHBOR_REFRESH_INTERVAL;
        }
    }
}
