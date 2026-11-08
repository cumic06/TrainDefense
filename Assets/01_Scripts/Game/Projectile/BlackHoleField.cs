using System.Collections.Generic;
using Cumic.Events;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 블랙홀 포탑이 착탄한 자리에 남는 장판. 지속 피해는 같은 오브젝트의 TriggerHandle(Tick)이 맡고,
    /// 이 컴포넌트는 범위 안 적을 중심으로 끌어당기는 것과 개조 변형(떠돌기·붕괴·중력 증폭·화이트홀)을 맡는다.
    /// 착탄 투사체가 Init을 불러야 동작한다(Init 전에는 아무것도 하지 않는다).
    /// </summary>
    [RequireComponent(typeof(TriggerHandle))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class BlackHoleField : MonoBehaviour
    {
        #region Variables
        // 중심에서 반경의 이 비율 안쪽까지 끌려온 적은 더 당기지 않는다(한 점에 겹쳐 떨리는 것 방지).
        private const float CORE_RADIUS_RATIO = 0.15f;
        // 장판 수명이 끝나 붕괴 폭발을 낸 뒤 TriggerHandle이 오브젝트를 지우기까지의 여유.
        private const float DESPAWN_MARGIN = 0.05f;
        private const float COLLAPSE_SHAKE_INTENSITY = 1f;
        private const float COLLAPSE_SHAKE_DURATION = 0.15f;
        #endregion

        #region Fields
        [SerializeField]
        [Tooltip("적을 중심으로 당기는 속도(유닛/초). 걷는 속도와 합쳐진다. 일반 몬스터 이동속도(약 4)보다 커야 장판 안에 붙잡힌다.")]
        private float pullSpeed = 5f;
        [SerializeField]
        [Tooltip("개조로 덮어쓰지 않을 때의 장판 유지 시간(초).")]
        private float duration = 2.5f;
        [SerializeField]
        [Tooltip("회전시킬 그림(자식).")]
        private Transform visual;
        [SerializeField]
        private float visualSpinSpeed = -480f;
        [SerializeField]
        [Tooltip("장판이 깔린 동안 그림이 커졌다 작아지는 폭(기본 크기 대비 비율). 0이면 맥동하지 않는다.")]
        private float pulseScaleAmount = 0.15f;
        [SerializeField]
        [Tooltip("한 번 커졌다 작아지는 데 걸리는 시간(초).")]
        private float pulseDuration = 0.8f;
        [SerializeField]
        [Tooltip("착탄 직후 그림이 이 비율(기본 크기 대비)의 작은 점에서 시작해 커지며 소환된다.")]
        private float growStartScale = 0.15f;
        [SerializeField]
        [Tooltip("작은 점에서 기본 크기까지 커지는 데 걸리는 시간(초). 0이면 바로 기본 크기로 나타난다.")]
        private float growDuration = 0.35f;
        [SerializeField]
        [Tooltip("개조 없는 기본 블랙홀 장판 그림.")]
        private Sprite blackHoleSprite;
        [SerializeField]
        [Tooltip("화이트홀 개조 시 장판 그림.")]
        private Sprite whiteHoleSprite;
        [SerializeField]
        [Tooltip("붕괴 개조가 사라질 때 터뜨릴 폭발 장판(공격력 피해형).")]
        private TriggerHandle collapseTriggerPrefab;

        private TriggerHandle _triggerHandle;
        private CircleCollider2D _area;
        private SpriteRenderer _visualRenderer;
        private Vector3 _visualBaseScale = Vector3.one;
        private float _pulseElapsedTime;
        private float _growElapsedTime;
        private TurretTrain _owner;
        private BlackHoleModifier _modifier;
        private Vector2 _driftDirection;
        private float _lifetime;
        private float _elapsedTime;
        private bool _isActive;
        private bool _isHoldingSound;
        private readonly List<Collider2D> _overlapResults = new();
        private ContactFilter2D _contactFilter;
        #endregion

        #region LifeCycle
        private void OnEnable()
        {
            _isActive = false;
            _owner = null;
            _modifier = null;
            _elapsedTime = 0f;
            _pulseElapsedTime = 0f;
            _growElapsedTime = 0f;

            // 풀 재사용 시 이전 맥동 크기가 남지 않게 소환 시작 크기로 되돌린다.
            if (visual != null)
                visual.localScale = _visualBaseScale * _GetGrowScale();
        }

        // 수명 전에 풀로 돌아가는 경우(판 종료 등)에도 반복음 몫을 내려놓는다.
        private void OnDisable()
        {
            _ReleaseSound();
        }

        private void Awake()
        {
            _triggerHandle = GetComponent<TriggerHandle>();
            _area = GetComponent<CircleCollider2D>();

            if (visual != null)
            {
                _visualRenderer = visual.GetComponent<SpriteRenderer>();
                _visualBaseScale = visual.localScale;
            }

            _contactFilter = ContactFilter2D.noFilter;
        }

        private void FixedUpdate()
        {
            if (!_isActive)
                return;

            float deltaTime = Time.fixedDeltaTime;
            _elapsedTime += deltaTime;

            if (_modifier.DriftSpeed > 0f)
                transform.position += (Vector3)(_driftDirection * (_modifier.DriftSpeed * deltaTime));

            _PullMonsters(deltaTime);

            if (_elapsedTime >= _lifetime)
                _Collapse();
        }

        private void Update()
        {
            if (visual == null)
                return;

            visual.Rotate(0f, 0f, visualSpinSpeed * Time.deltaTime);
            _growElapsedTime += Time.deltaTime;
            _UpdatePulse();
        }
        #endregion

        /// <summary>
        /// 착탄 직후 투사체가 부른다. owner가 블랙홀 포탑이 아니면 개조 없는 기본 블랙홀로 동작한다.
        /// </summary>
        public void Init(IProjectileTarget owner, Vector2 flightDirection)
        {
            _owner = owner as TurretTrain;
            _modifier = _owner != null ? _owner.BlackHole : new BlackHoleModifier();
            _driftDirection = flightDirection.sqrMagnitude > 0f ? flightDirection.normalized : Vector2.right;
            _lifetime = _modifier.LifetimeOverride > 0f ? _modifier.LifetimeOverride : duration;
            _elapsedTime = 0f;
            _growElapsedTime = 0f;
            _isActive = true;
            _ApplyVisualSprite();

            // 블랙홀 반복음은 장판이 생성된 이 시점부터 난다(날아가는 총알은 소리를 내지 않는다).
            if (!_isHoldingSound)
            {
                _isHoldingSound = true;
                BlackHoleSoundLoop.Acquire();
            }

            _triggerHandle.OverrideLifetime(_lifetime + DESPAWN_MARGIN);
        }

        private void _ReleaseSound()
        {
            if (!_isHoldingSound)
                return;

            _isHoldingSound = false;
            BlackHoleSoundLoop.Release();
        }

        // 화이트홀 개조면 화이트홀 그림, 아니면 블랙홀 그림. 풀 재사용 때마다 Init에서 다시 고른다.
        private void _ApplyVisualSprite()
        {
            if (_visualRenderer == null)
                return;

            Sprite sprite = _modifier.IsRepulse ? whiteHoleSprite : blackHoleSprite;

            if (sprite != null)
                _visualRenderer.sprite = sprite;
        }

        // 장판이 깔린 동안 그림이 사인 곡선으로 커졌다 작아진다. 콜라이더(당기는 범위)는 그대로 두고 그림만 움직인다.
        // 스케일드 시간이라 일시정지·상점(timeScale 0) 동안에는 맥동도 멈춘다.
        // 소환 직후엔 작은 점에서 커지는 배율(_GetGrowScale)이 곱해진다.
        private void _UpdatePulse()
        {
            float pulse = 1f;

            if (pulseScaleAmount > 0f && pulseDuration > 0f)
            {
                _pulseElapsedTime += Time.deltaTime;
                pulse += Mathf.Sin(_pulseElapsedTime / pulseDuration * Mathf.PI * 2f) * pulseScaleAmount;
            }

            visual.localScale = _visualBaseScale * (_GetGrowScale() * pulse);
        }

        // 작은 점(growStartScale) → 기본 크기(1). 끝에서 감속(ease-out)해 '빨려 들어가듯 펼쳐지는' 느낌을 낸다.
        private float _GetGrowScale()
        {
            if (growDuration <= 0f)
                return 1f;

            float progress = Mathf.Clamp01(_growElapsedTime / growDuration);
            float eased = 1f - (1f - progress) * (1f - progress) * (1f - progress);

            return Mathf.Lerp(growStartScale, 1f, eased);
        }

        private float _GetWorldRadius() => _area.radius * Mathf.Abs(transform.lossyScale.x);

        private void _PullMonsters(float deltaTime)
        {
            Vector2 center = transform.position;
            float radius = _GetWorldRadius();
            float coreRadius = radius * CORE_RADIUS_RATIO;
            float step = pullSpeed * _modifier.PullMultiplier * deltaTime;

            if (step <= 0f)
                return;

            _overlapResults.Clear();
            Physics2D.OverlapCircle(center, radius, _contactFilter, _overlapResults);

            foreach (var hit in _overlapResults)
            {
                if (hit == null || !hit.TryGetComponent(out Monster monster))
                    continue;
                if (monster.IsDead)
                    continue;

                Vector2 monsterPosition = monster.transform.position;

                if (_modifier.IsRepulse)
                {
                    // 화이트홀: 포탑에서 멀어지는 쪽으로 민다. (포탑이 없으면 장판 중심 바깥쪽)
                    Vector2 origin = _owner != null ? (Vector2)_owner.transform.position : center;
                    Vector2 away = monsterPosition - origin;
                    if (away.sqrMagnitude < 0.0001f)
                        away = monsterPosition - center;

                    monster.Pull(away.normalized * step);
                    continue;
                }

                Vector2 toCenter = center - monsterPosition;
                float distance = toCenter.magnitude;
                if (distance <= coreRadius)
                    continue;

                monster.Pull(toCenter / distance * Mathf.Min(step, distance - coreRadius));
            }
        }

        private void _Collapse()
        {
            _isActive = false;
            _ReleaseSound();

            if (_modifier.CollapseDamageMultiplier <= 0f || collapseTriggerPrefab == null)
                return;
            if (_owner == null || _owner.IsDead)
                return;

            TriggerHandle explosion = ResourceManager.Instance.Spawn(collapseTriggerPrefab, transform.position, Quaternion.identity);
            if (explosion == null)
                return;

            // 폭발 장판 콜라이더 반경을 블랙홀 반경에 맞춘다. (Projectile.TrySpawnTriggerHandle과 같은 보정)
            var explosionArea = explosion.GetComponent<CircleCollider2D>();
            float baseRadius = explosionArea != null && explosionArea.radius > 0f ? explosionArea.radius : 1f;
            float scale = _GetWorldRadius() / baseRadius;
            explosion.transform.localScale = new Vector3(scale, scale, 1f);
            explosion.Init(_owner.CurrentAttackDamage * _modifier.CollapseDamageMultiplier, _owner);

            GameEventSystem.Publish(new CameraShakeEvent(COLLAPSE_SHAKE_INTENSITY, COLLAPSE_SHAKE_DURATION));
        }
    }
}
