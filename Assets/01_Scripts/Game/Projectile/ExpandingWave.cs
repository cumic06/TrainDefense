using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class ExpandingWave : MonoBehaviour
    {
        [SerializeField]
        private CircleCollider2D waveCollider;

        [SerializeField]
        private TriggerHandle triggerHandle;

        [SerializeField]
        private Transform visualRoot;

        [SerializeField]
        [Min(0f)]
        private float startRadius = 0f;

        [SerializeField]
        [Min(0f)]
        private float endRadius = 3f;

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

        private Coroutine _expandCoroutine;
        private Vector3 _initialVisualScale = Vector3.one;
        private bool _hasCapturedVisualScale;
        private readonly HashSet<IProjectileTarget> _hitTargets = new();

        public float RequiredLifetime => expandDuration;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            _hitTargets.Clear();
            RestartWave();
        }

        private void OnDisable()
        {
            if (_expandCoroutine != null)
            {
                StopCoroutine(_expandCoroutine);
                _expandCoroutine = null;
            }

            _hitTargets.Clear();
        }

        public void SetWave(float radius)
        {
            endRadius = Mathf.Max(startRadius, radius);

            if (isActiveAndEnabled)
            {
                RestartWave();
            }
        }

        public void SetWave(float radius, float duration)
        {
            endRadius = Mathf.Max(startRadius, radius);
            expandDuration = Mathf.Max(0.01f, duration);

            if (isActiveAndEnabled)
            {
                RestartWave();
            }
        }

        private void RestartWave()
        {
            CacheReferences();

            if (_expandCoroutine != null)
            {
                StopCoroutine(_expandCoroutine);
            }

            ApplyRadius(startRadius);
            _expandCoroutine = StartCoroutine(ExpandCoroutine());
        }

        private IEnumerator ExpandCoroutine()
        {
            if (expandDuration <= 0f)
            {
                ApplyRadius(endRadius);
                DetectWaveFront(startRadius, endRadius);
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
                float currentRadius = Mathf.LerpUnclamped(startRadius, endRadius, curvedProgress);

                ApplyRadius(currentRadius);
                DetectWaveFront(previousRadius, currentRadius);
                previousRadius = currentRadius;
                yield return null;
            }

            ApplyRadius(endRadius);
            DetectWaveFront(previousRadius, endRadius);
            _expandCoroutine = null;
        }

        private void ApplyRadius(float radius)
        {
            if (waveCollider != null)
            {
                waveCollider.radius = radius;
            }

            if (visualRoot == null)
            {
                return;
            }

            float diameter = radius * 2f;
            float scaleFactor = visualBaseDiameter > 0f ? diameter / visualBaseDiameter : diameter;
            visualRoot.localScale = _initialVisualScale * scaleFactor;
        }

        private void CacheReferences()
        {
            if (waveCollider == null)
            {
                waveCollider = GetComponent<CircleCollider2D>();
            }

            if (triggerHandle == null)
            {
                triggerHandle = GetComponent<TriggerHandle>();
            }

            if (waveCollider != null)
            {
                waveCollider.isTrigger = true;
            }

            if (visualRoot != null && !_hasCapturedVisualScale)
            {
                _initialVisualScale = visualRoot.localScale;
                _hasCapturedVisualScale = true;
            }
        }

        private void Reset()
        {
            waveCollider = GetComponent<CircleCollider2D>();
            triggerHandle = GetComponent<TriggerHandle>();

            if (waveCollider != null)
            {
                waveCollider.isTrigger = true;
            }
        }

        private void DetectWaveFront(float previousRadius, float currentRadius)
        {
            if (triggerHandle == null)
            {
                return;
            }

            float minRadius = Mathf.Min(previousRadius, currentRadius);
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

                Vector2 closestPoint = col.ClosestPoint(transform.position);
                float distance = Vector2.Distance(transform.position, closestPoint);

                if (distance <= minRadius || distance > maxRadius)
                {
                    continue;
                }

                triggerHandle.ApplyWaveHit(target);
                _hitTargets.Add(target);
            }
        }
    }
}
