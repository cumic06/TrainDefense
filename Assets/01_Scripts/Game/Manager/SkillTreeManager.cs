using System.Collections.Generic;
using UnityEngine;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.SkillTree;
using TrainDefense.Game.Stats;
using TrainDefense.Localize;

namespace TrainDefense.Game
{
    /// <summary>
    /// 스킬트리(로비) 시스템. 노드를 습득하며 런 사이에 유지된다.
    /// 판정·계산은 SkillTreeCore(순수 C#)가 담당하고, 이 매니저는 재화·저장·이벤트·캐시 어댑터다.
    /// - TurretStat 노드: GetBonus(StatType)로 조회 → 포탑·레인지 스탯에 가산.
    /// - Passive 노드: GetValue(SkillTreePassiveType)로 조회 → 각 시스템이 적용.
    /// - TurretUnlock 노드: IsTrainUnlocked(trainId)로 조회 → 삼중택일 Add 풀이 게이트.
    ///
    /// 재화는 <see cref="PermanentUpgradeManager"/>가 엘리트 몬스터 처치로 적립하는 것을 그대로 쓴다 (지갑 하나).
    /// 여기서 따로 적립하지 않으므로 보유량·저장은 전적으로 그쪽 소유다.
    /// </summary>
    public class SkillTreeManager : Singleton<SkillTreeManager>
    {
        private const string SAVE_KEY = "SkillTreeSaveData";

        private SkillTreeCore _core;
        private SkillTreeSaveData _pendingSave;   // 코어 생성 전(DB 로드 전)에 읽어 둔 세이브
        private bool _hasLoggedEmptyNodeData;     // 빈 DB 에러 로그 1회 제한 (조회마다 스팸 방지)

        /// <summary>습득에 쓸 수 있는 보유 재화 — 엘리트 처치로 쌓이는 지갑을 그대로 조회한다.</summary>
        public int AvailableCoin => PermanentUpgradeManager.Instance != null
            ? PermanentUpgradeManager.Instance.EliteCoin
            : 0;

        // 씬 와이어링 0 — 첫 씬 로드 전에 스스로 생성된다 (씬 배치 불필요).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void _Bootstrap()
        {
            if (Instance != null) return;

            var managerObject = new GameObject("SkillTreeManager");
            managerObject.AddComponent<SkillTreeManager>();
        }

        #region LifeCycle
        protected override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            DontDestroyOnLoad(gameObject);
            _Load();
        }

        // 습득/환급마다 PlayerPrefs.Save()(디스크 flush)를 부르지 않고, 백그라운드 전환·종료 시 한 번에 기록한다.
        private void OnApplicationPause(bool pause)
        {
            if (pause) PlayerPrefs.Save();
        }

        private void OnApplicationQuit()
        {
            PlayerPrefs.Save();
        }
        #endregion

        // DB가 준비된 뒤 첫 조회 시점에 코어를 만든다. 노드 데이터 소스는 DB(skillNodeDataList)가 유일하다.
        private bool _EnsureCore()
        {
            if (_core != null) return true;
            if (DatabaseManager.Instance == null) return false;

            IReadOnlyList<SkillNodeData> datas = DatabaseManager.Instance.GetSkillNodeDatas();

            if (datas == null || datas.Count == 0)
            {
                if (!_hasLoggedEmptyNodeData)
                {
                    _hasLoggedEmptyNodeData = true;
                    Debug.LogError("[SkillTreeManager] DB의 skillNodeDataList가 비어 있습니다 — 스킬트리를 초기화할 수 없습니다.", this);
                }

                return false;
            }

            _core = new SkillTreeCore(datas);

            if (_pendingSave != null)
            {
                foreach (var entry in _pendingSave.nodeLevels)
                    _core.SetLevel(entry.nodeId, entry.level);

                _pendingSave = null;
            }

            return true;
        }

        #region Query
        public IReadOnlyCollection<SkillNodeData> GetNodes()
            => _EnsureCore() ? _core.Nodes : System.Array.Empty<SkillNodeData>();

        public SkillNodeData GetNode(string nodeId)
            => _EnsureCore() ? _core.GetNode(nodeId) : null;

        public int GetLevel(string nodeId)
            => _EnsureCore() ? _core.GetLevel(nodeId) : 0;

        public bool IsMaxLevel(string nodeId)
            => _EnsureCore() && _core.IsMaxLevel(nodeId);

        public bool ArePrerequisitesMet(string nodeId)
            => _EnsureCore() && _core.ArePrerequisitesMet(nodeId);

        public int GetNextCost(string nodeId)
            => _EnsureCore() ? _core.GetNextCost(nodeId) : int.MaxValue;

        /// <summary>현재 보유 재화로 해당 노드를 습득(레벨업) 가능한지.</summary>
        public bool CanAcquire(string nodeId)
            => _EnsureCore() && _core.CanAcquire(nodeId, AvailableCoin);

        /// <summary>현재 보유 재화로 습득 가능한 노드가 하나라도 있는지. (레드닷 판정용)</summary>
        public bool HasAcquirableNode()
            => _EnsureCore() && _core.HasAcquirableNode(AvailableCoin);

        /// <summary>레인(계열) 표시 이름 — DB의 skillTreeLaneDataList에서 조회, 미등록 레인은 기본 키 폴백.</summary>
        public string GetLaneName(SkillTreeLane lane)
        {
            if (DatabaseManager.Instance != null)
            {
                IReadOnlyList<SkillTreeLaneData> laneDatas = DatabaseManager.Instance.GetSkillTreeLaneDatas();
                if (laneDatas != null)
                {
                    foreach (var laneData in laneDatas)
                    {
                        if (laneData != null && laneData.Lane == lane)
                            return laneData.Name;
                    }
                }
            }

            return lane switch
            {
                SkillTreeLane.Firepower => LocalizeHelper.GetByKey("UI_SkillTree_Lane_Fire", "화력"),
                SkillTreeLane.Defense => LocalizeHelper.GetByKey("UI_SkillTree_Lane_Defense", "방어"),
                SkillTreeLane.Utility => LocalizeHelper.GetByKey("UI_SkillTree_Lane_Utility", "유틸"),
                _ => lane.ToString(),
            };
        }

        /// <summary>
        /// 해당 포탑이 삼중택일에 등장 가능한지 (TurretUnlock 게이트).
        /// 코어 미준비(DB 로드 전) 시 true — 스킬트리 문제로 선택지가 잠기는 일이 없도록 안전 폴백.
        /// </summary>
        public bool IsTrainUnlocked(string trainId)
            => !_EnsureCore() || _core.IsTrainUnlocked(trainId);
        #endregion

        #region Acquire / Respec
        /// <summary>노드 1레벨 습득 시도. 판정→차감→저장→이벤트를 명시적 순서로 지휘한다 (오케스트레이터).</summary>
        public bool TryAcquire(string nodeId)
        {
            if (!_EnsureCore()) return false;

            var upgradeManager = PermanentUpgradeManager.Instance;
            if (upgradeManager == null) return false;

            if (!_core.CanAcquire(nodeId, upgradeManager.EliteCoin)) return false;

            int cost = _core.GetNextCost(nodeId);

            // 차감을 레벨 반영보다 먼저 한다. 반대 순서면 차감이 실패했을 때 공짜 습득이 남는다.
            // (비용 0 노드는 SpendEliteCoin이 false를 돌려주므로 호출 자체를 건너뛴다)
            if (cost > 0 && !upgradeManager.SpendEliteCoin(cost)) return false;

            _core.TryLevelUp(nodeId, cost, out _);
            _SaveLevels();
            // 습득은 의식적 행동이라 강제 종료에도 잃지 않도록 즉시 디스크 flush
            PlayerPrefs.Save();
            _InvalidateCache();
            GameEventSystem.Publish(new SkillNodeAcquiredEvent(nodeId, cost, _core.GetLevel(nodeId)));

            return true;
        }

        /// <summary>리스펙 — 전 노드 초기화 + 지출 재화 전액 환급 (v1 무료). 습득한 노드가 없으면 아무것도 안 한다.</summary>
        public bool ResetAll()
        {
            if (!_EnsureCore()) return false;
            if (_core.Levels.Count == 0) return false;

            var upgradeManager = PermanentUpgradeManager.Instance;
            if (upgradeManager == null) return false;

            int refund = _core.GetTotalSpentPoints();
            _core.ResetAllLevels();
            // 환급도 지갑으로 돌아간다. 리스펙은 로비에서만 열리고 판 진입 때 RunEliteCoinEarned가 0으로 리셋되므로
            // 결산의 "이번 판 획득량"에는 섞이지 않는다.
            upgradeManager.AddEliteCoin(refund);
            _SaveLevels();
            PlayerPrefs.Save();
            _InvalidateCache();
            GameEventSystem.Publish(new SkillTreeResetEvent(refund));

            return true;
        }
        #endregion

        #region Bonus / Value
        // 레벨은 습득/리스펙 시에만 바뀌므로 누적값을 캐시하고, 그때만 재계산한다 (영구강화 선례).
        private Dictionary<StatType, float> _bonusCache;
        private Dictionary<SkillTreePassiveType, float> _valueCache;

        /// <summary>TurretStat 노드들의 해당 StatType 누적 보너스 (레벨 × 레벨당 증가량). 포탑·레인지 스탯에 가산용.</summary>
        public float GetBonus(StatType statType)
        {
            if (!_EnsureCore()) return 0f;
            if (_bonusCache == null) _RebuildCache();

            return _bonusCache.TryGetValue(statType, out float value) ? value : 0f;
        }

        /// <summary>Passive 노드들의 해당 효과 누적값 (레벨 × 레벨당 값). 각 시스템이 조회해 적용한다.</summary>
        public float GetValue(SkillTreePassiveType type)
        {
            if (!_EnsureCore()) return 0f;
            if (_valueCache == null) _RebuildCache();

            return _valueCache.TryGetValue(type, out float value) ? value : 0f;
        }

        private void _RebuildCache()
        {
            _bonusCache = new Dictionary<StatType, float>();
            _valueCache = new Dictionary<SkillTreePassiveType, float>();

            foreach (var node in _core.Nodes)
            {
                if (node == null) continue;

                int level = _core.GetLevel(node.Id);
                if (level <= 0) continue;

                if (node.Category == SkillNodeCategory.TurretStat)
                {
                    if (node.Stats == null) continue;

                    foreach (var stat in node.Stats)
                    {
                        if (stat == null) continue;

                        _bonusCache.TryGetValue(stat.Type, out float cur);
                        _bonusCache[stat.Type] = cur + stat.Value * level;
                    }
                }
                else if (node.Category == SkillNodeCategory.Passive)
                {
                    _valueCache.TryGetValue(node.PassiveType, out float cur);
                    _valueCache[node.PassiveType] = cur + node.PassiveValuePerLevel * level;
                }
                // TurretUnlock은 캐시 불필요 — IsTrainUnlocked가 코어 레벨을 직접 판정
            }
        }

        private void _InvalidateCache()
        {
            _bonusCache = null;
            _valueCache = null;
        }
        #endregion

        #region Save / Load
        private void _Load()
        {
            _pendingSave = SkillTreeSaveData.FromJson(PlayerPrefs.GetString(SAVE_KEY, ""));
        }

        private void _SaveLevels()
        {
            var saveData = new SkillTreeSaveData();
            foreach (var pair in _core.Levels)
                saveData.nodeLevels.Add(new SkillTreeSaveData.NodeLevelEntry { nodeId = pair.Key, level = pair.Value });

            PlayerPrefs.SetString(SAVE_KEY, saveData.ToJson());
        }

        [Sirenix.OdinInspector.Button("습득 노드 전부 삭제")]
        private void DeleteAllSaveData()
        {
            _core = null;
            _pendingSave = null;
            _InvalidateCache();
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            Debug.Log("[SkillTreeManager] 스킬트리 습득 기록이 삭제되었습니다. (재화는 영구강화 지갑 소유라 그대로)");
        }
        #endregion
    }
}
