using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class ExpandingWave : Projectile
    {
        #region Variables

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
        #endregion

        private float _endRadius;
        private Coroutine _expandCoroutine;
        private Vector3 _initialVisualScale = Vector3.one;
        private bool _hasCapturedVisualScale;
        private readonly HashSet<IProjectileTarget> _hitTargets = new();

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

        public override void Init(int damage, IProjectileTarget target = null, float attackRange = 0f)
        {
            if (attackRange > 0f)
            {
                _endRadius = attackRange;
            }

            base.Init(damage, target, attackRange);
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
            _expandCoroutine = StartCoroutine(_ExpandCoroutine());
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
                _DetectWaveFront(previousRadius, currentRadius);
                previousRadius = currentRadius;
                yield return null;
            }

            _ApplyRadius(_endRadius);
            _DetectWaveFront(previousRadius, _endRadius);
            _expandCoroutine = null;
        }

        private void _ApplyRadius(float radius)
        {
            float diameter = radius * 2f;
            float scaleFactor = visualBaseDiameter > 0f ? diameter / visualBaseDiameter : diameter;

            transform.localScale = _initialVisualScale * scaleFactor;
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
                if (col == null || !col.TryGetComponent<IProjectileTarget>(out var target))
                {
                    continue;
                }

                if (target == null || !target.IsActive || _hitTargets.Contains(target))
                {
                    continue;
                }

                _hitTargets.Add(target);
                ProcessEnter(target);
            }
        }
    }
}
