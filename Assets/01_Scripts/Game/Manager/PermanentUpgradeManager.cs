using System.Collections.Generic;
using UnityEngine;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.Manager;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game
{
    /// <summary>
    /// 영구(메타) 업그레이드 시스템. 엘리트 몬스터 처치 시 재화를 획득하고, 그 재화로 구매한다.
    /// 재화/레벨은 PlayerPrefs에 영구 저장되어 한 판이 끝나도 유지된다.
    /// - TurretStat 업그레이드: GetBonus(StatType)로 조회 → 포탑·레인지 스탯에 가산.
    /// - Passive 업그레이드: GetValue(PermanentUpgradeType)로 조회 → 각 시스템(포탑 수·체력·골드·경험치 등)이 적용.
    /// </summary>
    public class PermanentUpgradeManager : Singleton<PermanentUpgradeManager>
    {
        private const string ELITE_COIN_KEY = "PermanentUpgradeEliteCoin";
        private const string LEVELS_KEY = "PermanentUpgradeLevels";

        [SerializeField]
        [Tooltip("엘리트 몬스터 1마리 처치 시 획득하는 기본 엘리트 코인량")]
        private int _eliteCoinPerElite = 1;

        [SerializeField]
        [Tooltip("역 도달 보상이 걸리기 시작하는 기준 역 번호. 이 번호 이하의 역은 보상 0")]
        private int _stationRewardOffset = 5;

        [SerializeField]
        [Tooltip("역 도달 보상이 1 오르는 데 걸리는 역 수. 2면 두 역마다 +1")]
        private int _stationRewardStep = 2;

        private int _eliteCoin;
        private readonly Dictionary<string, int> _levels = new();

        public int EliteCoin => _eliteCoin;

        /// <summary>이번 판 동안 획득한 엘리트 재화 합계(게임오버 결산 표시용). 게임 진입 시 리셋된다.</summary>
        public int RunEliteCoinEarned { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (Instance == this)
                DontDestroyOnLoad(gameObject);

            _Load();
        }

        private void Start()
        {
            GameEventSystem.Subscribe<MonsterDeadEvent>(_OnMonsterDead);
            GameEventSystem.Subscribe<GameEnterEvent>(_OnGameEnter);
            GameEventSystem.Subscribe<StationPassedEvent>(_OnStationPassed);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<MonsterDeadEvent>(_OnMonsterDead);
            GameEventSystem.Unsubscribe<GameEnterEvent>(_OnGameEnter);
            GameEventSystem.Unsubscribe<StationPassedEvent>(_OnStationPassed);
        }

        private void _OnGameEnter(GameEnterEvent _)
        {
            RunEliteCoinEarned = 0;
        }

        // 엘리트 처치/구매마다 PlayerPrefs.Save()(디스크 flush)를 부르지 않고, 백그라운드 전환·종료 시 한 번에 기록한다.
        private void OnApplicationPause(bool pause)
        {
            if (pause) PlayerPrefs.Save();
        }

        private void OnApplicationQuit()
        {
            PlayerPrefs.Save();
        }

        // 엘리트 몬스터 처치 시 영구 재화 획득.
        private void _OnMonsterDead(MonsterDeadEvent monsterDeadEvent)
        {
            if (!monsterDeadEvent.IsElite) return;

            // 맵별 엘리트 재화 배율 (빠른·약한 몹 맵은 낮게, 느린·강한 몹 맵은 높게 → 맵별 재화 획득률 균등)
            float mapMultiplier = StageManager.Instance != null ? StageManager.Instance.CurrentStageData.EliteRewardMultiplier : 1f;
            int reward = Mathf.Max(1, Mathf.RoundToInt(_eliteCoinPerElite * mapMultiplier));
            AddEliteCoin(reward);
        }

        // 역 도달 시 영구 재화 획득. 사고 영역인 초반 역은 0이고, 뒤로 갈수록 완만하게 오른다.
        private void _OnStationPassed(StationPassedEvent stationPassedEvent)
        {
            AddEliteCoin(GetStationReward(stationPassedEvent.PassedStationCount));
        }

        /// <summary>역 도달 보상: 기준 역 이하면 0, 그 위로는 <c>⌈(역 번호 - 기준) / 단계⌉</c>.</summary>
        public int GetStationReward(int stationNumber)
        {
            if (_stationRewardStep <= 0) return 0;

            int stationsOverOffset = stationNumber - _stationRewardOffset;
            if (stationsOverOffset <= 0) return 0;

            return Mathf.CeilToInt(stationsOverOffset / (float)_stationRewardStep);
        }

        #region EliteCoin
        public void AddEliteCoin(int amount)
        {
            if (amount <= 0) return;
            _eliteCoin += amount;
            RunEliteCoinEarned += amount;
            _SaveEliteCoin();
            GameEventSystem.Publish(new EliteCoinChangedEvent(_eliteCoin));
        }

        public bool SpendEliteCoin(int amount)
        {
            if (amount <= 0 || _eliteCoin < amount) return false;
            _eliteCoin -= amount;
            _SaveEliteCoin();
            GameEventSystem.Publish(new EliteCoinChangedEvent(_eliteCoin));
            return true;
        }

        /// <summary>현재 재화로 구매 가능한(최대 레벨이 아니고 비용을 충족하는) 영구 업그레이드가 하나라도 있는지. (로비 버튼 레드닷 판정용)</summary>
        public bool HasAffordableUpgrade()
        {
            if (DatabaseManager.Instance == null) return false;

            foreach (var data in DatabaseManager.Instance.GetPermanentUpgradeDatas())
            {
                if (data == null) continue;
                if (IsAffordable(data.Id)) return true;
            }

            return false;
        }

        /// <summary>해당 영구 업그레이드를 현재 재화로 구매 가능한지(최대 레벨이 아니고 비용 충족). (슬롯 레드닷 판정용)</summary>
        public bool IsAffordable(string upgradeId)
        {
            if (DatabaseManager.Instance == null) return false;

            var data = DatabaseManager.Instance.GetPermanentUpgradeData(upgradeId);
            if (data == null) return false;
            if (IsMaxLevel(upgradeId)) return false;

            return data.GetCostAtLevel(GetLevel(upgradeId)) <= _eliteCoin;
        }
        #endregion

        #region Level / Purchase
        public int GetLevel(string upgradeId)
            => _levels.TryGetValue(upgradeId, out int level) ? level : 0;

        public bool IsMaxLevel(string upgradeId)
        {
            var data = DatabaseManager.Instance.GetPermanentUpgradeData(upgradeId);
            if (data == null || data.MaxUpgradeCount <= 0) return false;
            return GetLevel(upgradeId) >= data.MaxUpgradeCount;
        }

        /// <summary>현재 재화로 해당 업그레이드를 1레벨 구매 시도. 성공 시 true.</summary>
        public bool TryPurchase(string upgradeId)
        {
            var data = DatabaseManager.Instance.GetPermanentUpgradeData(upgradeId);
            if (data == null) return false;
            if (IsMaxLevel(upgradeId)) return false;

            int cost = data.GetCostAtLevel(GetLevel(upgradeId));
            if (!SpendEliteCoin(cost)) return false;

            _levels[upgradeId] = GetLevel(upgradeId) + 1;
            _SaveLevels();
            // 구매는 의식적 행동이라 강제 종료에도 잃지 않도록 즉시 디스크 flush (재화 차감 + 레벨을 함께 확정)
            PlayerPrefs.Save();
            _InvalidateCache();
            GameEventSystem.Publish<PermanentUpgradePurchasedEvent>(new PermanentUpgradePurchasedEvent(upgradeId, cost, GetLevel(upgradeId)));

            return true;
        }
        #endregion

        #region Bonus / Value
        // 레벨은 구매 시에만 바뀌므로 누적값을 캐시하고, 구매/초기화 때만 재계산한다 (매 호출 순회·할당 제거).
        private Dictionary<StatType, float> _bonusCache;
        private Dictionary<PermanentUpgradeType, float> _valueCache;

        /// <summary>TurretStat 카테고리 업그레이드들의 해당 StatType 누적 보너스 (레벨 × 레벨당 증가량). 포탑·레인지 스탯에 가산용.</summary>
        public float GetBonus(StatType statType)
        {
            if (_bonusCache == null) _RebuildCache();
            return _bonusCache.TryGetValue(statType, out float value) ? value : 0f;
        }

        /// <summary>Passive 카테고리 업그레이드들의 해당 효과 누적값 (레벨 × 레벨당 값). 각 시스템이 조회해 적용한다.</summary>
        public float GetValue(PermanentUpgradeType type)
        {
            if (_valueCache == null) _RebuildCache();
            return _valueCache.TryGetValue(type, out float value) ? value : 0f;
        }

        /// <summary>보유 레벨 기준으로 TurretStat/Passive 누적값을 한 번에 계산해 캐시한다.</summary>
        private void _RebuildCache()
        {
            _bonusCache = new Dictionary<StatType, float>();
            _valueCache = new Dictionary<PermanentUpgradeType, float>();

            foreach (var data in DatabaseManager.Instance.GetPermanentUpgradeDatas())
            {
                if (data == null) continue;
                int level = GetLevel(data.Id);
                if (level <= 0) continue;

                if (data.Category == PermanentUpgradeCategory.TurretStat)
                {
                    if (data.Stats == null) continue;
                    foreach (var stat in data.Stats)
                    {
                        if (stat == null) continue;
                        _bonusCache.TryGetValue(stat.Type, out float cur);
                        _bonusCache[stat.Type] = cur + stat.Value * level;
                    }
                }
                else
                {
                    _valueCache.TryGetValue(data.PassiveType, out float cur);
                    _valueCache[data.PassiveType] = cur + data.PassiveValuePerLevel * level;
                }
            }
        }

        // 레벨이 바뀌면(구매/초기화) 캐시를 버려 다음 조회 때 재계산하게 한다.
        private void _InvalidateCache()
        {
            _bonusCache = null;
            _valueCache = null;
        }
        #endregion

        #region Save / Load
        private void _Load()
        {
            _eliteCoin = PlayerPrefs.GetInt(ELITE_COIN_KEY, 0);

            _levels.Clear();
            // 형식: "id:level,id:level"
            string saved = PlayerPrefs.GetString(LEVELS_KEY, "");
            if (!string.IsNullOrEmpty(saved))
            {
                foreach (string entry in saved.Split(','))
                {
                    if (string.IsNullOrEmpty(entry)) continue;
                    string[] pair = entry.Split(':');
                    if (pair.Length == 2 && int.TryParse(pair[1], out int level))
                        _levels[pair[0]] = level;
                }
            }
        }

        private void _SaveEliteCoin()
        {
            // 디스크 flush(PlayerPrefs.Save)는 OnApplicationPause/Quit에서 일괄 — 엘리트 처치마다 디스크 쓰기 방지
            PlayerPrefs.SetInt(ELITE_COIN_KEY, _eliteCoin);
        }

        private void _SaveLevels()
        {
            var entries = new List<string>();
            foreach (var kv in _levels)
                entries.Add($"{kv.Key}:{kv.Value}");
            PlayerPrefs.SetString(LEVELS_KEY, string.Join(",", entries));
        }

        [Sirenix.OdinInspector.Button("테스트 엘리트 코인 +100")]
        private void AddTestEliteCoin()
        {
            AddEliteCoin(100);
            Debug.Log($"[PermanentUpgradeManager] 테스트 엘리트 코인 +100 (현재 {_eliteCoin})");
        }

        [Sirenix.OdinInspector.Button("영구 업그레이드/엘리트 코인 초기화")]
        private void ResetAll()
        {
            _eliteCoin = 0;
            _levels.Clear();
            _InvalidateCache();
            PlayerPrefs.DeleteKey(ELITE_COIN_KEY);
            PlayerPrefs.DeleteKey(LEVELS_KEY);
            PlayerPrefs.Save();
            Debug.Log("[PermanentUpgradeManager] 영구 업그레이드 데이터가 초기화되었습니다.");
        }
        #endregion
    }
}
