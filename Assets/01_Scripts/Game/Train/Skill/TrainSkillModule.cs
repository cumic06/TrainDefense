using System.Collections.Generic;
using System.Linq;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Stats;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// Train의 스킬/시한버프 책임을 분리한 컴포지션 모듈.
    /// 액티브 스킬(TrainSkillAction)과 시한 스탯 버프(TimedStatModifier)를 보유·관리한다.
    /// </summary>
    public class TrainSkillModule
    {
        private Train _owner;
        private TrainData _trainData;
        private TrainSkillAction _activeSkill;
        private readonly List<TimedStatModifier> _timedModifiers = new();
        private readonly List<TrainPassiveSkill> _passives = new();

        public TrainSkillAction ActiveSkill => _activeSkill;

        public bool HasActiveSkill => _activeSkill != null;

        public Sprite SkillIcon
        {
            get
            {
                if (_trainData == null || _activeSkill == null) return null;
                return _trainData.SkillIcon != null ? _trainData.SkillIcon : _trainData.Icon;
            }
        }

        public float SkillCooldown => _activeSkill != null ? _trainData?.TrainSkillData?.SkillCooldown ?? 0f : 0f;
        public bool CanUse => _activeSkill != null && _activeSkill.CanUse;
        public float CooldownRatio => _activeSkill?.GetCooldownRatio() ?? 0f;
        public float RemainingCooldown => _activeSkill?.RemainingCooldown ?? 0f;

        public void Initialize(Train owner, TrainData trainData, TrainChoiceSkillType skillTypeMask = TrainChoiceSkillType.None)
        {
            // 재초기화 시 기존 구독 해제
            for (int i = 0; i < _passives.Count; i++) _passives[i].Unsubscribe();
            _passives.Clear();
            _activeSkill?.Unsubscribe();

            _owner = owner;
            _trainData = trainData;
            _timedModifiers.Clear();

            bool createActive = skillTypeMask != TrainChoiceSkillType.Passive;
            _activeSkill = (trainData != null && createActive)
                ? TrainSkillActionFactory.Create(owner, trainData.TrainSkillData)
                : null;
        }

        public bool TryUse() => _activeSkill != null && _activeSkill.TryUse();

        /// <summary>
        /// 시한 스탯 버프. 동일 StatType이 이미 활성 중이면 시간만 리프레시(중첩 방지).
        /// </summary>
        public void ApplyTimedStat(StatType type, float percent, float duration)
        {
            if (duration <= 0f) return;

            var existing = _timedModifiers.FirstOrDefault(m => m.Type == type);
            if (existing != null)
            {
                existing.RemainingTime = duration;
                return;
            }

            // 상점과 동일한 레벨인지 곱셈 모델(0→1 토글)로 적용 → 공속은 역수 곱셈, 만료 시 1→0으로 정확히 역적용.
            _owner?.ApplyStatsLevelAware(new IStat[] { new SimpleStat { Type = type, Value = percent } }, 1, 0);
            _timedModifiers.Add(new TimedStatModifier
            {
                Type = type,
                Value = percent,
                RemainingTime = duration
            });
        }

        public void Tick(float deltaTime)
        {
            if (_timedModifiers.Count > 0)
            {
                for (int i = _timedModifiers.Count - 1; i >= 0; i--)
                {
                    var mod = _timedModifiers[i];
                    mod.RemainingTime -= deltaTime;
                    if (mod.RemainingTime <= 0f)
                    {
                        // 적용의 정확한 역연산(1→0 토글). 같은 모델이라 가산·곱셈 모두 정확히 복원.
                        _owner?.ApplyStatsLevelAware(new IStat[] { new SimpleStat { Type = mod.Type, Value = mod.Value } }, 0, 1);
                        _timedModifiers.RemoveAt(i);
                    }
                }
            }

            for (int i = 0; i < _passives.Count; i++)
            {
                _passives[i].Tick(deltaTime);
            }
        }

        public void RegisterPassiveFromData(Datas.TrainPassiveSkillData data)
        {
            if (data == null) return;
            RegisterPassivesFromRaw(data.ToDsl());
        }

        public void RegisterPassivesFromRaw(string raw)
        {
            var skills = TrainPassiveSkillFactory.ParseAll(raw);
            foreach (var p in skills)
            {
                p.Initialize(_owner);
                _passives.Add(p);
            }
        }

        /// <summary>모든 스킬 Unsubscribe + 클리어. Train.OnDestroy에서 호출.</summary>
        public void Dispose()
        {
            for (int i = 0; i < _passives.Count; i++) _passives[i].Unsubscribe();
            _passives.Clear();
            _activeSkill?.Unsubscribe();
            _timedModifiers.Clear();
        }
    }
}
