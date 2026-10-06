using System;
using System.Collections.Generic;
using UnityEngine;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;

namespace TrainDefense.Game.RunSave.Sections
{
    /// <summary>
    /// 기차 편성 섹션 — 편성된 기차 목록(데이터 id·레벨·체력 비율·생사·부여 스킬)과 엘리트 교체로 소비된 id, 누적 생존 시간.
    /// 기차 인스턴스를 통째로 직렬화하지 않고 "무엇을 어떤 순서로 몇 레벨까지 올렸는가"만 기록해,
    /// 복원 시 평소와 같은 생성 경로(SpawnTrain → Upgrade)를 그대로 태운다.
    /// </summary>
    public class TrainFormationSection : IRunStateSection
    {
        public string SectionId => "train_formation";

        [Serializable]
        private class Payload
        {
            public float mainTrainHpRatio = 1f;
            public List<string> replacedTrainIds = new();
            public List<TrainEntry> trains = new();
        }

        [Serializable]
        private class TrainEntry
        {
            public string trainDataId;
            public string replacedFromTrainDataId;
            public int level;
            public int upgradeGradeSum;
            public float hpRatio = 1f;
            public bool isDead;
            public int skillTypeMask;
            public string selectedSkillId;
            public List<UpgradeStepEntry> upgradeDeltas = new();
            public List<StatUpgradeAmountEntry> statUpgradeAmounts = new();
        }

        // 업그레이드 한 번의 증가량. 상점 강화 데이터는 구매하는 순간 만들어져 DB에 없으므로 id로는 되찾을 수 없다.
        [Serializable]
        private class UpgradeStepEntry
        {
            public TurretTrainStatus turret;
            public RangeTrainStatus range;
        }

        [Serializable]
        private class StatUpgradeAmountEntry
        {
            public int statType;
            public float amount;
        }

        public string Capture()
        {
            var trainManager = TrainManager.Instance;
            var mainTrain = trainManager != null ? trainManager.MainTrain : null;

            if (mainTrain == null)
                return null;

            var state = mainTrain.CaptureFormation();
            var payload = new Payload
            {
                mainTrainHpRatio = state.MainTrainHpRatio,
                replacedTrainIds = new List<string>(state.ReplacedTrainIds),
            };

            foreach (var train in state.Trains)
            {
                var entry = new TrainEntry
                {
                    trainDataId = train.TrainDataId,
                    replacedFromTrainDataId = train.ReplacedFromTrainDataId,
                    level = train.Level,
                    upgradeGradeSum = train.UpgradeGradeSum,
                    hpRatio = train.HpRatio,
                    isDead = train.IsDead,
                    skillTypeMask = train.SkillTypeMask,
                    selectedSkillId = train.SelectedSkillId,
                };

                foreach (var step in train.UpgradeDeltas)
                {
                    if (step != null)
                        entry.upgradeDeltas.Add(new UpgradeStepEntry { turret = step.Turret, range = step.Range });
                }

                foreach (var statUpgradeAmount in train.StatUpgradeAmounts)
                {
                    if (statUpgradeAmount != null)
                        entry.statUpgradeAmounts.Add(new StatUpgradeAmountEntry
                        {
                            statType = (int)statUpgradeAmount.Type,
                            amount = statUpgradeAmount.Amount,
                        });
                }

                payload.trains.Add(entry);
            }

            return JsonUtility.ToJson(payload);
        }

        public void Restore(string json)
        {
            var trainManager = TrainManager.Instance;
            var mainTrain = trainManager != null ? trainManager.MainTrain : null;

            if (mainTrain == null)
                return;

            Payload payload = null;
            try
            {
                payload = JsonUtility.FromJson<Payload>(json);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (payload == null)
                return;

            var state = new MainTrain.FormationState
            {
                MainTrainHpRatio = payload.mainTrainHpRatio,
                ReplacedTrainIds = payload.replacedTrainIds ?? new List<string>(),
                Trains = new List<MainTrain.FormationTrainState>(),
            };

            if (payload.trains != null)
            {
                foreach (var train in payload.trains)
                {
                    if (train == null || string.IsNullOrEmpty(train.trainDataId))
                        continue;

                    var entry = new MainTrain.FormationTrainState
                    {
                        TrainDataId = train.trainDataId,
                        ReplacedFromTrainDataId = train.replacedFromTrainDataId,
                        Level = train.level,
                        UpgradeGradeSum = train.upgradeGradeSum,
                        HpRatio = train.hpRatio,
                        IsDead = train.isDead,
                        SkillTypeMask = train.skillTypeMask,
                        SelectedSkillId = train.selectedSkillId,
                    };

                    if (train.upgradeDeltas != null)
                    {
                        foreach (var step in train.upgradeDeltas)
                        {
                            if (step != null)
                                entry.UpgradeDeltas.Add(new MainTrain.UpgradeStepDelta { Turret = step.turret, Range = step.range });
                        }
                    }

                    if (train.statUpgradeAmounts != null)
                    {
                        foreach (var statUpgradeAmount in train.statUpgradeAmounts)
                        {
                            if (statUpgradeAmount != null)
                                entry.StatUpgradeAmounts.Add(new MainTrain.StatUpgradeAmountEntry
                                {
                                    Type = (StatType)statUpgradeAmount.statType,
                                    Amount = statUpgradeAmount.amount,
                                });
                        }
                    }

                    state.Trains.Add(entry);
                }
            }

            mainTrain.RestoreFormation(state);
        }
    }
}
