using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.TrackEvent
{
    /// <summary>
    /// 자연환경 선로 이벤트(모래폭풍·한파·순풍)의 화면 연출. 카메라에 붙어 따라다니며
    /// 화면 전체 색 틴트 1장 + 흩날리는 입자 스프라이트 N개를 그린다.
    /// 프리팹 없이 런타임에 만든다(SupplyCrate와 같은 방식) — 텍스처도 코드로 생성해 에셋 의존이 없다.
    /// 시간은 Time.deltaTime 기준이라 일시정지·레벨업 창(timeScale 0) 동안에는 입자도 멈춘다.
    /// 효과(ITrackEventEffect)가 Begin에서 Create, End에서 FadeOutAndDestroy를 부른다.
    /// </summary>
    public class TrackEventWeatherEffect : MonoBehaviour
    {
        #region Variables

        /// <summary>연출 한 종류의 모양 정의.</summary>
        public struct WeatherStyle
        {
            public Color TintColor;          // 화면 전체 틴트 (alpha 0이면 틴트 없음)
            public Color ParticleColor;
            public int ParticleCount;
            public Vector2 Velocity;         // 입자 기본 이동 속도 (월드 유닛/초)
            public float VelocityJitter;     // 입자마다 속도 크기에 곱할 랜덤 폭 (0.3 → 0.7~1.3배)
            public Vector2 ParticleSize;     // 입자 크기 (x가 길면 바람 줄기 모양)
            public float SwayAmplitude;      // 진행 방향에 수직으로 흔들리는 폭 (눈송이)
            public float SwayFrequency;
            public float AutoFadeDelay;      // 0보다 크면 이 시간 뒤 스스로 사라진다 (즉발형 이벤트용)
        }

        private const string OBJECT_NAME = "TrackEventWeather";
        // Default 레이어는 Map보다 아래라 안 보인다 — 유닛·탄 위, 보상·UI 아래인 WarningEffect 레이어에 그린다.
        private const string SORTING_LAYER_NAME = "WarningEffect";
        private const int TINT_SORTING_ORDER = -100;
        private const int PARTICLE_SORTING_ORDER = -50;
        private const float CAMERA_FORWARD_OFFSET = 1f;
        // WeatherStyle의 크기·속도는 orthographicSize 10 기준 값 — 실제 카메라 크기에 비례해 키운다(줌이 바뀌어도 화면상 크기 유지).
        private const float REFERENCE_ORTHOGRAPHIC_SIZE = 10f;
        private const float FADE_IN_DURATION = 0.6f;
        private const float FADE_OUT_DURATION = 0.8f;
        private const float VIEW_MARGIN = 1.5f;
        private const int SOFT_DOT_TEXTURE_SIZE = 16;
        private const int TINT_TEXTURE_SIZE = 4;

        private static Sprite _softDotSprite;
        private static Sprite _tintSprite;

        #endregion

        #region Fields

        private readonly List<Transform> _particles = new List<Transform>();
        private readonly List<SpriteRenderer> _particleRenderers = new List<SpriteRenderer>();
        private readonly List<Vector2> _particleVelocities = new List<Vector2>();
        private readonly List<float> _particlePhases = new List<float>();

        private WeatherStyle _style;
        private Camera _camera;
        private SpriteRenderer _tintRenderer;
        private Vector2 _swayAxis;
        private float _viewScale = 1f;
        private float _elapsedTime;
        private float _alpha;
        private float _fadeDirection = 1f;

        #endregion

        #region LifeCycle

        private void Update()
        {
            if (_camera == null)
            {
                Destroy(gameObject);

                return;
            }

            float deltaTime = Time.deltaTime;
            _elapsedTime += deltaTime;

            if (_style.AutoFadeDelay > 0f && _fadeDirection > 0f && _elapsedTime >= _style.AutoFadeDelay)
                _fadeDirection = -1f;

            _UpdateAlpha(deltaTime);

            if (_alpha <= 0f && _fadeDirection < 0f)
            {
                Destroy(gameObject);

                return;
            }

            _FollowCamera();
            _UpdateParticles(deltaTime);
        }

        #endregion

        /// <summary>카메라 앞에 연출을 띄운다. 카메라가 없으면 null.</summary>
        public static TrackEventWeatherEffect Create(WeatherStyle style)
        {
            Camera camera = Camera.main;

            if (camera == null)
                return null;

            var go = new GameObject(OBJECT_NAME);
            TrackEventWeatherEffect effect = go.AddComponent<TrackEventWeatherEffect>();
            effect._Initialize(style, camera);

            return effect;
        }

        /// <summary>서서히 사라진 뒤 스스로 파괴된다. 이미 파괴됐으면 아무것도 안 한다.</summary>
        public void FadeOutAndDestroy()
        {
            _fadeDirection = -1f;
        }

        private void _Initialize(WeatherStyle style, Camera camera)
        {
            _style = style;
            _camera = camera;
            _alpha = 0f;
            _viewScale = Mathf.Max(0.1f, camera.orthographicSize / REFERENCE_ORTHOGRAPHIC_SIZE);

            Vector2 direction = style.Velocity.sqrMagnitude > 0f ? style.Velocity.normalized : Vector2.down;
            _swayAxis = new Vector2(-direction.y, direction.x);

            _FollowCamera();
            _CreateTint();
            _CreateParticles();
            _ApplyAlpha();
        }

        private void _CreateTint()
        {
            if (_style.TintColor.a <= 0f)
                return;

            var tintObject = new GameObject("Tint");
            tintObject.transform.SetParent(transform, false);
            _tintRenderer = tintObject.AddComponent<SpriteRenderer>();
            _tintRenderer.sprite = _GetTintSprite();
            _tintRenderer.sortingLayerName = SORTING_LAYER_NAME;
            _tintRenderer.sortingOrder = TINT_SORTING_ORDER;

            Vector2 viewSize = _GetViewSize() + Vector2.one * VIEW_MARGIN * 2f;
            tintObject.transform.localScale = new Vector3(viewSize.x, viewSize.y, 1f);
        }

        private void _CreateParticles()
        {
            Vector2 halfView = _GetViewSize() * 0.5f + Vector2.one * VIEW_MARGIN;
            Sprite sprite = _GetSoftDotSprite();

            for (int i = 0; i < _style.ParticleCount; i++)
            {
                var particleObject = new GameObject("Particle");
                particleObject.transform.SetParent(transform, false);
                particleObject.transform.localPosition = new Vector3(Random.Range(-halfView.x, halfView.x), Random.Range(-halfView.y, halfView.y), 0f);
                particleObject.transform.localScale = new Vector3(_style.ParticleSize.x * _viewScale, _style.ParticleSize.y * _viewScale, 1f);

                // 줄기 모양(가로로 긴 입자)은 진행 방향으로 눕힌다.
                float angle = Mathf.Atan2(_style.Velocity.y, _style.Velocity.x) * Mathf.Rad2Deg;
                particleObject.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

                SpriteRenderer spriteRenderer = particleObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = sprite;
                spriteRenderer.sortingLayerName = SORTING_LAYER_NAME;
                spriteRenderer.sortingOrder = PARTICLE_SORTING_ORDER;

                float speedScale = 1f + Random.Range(-_style.VelocityJitter, _style.VelocityJitter);

                _particles.Add(particleObject.transform);
                _particleRenderers.Add(spriteRenderer);
                _particleVelocities.Add(_style.Velocity * (speedScale * _viewScale));
                _particlePhases.Add(Random.Range(0f, Mathf.PI * 2f));
            }
        }

        // 카메라 로컬 좌표로 이동시키고, 화면 밖으로 나가면 반대편으로 되감아 개수를 유지한다.
        private void _UpdateParticles(float deltaTime)
        {
            Vector2 halfView = _GetViewSize() * 0.5f + Vector2.one * VIEW_MARGIN;

            for (int i = 0; i < _particles.Count; i++)
            {
                Transform particle = _particles[i];
                Vector3 position = particle.localPosition;
                float sway = _style.SwayAmplitude > 0f
                    ? Mathf.Cos(_elapsedTime * _style.SwayFrequency + _particlePhases[i]) * _style.SwayAmplitude * _style.SwayFrequency * _viewScale
                    : 0f;

                Vector2 step = (_particleVelocities[i] + _swayAxis * sway) * deltaTime;
                position.x = _Wrap(position.x + step.x, halfView.x);
                position.y = _Wrap(position.y + step.y, halfView.y);
                particle.localPosition = position;
            }
        }

        private void _UpdateAlpha(float deltaTime)
        {
            float duration = _fadeDirection > 0f ? FADE_IN_DURATION : FADE_OUT_DURATION;
            float previousAlpha = _alpha;
            _alpha = Mathf.Clamp01(_alpha + _fadeDirection * deltaTime / duration);

            if (!Mathf.Approximately(previousAlpha, _alpha))
                _ApplyAlpha();
        }

        private void _ApplyAlpha()
        {
            if (_tintRenderer != null)
            {
                Color tint = _style.TintColor;
                tint.a *= _alpha;
                _tintRenderer.color = tint;
            }

            Color particleColor = _style.ParticleColor;
            particleColor.a *= _alpha;

            foreach (SpriteRenderer particleRenderer in _particleRenderers)
                particleRenderer.color = particleColor;
        }

        private void _FollowCamera()
        {
            Transform cameraTransform = _camera.transform;
            transform.position = cameraTransform.position + cameraTransform.forward * (Mathf.Abs(cameraTransform.position.z) - CAMERA_FORWARD_OFFSET);
        }

        private Vector2 _GetViewSize()
        {
            float height = _camera.orthographicSize * 2f;

            return new Vector2(height * _camera.aspect, height);
        }

        private static float _Wrap(float value, float halfRange)
        {
            if (value > halfRange)
                return value - halfRange * 2f;

            if (value < -halfRange)
                return value + halfRange * 2f;

            return value;
        }

        // 가운데가 진하고 가장자리가 투명한 원 — 모래 알갱이·눈송이·바람 줄기(가로로 늘림)에 공용으로 쓴다.
        private static Sprite _GetSoftDotSprite()
        {
            if (_softDotSprite != null)
                return _softDotSprite;

            var texture = new Texture2D(SOFT_DOT_TEXTURE_SIZE, SOFT_DOT_TEXTURE_SIZE, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            float center = (SOFT_DOT_TEXTURE_SIZE - 1) * 0.5f;

            for (int y = 0; y < SOFT_DOT_TEXTURE_SIZE; y++)
            {
                for (int x = 0; x < SOFT_DOT_TEXTURE_SIZE; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float alpha = Mathf.Clamp01(1f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }

            texture.Apply();
            _softDotSprite = Sprite.Create(texture, new Rect(0f, 0f, SOFT_DOT_TEXTURE_SIZE, SOFT_DOT_TEXTURE_SIZE), new Vector2(0.5f, 0.5f), SOFT_DOT_TEXTURE_SIZE);

            return _softDotSprite;
        }

        // 1유닛 크기 흰 사각형 — 화면 크기로 늘려 틴트로 쓴다.
        private static Sprite _GetTintSprite()
        {
            if (_tintSprite != null)
                return _tintSprite;

            var texture = new Texture2D(TINT_TEXTURE_SIZE, TINT_TEXTURE_SIZE, TextureFormat.RGBA32, false);
            var pixels = new Color[TINT_TEXTURE_SIZE * TINT_TEXTURE_SIZE];

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;

            texture.SetPixels(pixels);
            texture.Apply();
            _tintSprite = Sprite.Create(texture, new Rect(0f, 0f, TINT_TEXTURE_SIZE, TINT_TEXTURE_SIZE), new Vector2(0.5f, 0.5f), TINT_TEXTURE_SIZE);

            return _tintSprite;
        }
    }
}
