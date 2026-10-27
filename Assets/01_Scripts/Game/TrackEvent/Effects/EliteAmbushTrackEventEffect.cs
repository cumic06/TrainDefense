using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game.TrackEvent.Effects
{
    /// <summary>
    /// 엘리트 매복 — 발동 즉시 엘리트 1마리를 스폰하고, 그 개체를 처치하면 엘리트 재화를 magnitude배(기본 2)로 받는다.
    /// 즉발형이라 권장 데이터 duration은 0(역까지) — 처치할 시간을 역 도착까지 준다. (짧게 잡으면 시간 만료 전에 못 잡은 보너스가 사라진다)
    /// 기본 보상은 PermanentUpgradeManager가 MonsterDeadEvent로 평소처럼 지급하고, 이 효과는 그 증가분의 (magnitude - 1)배를 추가로 지급한다.
    /// 어느 개체가 죽었는지는 이벤트에 없으므로 매복 엘리트의 IsDead를 보고 판별하며, 지급은 핸들러 순서에 의존하지 않도록 다음 Tick에 정산한다.
    /// </summary>
    public class EliteAmbushTrackEventEffect : ITrackEventEffect
    {
        private const float DEFAULT_REWARD_MULTIPLIER = 2f;

        private float _rewardMultiplier = DEFAULT_REWARD_MULTIPLIER;
        private Monster _ambushElite;
        private bool _isSubscribed;
        private bool _isKilled;
        private int _coinSnapshot;

        public void Begin(TrackEventData data)
        {
            _rewardMultiplier = data != null && data.Magnitude > 1f ? data.Magnitude : DEFAULT_REWARD_MULTIPLIER;
            _isKilled = false;
            _ambushElite = MonsterSpawner.Instance != null ? MonsterSpawner.Instance.SpawnEliteNow() : null;

            if (_ambushElite == null)
                return;

            _coinSnapshot = _GetEliteCoin();
            GameEventSystem.Subscribe<MonsterDeadEvent>(_OnMonsterDead);
            _isSubscribed = true;
        }

        public void Tick(float deltaTime)
        {
            if (_isKilled)
            {
                _PayBonus();

                return;
            }

            if (_isSubscribed)
                _coinSnapshot = _GetEliteCoin();
        }

        public void End(bool isInterrupted)
        {
            // 처치는 했는데 Tick 전에 끝난 경우를 놓치지 않는다.
            if (_isKilled)
                _PayBonus();

            _Unsubscribe();
            _ambushElite = null;
        }

        private void _OnMonsterDead(MonsterDeadEvent monsterDeadEvent)
        {
            // Monster.OnDead가 _isDead를 켠 뒤 같은 호출 안에서 이벤트를 발행하므로, 죽는 중인 개체가 매복 엘리트일 때만 IsDead가 true다.
            if (_isKilled || _ambushElite == null || !_ambushElite.IsDead || !_ambushElite.IsElite)
                return;

            _isKilled = true;
            _Unsubscribe();
        }

        // 기본 지급(엘리트 1마리분)이 반영된 뒤의 증가분을 기준으로 (magnitude - 1)배를 더한다.
        private void _PayBonus()
        {
            _isKilled = false;

            PermanentUpgradeManager manager = PermanentUpgradeManager.Instance;
            if (manager == null)
                return;

            int baseReward = manager.EliteCoin - _coinSnapshot;
            int bonus = Mathf.RoundToInt(baseReward * (_rewardMultiplier - 1f));
            if (bonus > 0)
                manager.AddEliteCoin(bonus);

            _coinSnapshot = _GetEliteCoin();
        }

        private static int _GetEliteCoin()
        {
            return PermanentUpgradeManager.Instance != null ? PermanentUpgradeManager.Instance.EliteCoin : 0;
        }

        private void _Unsubscribe()
        {
            if (!_isSubscribed)
                return;

            GameEventSystem.Unsubscribe<MonsterDeadEvent>(_OnMonsterDead);
            _isSubscribed = false;
        }
    }
}
