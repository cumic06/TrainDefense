using UnityEngine;
using Sirenix.OdinInspector;

namespace TrainDefense.Game
{
    /// <summary>
    /// 투사체 설정 데이터 (ScriptableObject)
    /// Unity Inspector에서 설정하여 재사용 가능
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileData", menuName = "Data/Projectile/ProjectileData")]
    public class ProjectileData : ScriptableObject
    {
        [BoxGroup("Movement")]
        [SerializeField]
        private MovementType movementType = MovementType.Linear;

        [BoxGroup("Movement")]
        [ShowIf("movementType", MovementType.Linear)]
        [SerializeField]
        private float speed = 10f;

        [BoxGroup("Lifecycle")]
        [SerializeField]
        private float destroyDelay = 0f;

        [BoxGroup("Lifecycle")]
        [SerializeField]
        private bool destroyOnTriggerEnter = true;

        [BoxGroup("Trigger Spawn")]
        [SerializeField]
        [LabelText("Trigger Handle 생성 여부")]
        private bool isSpawnTriggerHandle = false;

        [BoxGroup("Trigger Spawn")]
        [ShowIf("isSpawnTriggerHandle")]
        [SerializeField]
        [LabelText("생성할 Trigger Handle 프리팹")]
        private TriggerHandle triggerHandlePrefab;

        [BoxGroup("Scale")]
        [SerializeField]
        private bool scaleByAttackRange = false;

        [BoxGroup("Targeting")]
        [SerializeField]
        private bool isTargeting = false;

        [BoxGroup("Damage")]
        [SerializeField]
        private DamageType damageType = DamageType.Direct;

        [BoxGroup("Damage")]
        [ShowIf("damageType", DamageType.Tick)]
        [SerializeField]
        private float tickDamageInterval = 0.1f;

        [BoxGroup("Status Effects")]
        [SerializeField]
        private bool hasSlowEffect = false;

        [BoxGroup("Status Effects")]
        [ShowIf("hasSlowEffect")]
        [SerializeField]
        private float slowValue = 0.5f;

        [BoxGroup("Status Effects")]
        [SerializeField]
        private bool hasShoveEffect = false;

        [BoxGroup("Status Effects")]
        [ShowIf("hasShoveEffect")]
        [SerializeField]
        private float shovePower = 1f;

        [BoxGroup("Status Effects")]
        [ShowIf("hasShoveEffect")]
        [SerializeField]
        private float shoveDuration = 0.5f;

        [BoxGroup("Status Effects")]
        [SerializeField]
        private bool hasStunEffect = false;

        [BoxGroup("Status Effects")]
        [ShowIf("hasStunEffect")]
        [SerializeField]
        private float stunDuration = 0.5f;

        [BoxGroup("Warning")]
        [SerializeField]
        private bool hasWarning = false;

        [BoxGroup("Warning")]
        [ShowIf("hasWarning")]
        [SerializeField]
        private GameObject warningPrefab;

        [BoxGroup("Warning")]
        [ShowIf("hasWarning")]
        [SerializeField]
        private float warningDelaySeconds = 1f;
        [BoxGroup("Warning")]
        [ShowIf("hasWarning")]
        [SerializeField]
        private bool isScaleByAttackRange = false;

        #region Properties

        public MovementType MovementType => movementType;
        public float Speed => speed;
        public float DestroyDelay => destroyDelay;
        public bool DestroyOnTriggerEnter => destroyOnTriggerEnter;
        public bool IsSpawnTriggerHandle => isSpawnTriggerHandle;
        public TriggerHandle TriggerHandlePrefab => triggerHandlePrefab;
        public bool ScaleByAttackRange => scaleByAttackRange;
        public bool IsTargeting => isTargeting;
        public DamageType DamageType => damageType;
        public float TickDamageInterval => tickDamageInterval;
        public bool HasSlowEffect => hasSlowEffect;
        public float SlowValue => slowValue;
        public bool HasShoveEffect => hasShoveEffect;
        public float ShovePower => shovePower;
        public float ShoveDuration => shoveDuration;
        public bool HasStunEffect => hasStunEffect;
        public float StunDuration => stunDuration;
        public bool HasWarning => hasWarning;
        public GameObject WarningPrefab => warningPrefab;
        public float WarningDelaySeconds => warningDelaySeconds;
        public bool IsScaleByAttackRange => isScaleByAttackRange;

        #endregion
    }
}