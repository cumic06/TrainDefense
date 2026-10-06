using System.Collections.Generic;
using System.Linq;
using Cumic.Events;
using UnityEngine;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game
{
    /// <summary>
    /// MainTrain의 런 세이브 담당 부분 — 편성을 스냅샷하고, 저장된 스냅샷대로 편성을 다시 세운다.
    /// 기차 인스턴스를 직렬화하지 않고 "무엇을 어떤 업그레이드로 올렸는가"만 남긴 뒤,
    /// 복원 때 평소와 같은 생성 경로(SpawnTrain → Upgrade)를 그대로 태워 스탯 계산이 갈라지지 않게 한다.
    /// </summary>
    public partial class MainTrain
    {
        #region Run Save Variables

        // 기차별로 적용된 업그레이드 증가량 이력. 레벨 숫자만으로는 어떤 업그레이드였는지 알 수 없어 복원이 불가능하다.
        private readonly Dictionary<Train, List<UpgradeStepDelta>> _upgradeHistory = new();

        // 복원으로 다시 얹는 업그레이드에 붙이는 id 접두어. 로그에서 구매분과 구분하기 위한 것으로, 조회에는 쓰이지 않는다.
        private const string RESTORED_UPGRADE_ID_PREFIX = "restored_";

        // 엘리트 교체로 만들어진 기차 → 그 출발점이 된 base 기차의 데이터 id.
        // 엘리트는 스탯을 base에서 통째로 승계(CopyProgressFrom)하므로, 복원도 "base를 올린 뒤 교체"라는 같은 경로를 밟아야 한다.
        private readonly Dictionary<Train, string> _eliteOriginMap = new();

        /// <summary>런 세이브용 편성 스냅샷.</summary>
        public class FormationState
        {
            public float MainTrainHpRatio = 1f;
            public List<string> ReplacedTrainIds = new();
            public List<FormationTrainState> Trains = new();
        }

        /// <summary>편성된 기차 한 량의 스냅샷.</summary>
        public class FormationTrainState
        {
            public string TrainDataId;
            /// <summary>엘리트 교체로 만들어졌다면 그 출발점이 된 base 기차의 데이터 id. 아니면 비어 있다.</summary>
            public string ReplacedFromTrainDataId;
            public int Level;
            /// <summary>상점 강화 카드 등급의 합(개조 조건). 0이면 기록 없음(이전 세이브) 또는 강화 없음.</summary>
            public int UpgradeGradeSum;
            public float HpRatio = 1f;
            public bool IsDead;
            public int SkillTypeMask;
            public string SelectedSkillId;
            public List<UpgradeStepDelta> UpgradeDeltas = new();
            public List<StatUpgradeAmountEntry> StatUpgradeAmounts = new();
            /// <summary>저장 당시 공격 간격. 0이면 기록 없음(이전 세이브). 상점·레벨업 공속은 받은 순서에 따라 증가량이 달라 증가량 재생만으로는 재현되지 않는다.</summary>
            public float AttackInterval;
        }

        /// <summary>
        /// 업그레이드 한 번이 더한 증가량. 상점 강화 데이터는 구매하는 순간 만들어져(CreateRuntimeSingleStat) DB에 남지 않으므로,
        /// id로는 되찾을 수 없고 증가량 자체를 남겨야 복원할 수 있다.
        /// </summary>
        public class UpgradeStepDelta
        {
            public TurretTrainStatus Turret;
            public RangeTrainStatus Range;
        }

        /// <summary>스탯 하나의 누적 강화량. (Dictionary는 JsonUtility가 직렬화하지 못해 목록으로 편다)</summary>
        public class StatUpgradeAmountEntry
        {
            public StatType Type;
            public float Amount;
        }

        #endregion

        #region Run Save

        /// <summary>업그레이드 적용 이력을 기록한다. (UpgradeTrain·ReplaceTrain에서 호출)</summary>
        private void _RecordUpgradeHistory(Train train, ITrainUpgradeData upgradeData)
        {
            if (train == null || upgradeData == null)
                return;

            // 이 호출은 Upgrade가 끝난 뒤라 레벨이 이미 하나 올라 있다 — 방금 적용된 인덱스는 그 직전 값이다.
            int appliedLevelIndex = Mathf.Max(0, train.CurrentLevel - 1);
            var step = new UpgradeStepDelta();

            switch (upgradeData)
            {
                case TurretTrainUpgradeData turretUpgradeData:
                    step.Turret = turretUpgradeData.GetTurretStatusUpgrade(appliedLevelIndex);
                    break;

                case RangeTrainUpgradeData rangeUpgradeData:
                    step.Range = rangeUpgradeData.GetRangeStatusUpgrade(appliedLevelIndex);
                    break;
            }

            // 지금 업그레이드 경로(상점 스탯 강화)는 체력을 올리지 않는다. 올리는 업그레이드가 생기면 여기서 조용히 빠지므로 알린다.
            if (upgradeData.GetStatusUpgrade(appliedLevelIndex).MaxHp != 0f)
                Debug.LogWarning($"MainTrain: [{upgradeData.Id}]의 체력 증가량은 런 세이브에 담기지 않아 이어하기에서 빠집니다.");

            _AppendUpgradeStep(train, step);
        }

        private void _AppendUpgradeStep(Train train, UpgradeStepDelta step)
        {
            if (train == null || step == null)
                return;

            if (!_upgradeHistory.TryGetValue(train, out var history))
            {
                history = new List<UpgradeStepDelta>();
                _upgradeHistory[train] = history;
            }

            history.Add(step);
        }

        /// <summary>엘리트 교체 등으로 기차 인스턴스가 바뀔 때 업그레이드 이력과 base 출처를 새 인스턴스로 옮긴다.</summary>
        private void _TransferUpgradeHistory(Train oldTrain, Train newTrain, string oldTrainDataId)
        {
            if (oldTrain == null || newTrain == null)
                return;

            if (_upgradeHistory.TryGetValue(oldTrain, out var history))
            {
                _upgradeHistory[newTrain] = new List<UpgradeStepDelta>(history);
                _upgradeHistory.Remove(oldTrain);
            }

            // 엘리트가 다시 교체되더라도 출발점은 최초의 base로 유지한다(복원은 항상 그 base부터 시작하므로).
            string origin = _eliteOriginMap.TryGetValue(oldTrain, out string previousOrigin) ? previousOrigin : oldTrainDataId;

            if (!string.IsNullOrEmpty(origin))
                _eliteOriginMap[newTrain] = origin;

            _eliteOriginMap.Remove(oldTrain);
        }

        public FormationState CaptureFormation()
        {
            var state = new FormationState
            {
                MainTrainHpRatio = CurrentHpRatio,
                ReplacedTrainIds = _replacedTrainIds.ToList(),
            };

            foreach (var train in _currentTrains)
            {
                if (train == null || train.TrainData == null)
                    continue;

                var entry = new FormationTrainState
                {
                    TrainDataId = train.TrainData.Id,
                    ReplacedFromTrainDataId = _eliteOriginMap.TryGetValue(train, out string origin) ? origin : null,
                    Level = train.CurrentLevel,
                    UpgradeGradeSum = train.UpgradeGradeSum,
                    HpRatio = train.CurrentHpRatio,
                    IsDead = train.IsDead,
                    SkillTypeMask = (int)train.SkillTypeMask,
                    SelectedSkillId = train.SelectedSkillId,
                    AttackInterval = train.GetCurrentStatValue(StatType.AttackInterval),
                };

                if (_upgradeHistory.TryGetValue(train, out var history))
                    entry.UpgradeDeltas = new List<UpgradeStepDelta>(history);

                // 누적 강화량은 스탯이 아니라 "다음 강화가 얼마나 오를지"를 정한다 — 빠뜨리면 이어한 판에서 범위·연사가 처음처럼 크게 오른다.
                foreach (var pair in train.StatUpgradeAmounts)
                    entry.StatUpgradeAmounts.Add(new StatUpgradeAmountEntry { Type = pair.Key, Amount = pair.Value });

                state.Trains.Add(entry);
            }

            return state;
        }

        /// <summary>
        /// 이어하기 복원 — 현재 편성을 비우고 스냅샷대로 다시 세운다.
        /// 상점 업그레이드(UserDataManager) 복원이 끝난 뒤에 호출해야 SpawnTrain이 그 업그레이드를 함께 얹는다.
        /// </summary>
        public void RestoreFormation(FormationState state)
        {
            if (state == null)
                return;

            _ClearFormation();

            var databaseManager = DatabaseManager.Instance;

            if (databaseManager == null)
            {
                Debug.LogWarning("MainTrain: DatabaseManager가 없어 편성을 복원할 수 없습니다.");

                return;
            }

            // 교체 이력은 편성을 세우기 전에 복원한다. (이후 TriChoice 카드 차단 판단에 쓰인다)
            _replacedTrainIds.Clear();
            foreach (string replacedId in state.ReplacedTrainIds)
            {
                if (!string.IsNullOrEmpty(replacedId))
                    _replacedTrainIds.Add(replacedId);
            }

            foreach (var entry in state.Trains)
            {
                if (entry == null || string.IsNullOrEmpty(entry.TrainDataId))
                    continue;

                if (!_RestoreTrainEntry(entry, databaseManager))
                    break;
            }

            RearrangeAllTrainsToOriginalOrder();
        }

        // 한 량을 저장 당시 모습으로 되돌린다. 편성이 더 이상 늘지 않으면(정원 초과 등) false를 반환해 복원을 끊는다.
        private bool _RestoreTrainEntry(FormationTrainState entry, DatabaseManager databaseManager)
        {
            // 엘리트는 base를 세워 업그레이드까지 올린 뒤 교체한다 — 평소 획득 경로와 같아야 승계 스탯이 어긋나지 않는다.
            bool isElitePromoted = !string.IsNullOrEmpty(entry.ReplacedFromTrainDataId)
                                   && entry.ReplacedFromTrainDataId != entry.TrainDataId;
            string spawnTrainDataId = isElitePromoted ? entry.ReplacedFromTrainDataId : entry.TrainDataId;
            var spawnTrainData = databaseManager.GetTrainData(spawnTrainDataId);

            if (spawnTrainData == null)
            {
                Debug.LogWarning($"MainTrain: 복원할 TrainData [{spawnTrainDataId}]를 찾지 못해 이 칸을 건너뜁니다.");

                return true;
            }

            var skillType = (TrainChoiceSkillType)entry.SkillTypeMask;
            int beforeCount = _currentTrains.Count;
            SpawnTrain(spawnTrainData, skillType, entry.SelectedSkillId);

            // MaxTrainCount 초과 등으로 스폰이 거절되면 편성이 늘지 않는다 — 조용히 어긋나지 않게 여기서 끊는다.
            if (_currentTrains.Count == beforeCount)
            {
                Debug.LogWarning($"MainTrain: [{spawnTrainDataId}] 복원 스폰이 거절되어 이후 칸을 중단합니다.");

                return false;
            }

            var train = _currentTrains[_currentTrains.Count - 1];
            _RestoreUpgrades(train, entry);

            if (isElitePromoted)
            {
                var eliteTrainData = databaseManager.GetTrainData(entry.TrainDataId);

                if (eliteTrainData == null)
                {
                    Debug.LogWarning($"MainTrain: 엘리트 TrainData [{entry.TrainDataId}]를 찾지 못해 base 상태로 남깁니다.");
                }
                else
                {
                    var beforeReplace = new HashSet<Train>(_currentTrains);
                    ReplaceTrain(spawnTrainDataId, eliteTrainData, skillType, entry.SelectedSkillId);
                    train = _currentTrains.FirstOrDefault(t => t != null && !beforeReplace.Contains(t)) ?? train;
                }
            }

            if (entry.AttackInterval > 0f)
                train.RestoreAttackInterval(entry.AttackInterval);

            _RestoreHpAndDeath(train, entry);

            return true;
        }

        // 저장된 증가량을 순서대로 다시 얹는다. 평소 경로(Train.Upgrade)를 그대로 태워야 레벨과 상점 배율이 함께 맞는다.
        private void _RestoreUpgrades(Train train, FormationTrainState entry)
        {
            if (train == null)
                return;

            // 증가량 재생(Train.Upgrade)은 카드 등급을 모르므로 개조 조건용 합계는 따로 되돌린다.
            train.AddUpgradeGradeSum(entry.UpgradeGradeSum);

            // 스탯값과 별개로 되돌려야 하는 값 — 범위 증가분이 이 누적량에서 역산되므로, 빠뜨리면 이어한 판의 다음 강화가 1회차 폭으로 되돌아간다.
            if (entry.StatUpgradeAmounts != null)
            {
                foreach (var statUpgradeAmount in entry.StatUpgradeAmounts)
                {
                    if (statUpgradeAmount != null && statUpgradeAmount.Amount != 0f)
                        train.AddStatUpgradeAmount(statUpgradeAmount.Type, statUpgradeAmount.Amount);
                }
            }

            if (entry.UpgradeDeltas == null || entry.UpgradeDeltas.Count == 0)
            {
                // 증가량 없이 레벨만 남은 세이브(delta 포맷 이전)는 스탯을 재현할 수 없다 — 조용히 초기 스탯으로 두면 이어하기가 손해로 보인다.
                if (entry.Level > 0)
                    Debug.LogWarning($"MainTrain: [{entry.TrainDataId}]의 업그레이드 이력이 없어 레벨 {entry.Level}을 재현하지 못했습니다.");

                return;
            }

            foreach (var step in entry.UpgradeDeltas)
            {
                if (step == null)
                    continue;

                var upgradeData = _CreateRestoreUpgradeData(train, step);

                if (upgradeData == null)
                {
                    Debug.LogWarning($"MainTrain: [{entry.TrainDataId}]는 업그레이드를 받을 수 없는 종류라 증가량을 되돌리지 못했습니다.");

                    break;
                }

                train.Upgrade(upgradeData);
                // 복원한 판을 다시 저장할 때도 이 증가량이 남아야 한다.
                _AppendUpgradeStep(train, step);
            }
        }

        // 저장된 증가량 하나를 담은 1회용 업그레이드 데이터. 상점 강화가 구매할 때 만드는 것과 같은 형태다.
        private ITrainUpgradeData _CreateRestoreUpgradeData(Train train, UpgradeStepDelta step)
        {
            // 실제로 읽히는 인덱스는 "이번에 적용할 레벨"(= 현재 레벨) 하나뿐이라 그 인덱스까지 채운다.
            int levelCount = train.CurrentLevel + 1;
            string id = $"{RESTORED_UPGRADE_ID_PREFIX}{train.TrainData?.Id}";
            string name = train.TrainData != null ? train.TrainData.Name : string.Empty;

            return train switch
            {
                TurretTrain => TurretTrainUpgradeData.CreateRuntimeSingleStat(id, name, step.Turret, levelCount),
                RangeTrain => RangeTrainUpgradeData.CreateRuntimeSingleStat(id, name, step.Range, levelCount),
                _ => null,
            };
        }

        private void _RestoreHpAndDeath(Train train, FormationTrainState entry)
        {
            if (train == null)
                return;

            train.SetHpToRatio(Mathf.Clamp01(entry.HpRatio));

            // 죽은 채로 저장된 칸은 평소 사망 경로를 그대로 태워 죽인다(상점의 긴급 수리로 되살릴 수 있는 상태 유지).
            if (entry.IsDead && !train.IsDead)
                train.TakeDamage(float.MaxValue);
        }

        // 게임 진입 시 자동 스폰된 기본 편성을 걷어낸다. 복원한 편성과 겹치는 것을 막는다.
        private void _ClearFormation()
        {
            foreach (var train in _currentTrains.ToList())
            {
                if (train == null)
                    continue;

                _upgradeHistory.Remove(train);
                _eliteOriginMap.Remove(train);
                Destroy(train.gameObject);
            }

            _currentTrains.Clear();
            _currentAliveTrains.Clear();
            _deadTrains.Clear();
            _trainOriginalIndexMap.Clear();

            GameEventSystem.Publish(new TrainFormationClearedEvent());
        }

        #endregion
    }
}
