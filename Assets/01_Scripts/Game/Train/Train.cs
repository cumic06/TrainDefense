using UnityEngine;
using Cumic;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using Sirenix.OdinInspector;
using TrainDefense.Game.Stats;
using System.Collections.Generic;
using System.Linq;

namespace TrainDefense.Game
{
    public abstract class Train : MonoBehaviour, ITrainable, IProjectileTarget
    {
        #region Verialbes

        #region Field
        [SerializeField]
        protected string id;
        [SerializeField]
        protected float explosionRadius = 5f;
        [SerializeField]
        protected float explosionForce = 10f;
        [SerializeField]
        protected bool isRotateTurret = true;
        #endregion

        [ShowInInspector, ReadOnly]
        protected TrainData _trainData;
        protected bool _isDead;
        protected float _currentHp;
        protected int _currentLevel;
        protected float _currentMaxHp;
        protected readonly TrainSkillModule _skillModule = new();
        protected TrainChoiceSkillType _skillTypeMask = TrainChoiceSkillType.None;
        protected string _selectedSkillId = null;
        private bool _initialized;

        // ApplyStat 퍼센트 누적 손실 방지용 fractional accumulator (UtilMath.AccumulateIntDelta 참조)
        private float _statMaxHpAccum;

        [HideInInspector]
        public bool IsUnDead;

        public string Id => id;
        public TrainData TrainData => _trainData;
        public bool IsMainTrain => _trainData.IsMainTrain;

        public bool IsDead => _isDead;
        public int CurrentLevel => _currentLevel;
        public bool HasActiveSkill => _skillModule.HasActiveSkill;
        public Sprite SkillIcon => _skillModule.SkillIcon;
        public float SkillCooldown => _skillModule.SkillCooldown;
        public bool CanUseSkill => _skillModule.CanUse;
        public float SkillCooldownRatio => _skillModule.CooldownRatio;
        
        #endregion

        protected virtual void Start()
        {
            if (_initialized)
                return;

            Setup();
        }

        public virtual void Initialize(TrainData trainData, TrainChoiceSkillType skillType = TrainChoiceSkillType.None, string selectedSkillId = null)
        {
            _trainData = trainData;
            _skillTypeMask = skillType;
            _selectedSkillId = selectedSkillId;
            Setup();
            _initialized = true;
        }

        protected virtual void Setup()
        {
            _isDead = false;
            if (_trainData == null)
            {
                _skillModule.Initialize(this, null, ApplyStat);
                return;
            }

            _currentMaxHp = _trainData.TrainStatusData.MaxHp;
            _currentHp = _currentMaxHp;
            _currentLevel = -1;
            _skillModule.Initialize(this, _trainData, ApplyStat, _skillTypeMask);
        }

        public Transform TargetTransform => transform;
        public bool IsActive => !IsDead;

        public virtual Transform GetSkillSpawnPoint(int index)
        {
            return transform;
        }

        public void Slow(float slowValue)
        {

        }
        public void ResetMoveSpeed()
        {

        }
        public void Shove(float shovePower, float shoveDuration)
        {

        }
        public void Stun(float stunDuration)
        {

        }

        public virtual void TakeDamage(float damage)
        {
            TakeDamage(damage, false);
        }

        public virtual void TakeDamage(float damage, bool isCritical)
        {
            if (_isDead) return;

            _currentHp -= damage;
            _currentHp = Mathf.Clamp(_currentHp, 0, _currentMaxHp);

            GameEventSystem.Publish(new HitEvent(_currentHp, _currentMaxHp, this, transform.position, damage, isCritical));

            if (_currentHp <= 0)
            {
                OnDead();
            }
        }

        protected virtual void OnDead()
        {
            if (IsUnDead) return;

            if (!IsMainTrain)
            {
                GameEventSystem.Publish(new TrainDeadEvent(this));
            }

            _isDead = true;

            if (TrainData.AttackSoundType != SoundType.None && TrainData.DamageType == DamageType.Tick)
            {
                SoundManager.Instance.StopSFX(TrainData.AttackSoundType);
            }

            // 주변 적을 밀치는 효과
            PushNearbyEnemies();
        }

        protected virtual void PushNearbyEnemies()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius)
            .Where(collider => collider.TryGetComponent(out Monster monster) && monster.gameObject.activeInHierarchy)
            .ToArray();

            foreach (var collider in colliders)
            {
                if (collider.TryGetComponent(out Monster monster))
                {
                    monster.Shove(explosionForce, 0.5f);
                }
            }
        }

        public virtual void Resurrect()
        {
            // HP를 최대치로 복원하고 죽음 상태 해제
            _isDead = false;
            RestoreHpToMax();
        }

        public virtual void RestoreHpToMax()
        {
            _currentHp = _currentMaxHp;
            GameEventSystem.Publish(new HitEvent(_currentHp, _currentMaxHp, this, transform.position, 0));//체력 UI 복원 이벤트 재사용
        }

        public virtual bool TryUseSkill() => _skillModule.TryUse();

        public void ApplyTimedStat(StatType type, float percent, float duration)
            => _skillModule.ApplyTimedStat(type, percent, duration);

        protected virtual void Update()
        {
            _skillModule.Tick(Time.deltaTime);
        }

        protected virtual void OnDestroy()
        {
            _skillModule.Dispose();
        }

        public virtual void Upgrade(ITrainUpgradeData upgradeData)
        {
            if (upgradeData == null) return;

            int currentLevel = CurrentLevel; // 업그레이드 전 레벨
            _currentLevel++;

            // Train 초기 레벨은 -1, upgradeStats 배열은 0부터 시작
            // View 표시 및 업그레이드 적용 시: 레벨 + 1 인덱스 사용
            // 레벨 -1이면 인덱스 0, 레벨 0이면 인덱스 1
            int upgradeLevelIndex = currentLevel + 1;
            var statusUpgrade = upgradeData.GetStatusUpgrade(upgradeLevelIndex);
            _currentMaxHp += statusUpgrade.MaxHp;
            _currentHp += statusUpgrade.MaxHp;
            _currentHp = Mathf.Clamp(_currentHp, 0, _currentMaxHp);

            GameEventSystem.Publish(new TrainLevelUpEvent(this, _currentLevel));
        }

        public virtual void StatusUpgrade(TrainStatusData upgradeData)
        {
            _currentMaxHp += upgradeData.MaxHp;
            _currentHp += upgradeData.MaxHp;
            _currentHp = Mathf.Clamp(_currentHp, 0, _currentMaxHp);
        }

        public virtual void StatusUpgrade(TurretTrainStatus upgradeData)
        {

        }

        public virtual void StatusUpgrade(RangeTrainStatus upgradeData)
        {

        }

        public virtual void ApplyStats(IStat[] stats)
        {
            if (stats == null || stats.Length == 0) return;

            foreach (var stat in stats)
            {
                ApplyStat(stat);
            }
        }

        public virtual void ApplyStatsLevelAware(IStat[] stats, int newLevel, int prevLevel = 0)
        {
            if (stats == null || stats.Length == 0) return;
            foreach (var stat in stats)
                ApplyStatLevelAware(stat, newLevel, prevLevel);
        }

        protected virtual void ApplyStatLevelAware(IStat stat, int newLevel, int prevLevel)
        {
            if (stat == null) return;
            switch (stat.Type)
            {
                case StatType.MaxHp:
                {
                    float baseMaxHp = _trainData.TrainStatusData.MaxHp;
                    float percent = stat.Value / 100f;
                    float delta = baseMaxHp * percent * (newLevel - prevLevel);
                    _currentMaxHp += delta;
                    _currentHp += delta;
                    _currentHp = Mathf.Clamp(_currentHp, 0, _currentMaxHp);
                    break;
                }
            }
        }

        public virtual void ApplyStatsByCurrentValue(IStat[] stats)
        {
            ApplyStats(stats);
        }

        public virtual void ApplyPassiveSkills() { }

        public virtual string GetStatSummary() => $"MaxHp={_currentMaxHp}";

        protected virtual void ApplyStat(IStat stat)
        {
            if (stat == null) return;

            switch (stat.Type)
            {
                case StatType.MaxHp:
                    {
                        float baseMaxHp = _trainData.TrainStatusData.MaxHp;
                        float deltaHp = UtilMath.AccumulateIntDelta(ref _statMaxHpAccum, baseMaxHp * stat.Value / 100f);
                        _currentMaxHp += deltaHp;
                        _currentHp += deltaHp;
                        _currentHp = Mathf.Clamp(_currentHp, 0, _currentMaxHp);
                        break;
                    }
            }
        }

        // Elite 교체(MainTrain.ReplaceTrain) 시 기존 트레인의 누적 강화(영구 + 카드)를 새 인스턴스에 승계.
        // delta 방식: newCurrent = newBase + (oldCurrent - oldBase). HP는 비율 보존.
        // 서브클래스는 base 호출 후 자기 status struct delta를 직접 옮긴다.
        public virtual void CopyProgressFrom(Train source)
        {
            if (source == null || source._trainData == null || _trainData == null) return;

            _currentLevel = source._currentLevel;

            float oldBaseMaxHp = source._trainData.TrainStatusData.MaxHp;
            float maxHpDelta = source._currentMaxHp - oldBaseMaxHp;
            float hpRatio = source._currentMaxHp > 0 ? source._currentHp / source._currentMaxHp : 1f;

            _currentMaxHp += maxHpDelta;
            _currentHp = Mathf.Clamp(_currentMaxHp * hpRatio, 0, _currentMaxHp);

            _statMaxHpAccum = source._statMaxHpAccum;
        }
    }
}
