using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class ExpandingWave : Projectile
    {
        #region Variables
        // 입자 속도 편차: 가장 느린 입자는 끝 반경의 70%까지만 가서 한 줄 링이 아니라 흩뿌려진 모양이 된다.
        private const float BURST_PARTICLE_MIN_SPEED_RATIO = 0.7f;
        private const int TRAVEL_RATIO_SAMPLE_COUNT = 20;

        #region Fields
        [SerializeField]
        private CircleCollider2D waveCollider;

        [SerializeField]
        [Min(0f)]
        private float startRadius = 0f;

        [SerializeField]
        [Min(0.01f)]
        private float expandDuration = 0.35f;

        [SerializeField]
        private AnimationCurve expandCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [SerializeField]
        [Min(0.01f)]
        private float visualBaseDiameter = 1f;

        [SerializeField]
        private LayerMask detectionLayerMask = ~0;

        // 파동과 함께 터뜨릴 입자(선택). 입자 수명 동안 파동 끝 반경까지 날아가도록 발사 속도를 맞춘다.
        [SerializeField]
        private ParticleSystem burstParticle;

        // 퍼지는 동안 옅어져 끝 반경에서 사라질 원판(선택). 비우면 원판이 끝까지 그대로 남는다.
        [SerializeField]
        private SpriteRenderer fadeRenderer;
        #endregion

        private float _endRadius;
        private Coroutine _expandCoroutine;
        private Vector3 _initialVisualScale = Vector3.one;
        private bool _hasCapturedVisualScale;
        private readonly HashSet<IProjectileTarget> _hitTargets = new();
        private ParticleSystem[] _burstParticleLayers;
        private float[] _burstParticleTravelRatios;
        // 소환 직후(OnEnable)엔 끝 반경을 아직 모른다(Init이 뒤에 온다). 그때 터뜨리면 속도 0으로 가운데에 뭉쳐
        // 반짝이고 사라지므로, 반경이 정해질 때까지 입자 발사를 미뤘다가 Init에서 터뜨린다.
        private bool _isBurstParticlePending;
        private Color _fadeRendererBaseColor;

        public float RequiredLifetime => expandDuration;
        #endregion

        private void Awake()
        {
            _CacheReferences();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _hitTargets.Clear();
            _RestartWave();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_expandCoroutine != null)
            {
                StopCoroutine(_expandCoroutine);
                _expandCoroutine = null;
            }

            _hitTargets.Clear();
        }

        public override void Init(float damage, IProjectileTarget owner, IProjectileTarget target = null, float attackRange = 0f, float criticalChance = 0f, float criticalDamage = 0f, float scaleRange = 0f)
        {
            if (attackRange > 0f)
            {
                _endRadius = attackRange;
            }

            if (_isBurstParticlePending && isActiveAndEnabled)
            {
                _PlayBurstParticle();
            }

            base.Init(damage, owner, target, attackRange, criticalChance, criticalDamage, scaleRange);
        }

        public void SetWave(float radius)
        {
            _endRadius = Mathf.Max(startRadius, radius);

            if (isActiveAndEnabled)
            {
                _RestartWave();
            }
        }

        public void SetWave(float radius, float duration)
        {
            _endRadius = Mathf.Max(startRadius, radius);
            expandDuration = Mathf.Max(0.01f, duration);

            if (isActiveAndEnabled)
            {
                _RestartWave();
            }
        }

        private void _RestartWave()
        {
            _CacheReferences();

            if (_expandCoroutine != null)
            {
                StopCoroutine(_expandCoroutine);
            }

            _ApplyRadius(startRadius);
            _ApplyFade(0f);
            _expandCoroutine = StartCoroutine(_ExpandCoroutine());
            _PlayBurstParticle();
        }

        private void _PlayBurstParticle()
        {
            if (burstParticle == null)
            {
                return;
            }

            if (_endRadius <= 0f)
            {
                _isBurstParticlePending = true;
                return;
            }

            _isBurstParticlePending = false;

            // 자식 입자층(파편 등)도 같은 반경까지 날아가도록 층마다 자기 수명으로 속도를 맞춘다.
            // 발사 속도가 0인 층(가운데 섬광 등)은 제자리 연출이라 건드리지 않는다.
            if (_burstParticleLayers == null)
            {
                _burstParticleLayers = burstParticle.GetComponentsInChildren<ParticleSystem>(true);
                _burstParticleTravelRatios = new float[_burstParticleLayers.Length];
                for (int i = 0; i < _burstParticleLayers.Length; i++)
                {
                    _burstParticleTravelRatios[i] = _burstParticleLayers[i].main.startSpeed.constantMax > 0f
                        ? _GetTravelRatio(_burstParticleLayers[i])
                        : 0f;
                }
            }

            for (int i = 0; i < _burstParticleLayers.Length; i++)
            {
                var main = _burstParticleLayers[i].main;
                float lifetime = main.startLifetime.constantMax;
                if (lifetime <= 0f || _burstParticleTravelRatios[i] <= 0f)
                {
                    continue;
                }

                float speed = _endRadius / (lifetime * _burstParticleTravelRatios[i]);
                main.startSpeed = new ParticleSystem.MinMaxCurve(speed * BURST_PARTICLE_MIN_SPEED_RATIO, speed);
            }

            burstParticle.Clear();
            burstParticle.Play();
        }

        // 수명 동안 실제로 가는 거리 ÷ (발사 속도 × 수명). 감속 곡선(속도 배율)이 없으면 1,
        // 1→0 직선 감속이면 0.5 — 파동의 감속 곡선(0→1, 시작 기울기 2)과 같은 궤적이 된다.
        private static float _GetTravelRatio(ParticleSystem layer)
        {
            var velocity = layer.velocityOverLifetime;
            if (!velocity.enabled || velocity.speedModifier.mode != ParticleSystemCurveMode.Curve)
            {
                return 1f;
            }

            var speedCurve = velocity.speedModifier.curve;
            float sum = 0f;
            for (int step = 0; step < TRAVEL_RATIO_SAMPLE_COUNT; step++)
            {
                sum += speedCurve.Evaluate((step + 0.5f) / TRAVEL_RATIO_SAMPLE_COUNT);
            }

            return Mathf.Max(0.01f, sum / TRAVEL_RATIO_SAMPLE_COUNT * velocity.speedModifierMultiplier);
        }

        private IEnumerator _ExpandCoroutine()
        {
            if (expandDuration <= 0f)
            {
                _ApplyRadius(_endRadius);
                _DetectWaveFront(startRadius, _endRadius);
                _expandCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            float previousRadius = startRadius;

            while (elapsed < expandDuration)
            {
                elapsed += Time.deltaTime;

                float progress = Mathf.Clamp01(elapsed / expandDuration);
                float curvedProgress = expandCurve != null ? expandCurve.Evaluate(progress) : progress;
                float currentRadius = Mathf.LerpUnclamped(startRadius, _endRadius, curvedProgress);

                _ApplyRadius(currentRadius);
                _ApplyFade(progress);
                _DetectWaveFront(previousRadius, currentRadius);
                previousRadius = currentRadius;
                yield return null;
            }

            _ApplyRadius(_endRadius);
            _ApplyFade(1f);
            _DetectWaveFront(previousRadius, _endRadius);
            _expandCoroutine = null;
        }

        private void _ApplyRadius(float radius)
        {
            float diameter = radius * 2f;
            float scaleFactor = visualBaseDiameter > 0f ? diameter / visualBaseDiameter : diameter;

            transform.localScale = _initialVisualScale * scaleFactor;
        }

        // 처음엔 진하게 버티다 끝에서 빠르게 사라진다(1 - 진행²).
        private void _ApplyFade(float progress)
        {
            if (fadeRenderer == null)
            {
                return;
            }

            Color color = _fadeRendererBaseColor;
            color.a *= 1f - progress * progress;
            fadeRenderer.color = color;
        }

        private void _CacheReferences()
        {
            if (waveCollider == null)
            {
                waveCollider = GetComponent<CircleCollider2D>();
            }

            if (waveCollider != null)
            {
                waveCollider.isTrigger = true;
            }

            if (!_hasCapturedVisualScale)
            {
                _initialVisualScale = transform.localScale;
                if (fadeRenderer != null)
                {
                    _fadeRendererBaseColor = fadeRenderer.color;
                }
                _hasCapturedVisualScale = true;
            }
        }

        // 파도 확장 방식으로 데미지를 주므로 Projectile의 트리거 무효화
        private void OnTriggerEnter2D(Collider2D other) { }
        private void OnTriggerStay2D(Collider2D other) { }
        private void OnTriggerExit2D(Collider2D other) { }

        private void Reset()
        {
            waveCollider = GetComponent<CircleCollider2D>();

            if (waveCollider != null)
            {
                waveCollider.isTrigger = true;
            }
        }

        private void _DetectWaveFront(float previousRadius, float currentRadius)
        {
            float maxRadius = Mathf.Max(previousRadius, currentRadius);

            if (maxRadius <= 0f)
            {
                return;
            }

            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, maxRadius, detectionLayerMask);

            foreach (var col in colliders)
            {
                if (col == null || !col.TryGetComponent<Monster>(out var monster))
                {
                    continue;
                }

                IProjectileTarget target = monster;
                if (!target.IsActive || _hitTargets.Contains(target))
                {
                    continue;
                }

                _hitTargets.Add(target);
                ProcessEnter(target);
            }
        }
    }
}
