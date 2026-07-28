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
        private bool scaleByRange = false;

        [BoxGroup("Scale")]
        [ShowIf("scaleByRange")]
        [SerializeField]
        private ScaleByRangeType scaleRangeType;

        [BoxGroup("Targeting")]
        [SerializeField]
        private bool isTargeting = false;

        [BoxGroup("Visual")]
        [SerializeField]
        [LabelText("모델 회전 여부 (false면 발사 방향 무관 직립 유지)")]
        private bool isRotateModel = true;

        [BoxGroup("Damage")]
        [SerializeField]
        [LabelText("포탑에서 직접 데미지 (이펙트 전용 프로젝타일)")]
        private bool directDamage = false;

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
        [ShowIf("hasSlowEffect")]
        [SerializeField]
        [Tooltip("Stay 슬로우의 지속시간(초). 자동복원 슬로우를 매 Stay마다 갱신하므로 장판 안에서는 유지되고, 장판이 꺼지면(버스트 종료) 이 시간 후 자연 해제된다. 0이면 옛 갱신형(이탈 시에만 해제).")]
        private float slowDuration = 0.1f;

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

        [BoxGroup("Pierce")]
        [SerializeField]
        private bool pierce = false;

        [BoxGroup("Pierce")]
        [ShowIf("pierce")]
        [SerializeField]
        [Min(1)]
        private int maxPenetration = 1;

        [BoxGroup("Spread")]
        [SerializeField]
        [Min(0f)]
        private float spreadAngle = 0f;

        [BoxGroup("Camera Shake")]
        [SerializeField]
        private bool shakeOnDestroy = false;

        [BoxGroup("Camera Shake")]
        [ShowIf("shakeOnDestroy")]
        [SerializeField]
        private float shakeIntensity = 0.5f;

        [BoxGroup("Camera Shake")]
        [ShowIf("shakeOnDestroy")]
        [SerializeField]
        private float shakeDuration = 0.2f;

        #region Properties

        public bool DirectDamage => directDamage;
        public MovementType MovementType => movementType;
        public float Speed => speed;
        public float DestroyDelay => destroyDelay;
        public bool DestroyOnTriggerEnter => destroyOnTriggerEnter;
        public bool IsSpawnTriggerHandle => isSpawnTriggerHandle;
        public TriggerHandle TriggerHandlePrefab => triggerHandlePrefab;
        public bool ScaleByArea => scaleByRange;
        public ScaleByRangeType ScaleRangeType => scaleRangeType;
        public bool IsTargeting => isTargeting;
        public bool IsRotateModel => isRotateModel;
        public DamageType DamageType => damageType;
        public float TickDamageInterval => tickDamageInterval;
        public bool HasSlowEffect => hasSlowEffect;
        public float SlowValue => slowValue;
        public float SlowDuration => slowDuration;
        public bool HasShoveEffect => hasShoveEffect;
        public float ShovePower => shovePower;
        public float ShoveDuration => shoveDuration;
        public bool HasStunEffect => hasStunEffect;
        public float StunDuration => stunDuration;
        public bool Pierce => pierce;
        public int MaxPenetration => maxPenetration;
        public float SpreadAngle => spreadAngle;
        public bool ShakeOnDestroy => shakeOnDestroy;
        public float ShakeIntensity => shakeIntensity;
        public float ShakeDuration => shakeDuration;

        #endregion
    }
}