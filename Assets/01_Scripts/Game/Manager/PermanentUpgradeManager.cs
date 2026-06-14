using System.Collections.Generic;
using UnityEngine;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
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
        private const string CURRENCY_KEY = "PermanentUpgradeCurrency";
        private const string LEVELS_KEY = "PermanentUpgradeLevels";

        [SerializeField]
        [Tooltip("엘리트 몬스터 1마리 처치 시 획득하는 기본 영구 재화량")]
        private int _currencyPerElite = 1;

        private int _currency;
        private readonly Dictionary<string, int> _levels = new();

        public int Currency => _currency;

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
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<MonsterDeadEvent>(_OnMonsterDead);
        }

        // 엘리트 몬스터 처치 시 영구 재화 획득. EliteRewardRate(%) 패시브가 획득량을 증가시킨다.
        private void _OnMonsterDead(MonsterDeadEvent monsterDeadEvent)
        {
            if (!monsterDeadEvent.IsElite) return;

            float rewardRate = GetValue(PermanentUpgradeType.EliteRewardRate);
            int reward = Mathf.Max(1, Mathf.RoundToInt(_currencyPerElite * (1f + rewardRate / 100f)));
            AddCurrency(reward);
        }

        #region Currency
        public void AddCurrency(int amount)
        {
            if (amount <= 0) return;
            _currency += amount;
            _SaveCurrency();
            // TODO: 재화 변경 UI 이벤트 발행
        }

        public bool SpendCurrency(int amount)
        {
            if (amount <= 0 || _currency < amount) return false;
            _currency -= amount;
            _SaveCurrency();
            return true;
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
            if (!SpendCurrency(cost)) return false;

            _levels[upgradeId] = GetLevel(upgradeId) + 1;
            _SaveLevels();
            // TODO: 구매 완료 UI 이벤트 발행
            return true;
        }
        #endregion

        #region Bonus / Value
        /// <summary>TurretStat 카테고리 업그레이드들의 해당 StatType 누적 보너스 (레벨 × 레벨당 증가량). 포탑·레인지 스탯에 가산용.</summary>
        public float GetBonus(StatType statType)
        {
            float total = 0f;
            foreach (var data in DatabaseManager.Instance.GetPermanentUpgradeDatas())
            {
                if (data == null || data.Category != PermanentUpgradeCategory.TurretStat) continue;
                int level = GetLevel(data.Id);
                if (level <= 0 || data.Stats == null) continue;

                foreach (var stat in data.Stats)
                {
                    if (stat != null && stat.Type == statType)
                        total += stat.Value * level;
                }
            }
            return total;
        }

        /// <summary>Passive 카테고리 업그레이드들의 해당 효과 누적값 (레벨 × 레벨당 값). 각 시스템이 조회해 적용한다.</summary>
        public float GetValue(PermanentUpgradeType type)
        {
            float total = 0f;
            foreach (var data in DatabaseManager.Instance.GetPermanentUpgradeDatas())
            {
                if (data == null || data.Category != PermanentUpgradeCategory.Passive) continue;
                if (data.PassiveType != type) continue;
                int level = GetLevel(data.Id);
                if (level > 0)
                    total += data.PassiveValuePerLevel * level;
            }
            return total;
        }
        #endregion

        #region Save / Load
        private void _Load()
        {
            _currency = PlayerPrefs.GetInt(CURRENCY_KEY, 0);

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

        private void _SaveCurrency()
        {
            PlayerPrefs.SetInt(CURRENCY_KEY, _currency);
            PlayerPrefs.Save();
        }

        private void _SaveLevels()
        {
            var entries = new List<string>();
            foreach (var kv in _levels)
                entries.Add($"{kv.Key}:{kv.Value}");
            PlayerPrefs.SetString(LEVELS_KEY, string.Join(",", entries));
            PlayerPrefs.Save();
        }

        [Sirenix.OdinInspector.Button("영구 업그레이드/재화 초기화")]
        private void ResetAll()
        {
            _currency = 0;
            _levels.Clear();
            PlayerPrefs.DeleteKey(CURRENCY_KEY);
            PlayerPrefs.DeleteKey(LEVELS_KEY);
            PlayerPrefs.Save();
            Debug.Log("[PermanentUpgradeManager] 영구 업그레이드 데이터가 초기화되었습니다.");
        }
        #endregion
    }
}
