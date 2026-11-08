using System.Collections.Generic;
using UnityEngine;
using Cumic;
using Cumic.Events;
using Cumic.Sequence;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.Manager;
using TrainDefense.Game.RunSave;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game
{
    /// <summary>
    /// 엘리트 코인 지갑. 처치·역 도달로 적립하고 스킬 트리가 쓴다. 옛 영구 강화 구매는 없어졌고, 남은 레벨은 로드 때 환급하고 지운다.
    /// 재화/레벨은 PlayerPrefs에 영구 저장되어 한 판이 끝나도 유지된다.
    /// GetBonus·GetValue는 레벨이 없어 항상 0이다.
    /// </summary>
    public class PermanentUpgradeManager : Singleton<PermanentUpgradeManager>
    {
        private const string ELITE_COIN_KEY = "PermanentUpgradeEliteCoin";
        private const string LEVELS_KEY = "PermanentUpgradeLevels";
        // 이번 판에 번 합계의 디스크 사본. 런 세이브(EliteCoinSection)는 역 도착 때만 쓰이므로, 그 뒤 앱이 꺼졌을 때
        // "세이브 이후에 번 만큼"을 이어하기에서 되돌리려면 지갑과 같은 시점에 기록된 값이 필요하다.
        private const string RUN_ELITE_COIN_EARNED_KEY = "PermanentUpgradeRunEliteCoinEarned";
        // 옛 영구 강화 환급 상한. 11-03 스킬 트리 총비용과 같은 값으로 정했고, 트리 비용이 바뀌어도 따라가지 않는다.
        private const int LEGACY_REFUND_CAP = 4782;

        // 스킬 트리 이전 영구 강화 비용표 (develop 1.0.4 DB 값). 지금 DB는 같은 id에 다른 값이 들어 있어 환급 계산에 쓸 수 없다
        private static readonly (string id, int baseCost, float growthRate, int maxLevel)[] LegacyUpgradeCosts =
        {
            ("120001", 10, 1.5f, 10),
            ("120002", 10, 1.5f, 10),
            ("120003", 10, 1.5f, 10),
            ("120004", 30, 2f, 5),
            ("120005", 100, 5f, 3),
            ("120006", 150, 3.5f, 2),
        };

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

            // 로비 씬에 놓인 매니저라 로비에 다시 올 때마다 하나 더 생긴다. 중복분이 살아 이벤트를 받으면
            // 오래된 메모리 값으로 재화를 덮어쓸 수 있으므로 여기서 지운다(씬 쪽 DDOL 플래그는 꺼져 있어 베이스가 안 지운다).
            if (Instance != this)
            {
                Destroy(gameObject);

                return;
            }

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

        private void _OnGameEnter(GameEnterEvent gameEnterEvent)
        {
            RunEliteCoinEarned = 0;

            // 새 판이면 디스크 사본도 0으로. 이어하기(복원 대기 중)는 사본을 남겨 둬야 세이브 이후 분을 계산할 수 있다.
            bool isContinue = RunSaveManager.Instance != null && RunSaveManager.Instance.IsContinuePending;

            if (!gameEnterEvent.IsLobby && !isContinue)
                PlayerPrefs.SetInt(RUN_ELITE_COIN_EARNED_KEY, 0);
        }

        /// <summary>
        /// 이어하기 복원(EliteCoinSection) — 판을 세이브 시점으로 되돌린다.
        /// 세이브 이후에 번 재화(디스크 사본 − 세이브 값)는 지갑에서 빼고, 이번 판 합계는 세이브 값으로 맞춘다.
        /// 로비에서 쓴 재화는 건드리지 않는다. 세이브 뒤 로비에서 재화를 초기화했을 수 있으므로 지갑 잔액을 넘지 않게 맞춘다.
        /// </summary>
        public void RestoreRunState(int runEliteCoinEarned)
        {
            int savedRunEarned = Mathf.Max(0, runEliteCoinEarned);
            int earnedAfterSave = PlayerPrefs.GetInt(RUN_ELITE_COIN_EARNED_KEY, savedRunEarned) - savedRunEarned;

            if (earnedAfterSave > 0)
            {
                _eliteCoin = Mathf.Max(0, _eliteCoin - earnedAfterSave);
                _SaveEliteCoin();
                GameEventSystem.Publish(new EliteCoinChangedEvent(_eliteCoin));
            }

            RunEliteCoinEarned = Mathf.Min(savedRunEarned, _eliteCoin);
            _SaveRunEliteCoinEarned();
        }

        // 판 안에서 번 재화만 디스크 사본에 적는다(스킬 트리 환급 같은 로비 지급은 이번 판 합계와 무관).
        private void _SaveRunEliteCoinEarned()
        {
            if (InGameSequence.Instance == null)
                return;

            PlayerPrefs.SetInt(RUN_ELITE_COIN_EARNED_KEY, RunEliteCoinEarned);
        }

        // 엘리트 처치마다 PlayerPrefs.Save()(디스크 flush)를 부르지 않고, 백그라운드 전환·종료 시 한 번에 기록한다.
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
            _TryGrantKillCoin();

            if (!monsterDeadEvent.IsElite) return;

            // 맵별 엘리트 재화 배율 (빠른·약한 몹 맵은 낮게, 느린·강한 몹 맵은 높게 → 맵별 재화 획득률 균등)
            float mapMultiplier = StageManager.Instance != null ? StageManager.Instance.CurrentStageData.EliteRewardMultiplier : 1f;
            int reward = Mathf.Max(1, Mathf.RoundToInt(_eliteCoinPerElite * mapMultiplier));
            // 스킬 트리 '엘리트 코인' 칸의 추가분은 맵·난이도 배율과 무관하게 정확히 +N개다.
            AddEliteCoin(_RollDifficultyCoin(reward) + _GetEliteCoinFlatBonus());
        }

        private static float _GetDifficultyCoinMultiplier()
        {
            return StageManager.Instance != null ? StageManager.Instance.DifficultyValues.eliteCoinMultiplier : 1f;
        }

        // 처치로 버는 코인에 난이도 배율을 곱한다.
        private static int _RollDifficultyCoin(int amount)
        {
            return _RollFractionalAmount(amount * _GetDifficultyCoinMultiplier());
        }

        // 스킬 트리 '엘리트 코인' 노드의 추가 재화(엘리트 처치마다 +N개).
        private static int _GetEliteCoinFlatBonus()
        {
            if (SkillTreeManager.Instance == null) return 0;

            return Mathf.Max(0, Mathf.RoundToInt(SkillTreeManager.Instance.GetValue(SkillTreePassiveType.EliteCoinBonusFlat)));
        }

        // 소수 부분은 확률로 지급해 기댓값을 맞춘다. 소수 부분이 없으면 난수를 쓰지 않는다.
        private static int _RollFractionalAmount(float amount)
        {
            int wholeAmount = Mathf.FloorToInt(amount);
            float fraction = amount - wholeAmount;

            if (fraction <= 0f) return wholeAmount;

            return Random.value < fraction ? wholeAmount + 1 : wholeAmount;
        }

        // 스킬 트리 노드 — 적 처치 시 확률로 재화 1개. 로비 배경 연출의 처치로는 쌓이지 않게 판 안에서만 굴린다.
        private void _TryGrantKillCoin()
        {
            if (InGameSequence.Instance == null || SkillTreeManager.Instance == null) return;

            float chancePercent = SkillTreeManager.Instance.GetValue(SkillTreePassiveType.KillEliteCoinChance);

            if (chancePercent > 0f && Random.value * 100f < chancePercent)
                AddEliteCoin(_RollDifficultyCoin(1));
        }

        // 역 도달 시 영구 재화 획득. 사고 영역인 초반 역은 0이고, 뒤로 갈수록 완만하게 오른다.
        private void _OnStationPassed(StationPassedEvent stationPassedEvent)
        {
            AddEliteCoin(GetStationReward(stationPassedEvent.PassedStationCount));
        }

        /// <summary>역 도달 보상: 기준 역 이하면 0, 그 위로는 <c>⌈⌈(역 번호 - 기준) / 단계⌉ × 난이도 배율⌉</c>.</summary>
        public int GetStationReward(int stationNumber)
        {
            if (_stationRewardStep <= 0) return 0;

            int stationsOverOffset = stationNumber - _stationRewardOffset;
            if (stationsOverOffset <= 0) return 0;

            int baseReward = Mathf.CeilToInt(stationsOverOffset / (float)_stationRewardStep);

            return Mathf.CeilToInt(baseReward * _GetDifficultyCoinMultiplier());
        }

        #region EliteCoin
        public void AddEliteCoin(int amount)
        {
            if (amount <= 0) return;
            _eliteCoin += amount;
            RunEliteCoinEarned += amount;
            _SaveEliteCoin();
            _SaveRunEliteCoinEarned();
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
        #endregion

        #region Level
        public int GetLevel(string upgradeId)
            => _levels.TryGetValue(upgradeId, out int level) ? level : 0;
        #endregion

        #region Bonus / Value
        // 누적값을 캐시하고 초기화 때만 재계산한다 (매 호출 순회·할당 제거).
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

        // 레벨이 바뀌면(초기화) 캐시를 버려 다음 조회 때 재계산하게 한다.
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

            _RefundLegacyUpgrades();
        }

        // 옛 영구 강화 레벨이 남은 세이브는 쓴 코인을 돌려주고 레벨을 지운다. 강화는 이제 스킬 트리로 대체됐다.
        // 레벨 키를 지우므로 다음 실행에서 다시 환급되지 않는다.
        private void _RefundLegacyUpgrades()
        {
            if (_levels.Count == 0) return;

            int refund = 0;
            int refundedUpgradeCount = 0;
            foreach (var upgrade in LegacyUpgradeCosts)
            {
                if (!_levels.TryGetValue(upgrade.id, out int level)) continue;

                refundedUpgradeCount++;
                int boughtLevels = Mathf.Min(level, upgrade.maxLevel);
                for (int current = 0; current < boughtLevels; current++)
                    refund += Mathf.RoundToInt(upgrade.baseCost * Mathf.Pow(upgrade.growthRate, current));
            }

            if (refundedUpgradeCount < _levels.Count)
                Debug.LogWarning($"[PermanentUpgradeManager] 비용표에 없는 영구 강화 {_levels.Count - refundedUpgradeCount}개는 환급 없이 삭제됩니다: {PlayerPrefs.GetString(LEVELS_KEY, "")}");

            _eliteCoin += Mathf.Min(refund, LEGACY_REFUND_CAP);
            _levels.Clear();
            _SaveEliteCoin();
            PlayerPrefs.DeleteKey(LEVELS_KEY);
            PlayerPrefs.Save();
        }

        private void _SaveEliteCoin()
        {
            // 디스크 flush(PlayerPrefs.Save)는 OnApplicationPause/Quit에서 일괄 — 엘리트 처치마다 디스크 쓰기 방지
            PlayerPrefs.SetInt(ELITE_COIN_KEY, _eliteCoin);
        }

        [Sirenix.OdinInspector.Button("테스트 엘리트 코인 +100")]
        private void AddTestEliteCoin()
        {
            AddEliteCoin(100);
            Debug.Log($"[PermanentUpgradeManager] 테스트 엘리트 코인 +100 (현재 {_eliteCoin})");
        }

        // 옵션 "전체 초기화"에서도 부른다. 디스크만 지우면 DDOL인 이 매니저의 메모리 값이 다음 저장 때 되살아난다.
        [Sirenix.OdinInspector.Button("영구 업그레이드/엘리트 코인 초기화")]
        public void ResetAll()
        {
            _eliteCoin = 0;
            _levels.Clear();
            _InvalidateCache();
            PlayerPrefs.DeleteKey(ELITE_COIN_KEY);
            PlayerPrefs.DeleteKey(LEVELS_KEY);
            PlayerPrefs.DeleteKey(RUN_ELITE_COIN_EARNED_KEY);
            PlayerPrefs.Save();
            Debug.Log("[PermanentUpgradeManager] 영구 업그레이드 데이터가 초기화되었습니다.");
        }
        #endregion
    }
}
