using System.Collections.Generic;
using System.Linq;
using Cumic.Events;
using UnityEngine;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;

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

        // 기차별로 적용된 업그레이드 데이터 id 이력. 레벨 숫자만으로는 어떤 업그레이드였는지 알 수 없어 복원이 불가능하다.
        private readonly Dictionary<Train, List<string>> _upgradeHistory = new();

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
            public float HpRatio = 1f;
            public bool IsDead;
            public int SkillTypeMask;
            public string SelectedSkillId;
            public List<string> UpgradeDataIds = new();
        }

        #endregion

        #region Run Save

        /// <summary>업그레이드 적용 이력을 기록한다. (UpgradeTrain·ReplaceTrain에서 호출)</summary>
        private void _RecordUpgradeHistory(Train train, ITrainUpgradeData upgradeData)
        {
            if (train == null || upgradeData == null || string.IsNullOrEmpty(upgradeData.Id))
                return;

            if (!_upgradeHistory.TryGetValue(train, out var history))
            {
                history = new List<string>();
                _upgradeHistory[train] = history;
            }

            history.Add(upgradeData.Id);
        }

        /// <summary>엘리트 교체 등으로 기차 인스턴스가 바뀔 때 업그레이드 이력과 base 출처를 새 인스턴스로 옮긴다.</summary>
        private void _TransferUpgradeHistory(Train oldTrain, Train newTrain, string oldTrainDataId)
        {
            if (oldTrain == null || newTrain == null)
                return;

            if (_upgradeHistory.TryGetValue(oldTrain, out var history))
            {
                _upgradeHistory[newTrain] = new List<string>(history);
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
                    HpRatio = train.CurrentHpRatio,
                    IsDead = train.IsDead,
                    SkillTypeMask = (int)train.SkillTypeMask,
                    SelectedSkillId = train.SelectedSkillId,
                };

                if (_upgradeHistory.TryGetValue(train, out var history))
                    entry.UpgradeDataIds = new List<string>(history);

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
            _RestoreUpgrades(train, entry, databaseManager);

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

            _RestoreHpAndDeath(train, entry);

            return true;
        }

        // 저장된 업그레이드 이력을 순서대로 다시 적용한다.
        //
        // ⚠️ 현재 미지원 — id로 ITrainUpgradeData를 되찾는 경로가 이 브랜치에 없다.
        // 이 기능이 만들어질 당시 있던 DatabaseManager.GetTrainUpgradeDataById가
        // 상점 스탯 강화 등급 체계 도입(e4efa240) 때 사라졌고, 대체 조회가 아직 없다.
        // 이력은 저장되고 있으므로, 조회 경로만 되살리면 아래 주석 처리된 흐름을 그대로 쓸 수 있다.
        private void _RestoreUpgrades(Train train, FormationTrainState entry, DatabaseManager databaseManager)
        {
            if (train == null || entry.UpgradeDataIds == null)
                return;

            if (entry.UpgradeDataIds.Count > 0)
            {
                // 조용히 초기 스탯으로 부활시키면 이어하기가 손해처럼 보이므로 남긴다.
                Debug.LogWarning($"MainTrain: [{entry.TrainDataId}]의 업그레이드 {entry.UpgradeDataIds.Count}건을 복원하지 못했습니다 — id로 업그레이드 데이터를 찾는 경로가 없습니다.");

                return;
            }

            // 이력이 유실된 세이브 대비: 레벨 숫자만 남아 있으면 최소한 로그로 알린다(스탯은 이력 기준으로만 재현된다).
            if (entry.Level > 0)
                Debug.LogWarning($"MainTrain: [{entry.TrainDataId}]의 업그레이드 이력이 없어 레벨 {entry.Level}을 재현하지 못했습니다.");
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
