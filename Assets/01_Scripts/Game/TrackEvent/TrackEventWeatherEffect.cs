using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.TrackEvent
{
    /// <summary>
    /// 자연환경 선로 이벤트(모래폭풍·한파·순풍·비·안개·폭염)의 화면 연출. 카메라에 붙어 따라다니며
    /// 화면 전체 색 틴트 1장 + ParticleSystem 레이어 N개(뒤/앞 깊이감) + 선택적 번쩍임(번개)을 그린다.
    /// 프리팹 없이 런타임에 만든다(SupplyCrate와 같은 방식) — 입자 텍스처도 코드로 생성해 에셋 의존이 없다.
    /// 입자는 픽셀 아트 톤에 맞춰 점(Point) 필터 픽셀 모양을 쓰고, 안개·먼지 덩어리만 부드러운 원을 쓴다.
    /// 시간은 스케일드 시간 기준이라 일시정지·레벨업 창(timeScale 0) 동안에는 입자도 멈춘다.
    /// 효과(ITrackEventEffect)가 Begin에서 Create, End에서 FadeOutAndDestroy를 부른다.
    /// </summary>
    public class TrackEventWeatherEffect : MonoBehaviour
    {
        #region Variables

        /// <summary>입자 텍스처 모양.</summary>
        public enum ParticleShape
        {
            SoftDot,     // 가운데가 진한 작은 원 — 늘이면(IsStretched) 빗줄기·바람 줄기
            PixelSquare, // 단단한 픽셀 한 점 — 모래 알갱이·불티·잔눈
            Snowflake,   // 7x7 픽셀 눈송이
            Cloud,       // 크고 부드러운 덩어리 — 안개·먼지 구름
        }

        /// <summary>파티클 레이어 한 겹의 정의. 크기·속도는 orthographicSize 10 기준 값이다.</summary>
        public struct ParticleLayer
        {
            public ParticleShape Shape;
            public Color ColorMin;           // 입자마다 ColorMin~ColorMax 사이 랜덤 색
            public Color ColorMax;
            public int MaxParticles;         // 화면에 동시에 떠 있는 입자 수
            public float Lifetime;
            public Vector2 Velocity;         // 기본 이동 속도 (월드 유닛/초)
            public float VelocityJitter;     // 속도에 곱할 랜덤 폭 (0.3 → 0.7~1.3배)
            public Vector2 SizeRange;        // 입자 크기 최소~최대
            public bool IsStretched;         // 진행 방향으로 늘인다 (빗줄기·바람 줄기)
            public float StretchLength;      // 늘일 때 속도에 곱하는 길이 배율
            public float NoiseStrength;      // 0보다 크면 노이즈로 흔들린다 (눈송이·아지랑이·모래)
            public float NoiseFrequency;
            public float RotationSpeed;      // 초당 회전 각도 ±범위 (눈송이)
            public float FadeEdge;           // 수명 앞·뒤 몇 % 동안 알파가 차오르고 빠지는지 (0~0.5)
            public int SortingOrder;
        }

        /// <summary>연출 한 종류의 모양 정의.</summary>
        public struct WeatherStyle
        {
            public Color TintColor;          // 화면 전체 틴트 (alpha 0이면 틴트 없음)
            public ParticleLayer[] Layers;
            public float FlashInterval;      // 0보다 크면 대략 이 간격으로 화면이 번쩍인다 (번개)
            public float AutoFadeDelay;      // 0보다 크면 이 시간 뒤 스스로 사라진다 (즉발형 이벤트용)
        }

        private const string OBJECT_NAME = "TrackEventWeather";
        // Default 레이어는 Map보다 아래라 안 보인다 — 유닛·탄 위, 보상·UI 아래인 WarningEffect 레이어에 그린다.
        private const string SORTING_LAYER_NAME = "WarningEffect";
        private const string PRIMARY_SHADER_NAME = "Sprites/Default";   // GraphicsSettings 항상 포함 셰이더 — 빌드에서도 Find 가능
        private const string FALLBACK_SHADER_NAME = "Universal Render Pipeline/Particles/Unlit";
        private const int TINT_SORTING_ORDER = -100;
        private const int FLASH_SORTING_ORDER = -40;
        private const float CAMERA_FORWARD_OFFSET = 1f;
        // 크기·속도는 orthographicSize 10 기준 값 — 실제 카메라 크기에 비례해 키운다(줌이 바뀌어도 화면상 크기 유지).
        private const float REFERENCE_ORTHOGRAPHIC_SIZE = 10f;
        private const float FADE_IN_DURATION = 0.6f;
        private const float FADE_OUT_DURATION = 0.8f;
        private const float VIEW_MARGIN = 1.5f;
        private const float MIN_PARTICLE_LIFETIME = 0.1f;
        private const float FLASH_PEAK_ALPHA = 0.35f;
        private const float FLASH_DURATION = 0.35f;
        private const float FLASH_INTERVAL_JITTER = 0.4f;
        private const int SOFT_DOT_TEXTURE_SIZE = 16;
        private const int CLOUD_TEXTURE_SIZE = 32;
        private const int PIXEL_SQUARE_TEXTURE_SIZE = 2;
        private const int TINT_TEXTURE_SIZE = 4;

        private static readonly string[] SNOWFLAKE_PATTERN =
        {
            "...#...",
            ".#.#.#.",
            "..###..",
            "#######",
            "..###..",
            ".#.#.#.",
            "...#...",
        };

        private static readonly Dictionary<ParticleShape, Texture2D> _shapeTextures = new Dictionary<ParticleShape, Texture2D>();
        private static Sprite _tintSprite;

        #endregion

        #region Fields

        private readonly List<ParticleSystem> _particleSystems = new List<ParticleSystem>();
        private readonly List<Material> _layerMaterials = new List<Material>();

        private WeatherStyle _style;
        private Camera _camera;
        private SpriteRenderer _tintRenderer;
        private SpriteRenderer _flashRenderer;
        private float _viewScale = 1f;
        private float _elapsedTime;
        private float _alpha;
        private float _fadeDirection = 1f;
        private float _flashCooldown;
        private float _flashElapsedTime = FLASH_DURATION;

        #endregion

        #region LifeCycle

        private void OnDestroy()
        {
            foreach (Material material in _layerMaterials)
            {
                if (material != null)
                    Destroy(material);
            }

            _layerMaterials.Clear();
        }

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
            _UpdateFlash(deltaTime);
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

            foreach (ParticleSystem particleSystem in _particleSystems)
            {
                if (particleSystem != null)
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void _Initialize(WeatherStyle style, Camera camera)
        {
            _style = style;
            _camera = camera;
            _alpha = 0f;
            _viewScale = Mathf.Max(0.1f, camera.orthographicSize / REFERENCE_ORTHOGRAPHIC_SIZE);
            _flashCooldown = _GetNextFlashCooldown();

            _FollowCamera();
            _CreateTint();
            _CreateFlash();
            _CreateParticleLayers();
            _ApplyAlpha();
        }

        private void _CreateTint()
        {
            if (_style.TintColor.a <= 0f)
                return;

            _tintRenderer = _CreateFullScreenSprite("Tint", TINT_SORTING_ORDER);
        }

        private void _CreateFlash()
        {
            if (_style.FlashInterval <= 0f)
                return;

            _flashRenderer = _CreateFullScreenSprite("Flash", FLASH_SORTING_ORDER);
            _flashRenderer.color = Color.clear;
        }

        private SpriteRenderer _CreateFullScreenSprite(string objectName, int sortingOrder)
        {
            var spriteObject = new GameObject(objectName);
            spriteObject.transform.SetParent(transform, false);

            SpriteRenderer spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = _GetTintSprite();
            spriteRenderer.sortingLayerName = SORTING_LAYER_NAME;
            spriteRenderer.sortingOrder = sortingOrder;

            Vector2 viewSize = _GetViewSize() + Vector2.one * VIEW_MARGIN * 2f;
            spriteObject.transform.localScale = new Vector3(viewSize.x, viewSize.y, 1f);

            return spriteRenderer;
        }

        private void _CreateParticleLayers()
        {
            if (_style.Layers == null)
                return;

            Shader shader = Shader.Find(PRIMARY_SHADER_NAME);

            if (shader == null)
                shader = Shader.Find(FALLBACK_SHADER_NAME);

            if (shader == null)
            {
                Debug.LogWarning("[TrackEventWeatherEffect] 파티클 셰이더를 찾지 못해 입자 없이 틴트만 표시합니다.");

                return;
            }

            for (int i = 0; i < _style.Layers.Length; i++)
                _CreateParticleLayer(_style.Layers[i], shader, i);
        }

        private void _CreateParticleLayer(ParticleLayer layer, Shader shader, int index)
        {
            if (layer.MaxParticles <= 0)
                return;

            var layerObject = new GameObject($"Layer_{index}_{layer.Shape}");
            layerObject.transform.SetParent(transform, false);

            ParticleSystem particleSystem = layerObject.AddComponent<ParticleSystem>();
            // AddComponent 직후엔 재생 중이라 duration 등을 바꿀 수 없다 — 멈추고 설정한 뒤 다시 재생한다.
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            float lifetime = Mathf.Max(MIN_PARTICLE_LIFETIME, layer.Lifetime);
            Vector2 viewSize = _GetViewSize() + Vector2.one * VIEW_MARGIN * 2f;

            ParticleSystem.MainModule main = particleSystem.main;
            main.duration = lifetime;
            main.loop = true;
            main.prewarm = true; // 시작하자마자 화면이 차 있게
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(layer.SizeRange.x * _viewScale, layer.SizeRange.y * _viewScale);
            main.startRotation = layer.IsStretched ? 0f : new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(layer.ColorMin, layer.ColorMax);
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local; // 카메라를 따라다니는 화면 고정 연출
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.maxParticles = layer.MaxParticles;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = layer.MaxParticles / lifetime;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(viewSize.x, viewSize.y, 0.01f);

            _ApplyVelocity(particleSystem, layer);
            _ApplyFadeOverLifetime(particleSystem, layer);
            _ApplyNoise(particleSystem, layer);
            _ApplyRotation(particleSystem, layer);

            var material = new Material(shader)
            {
                name = $"{OBJECT_NAME}_{layer.Shape}",
                mainTexture = _GetShapeTexture(layer.Shape),
            };
            _layerMaterials.Add(material);

            ParticleSystemRenderer particleRenderer = layerObject.GetComponent<ParticleSystemRenderer>();
            particleRenderer.sharedMaterial = material;
            particleRenderer.sortingLayerName = SORTING_LAYER_NAME;
            particleRenderer.sortingOrder = layer.SortingOrder;
            particleRenderer.renderMode = layer.IsStretched ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;

            if (layer.IsStretched)
            {
                particleRenderer.lengthScale = 1f;
                particleRenderer.velocityScale = layer.StretchLength;
            }

            particleSystem.Play(true);
            _particleSystems.Add(particleSystem);
        }

        private void _ApplyVelocity(ParticleSystem particleSystem, ParticleLayer layer)
        {
            Vector2 velocity = layer.Velocity * _viewScale;
            float jitter = Mathf.Clamp01(layer.VelocityJitter);

            ParticleSystem.VelocityOverLifetimeModule velocityModule = particleSystem.velocityOverLifetime;
            velocityModule.enabled = true;
            velocityModule.space = ParticleSystemSimulationSpace.Local;
            velocityModule.x = new ParticleSystem.MinMaxCurve(velocity.x * (1f - jitter), velocity.x * (1f + jitter));
            velocityModule.y = new ParticleSystem.MinMaxCurve(velocity.y * (1f - jitter), velocity.y * (1f + jitter));
            velocityModule.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }

        // 화면 한가운데서 갑자기 생기거나 사라지지 않도록 수명 앞뒤로 알파를 차오르게/빠지게 한다.
        private static void _ApplyFadeOverLifetime(ParticleSystem particleSystem, ParticleLayer layer)
        {
            float fadeEdge = Mathf.Clamp(layer.FadeEdge, 0.01f, 0.49f);

            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, fadeEdge),
                    new GradientAlphaKey(1f, 1f - fadeEdge),
                    new GradientAlphaKey(0f, 1f),
                });

            ParticleSystem.ColorOverLifetimeModule colorModule = particleSystem.colorOverLifetime;
            colorModule.enabled = true;
            colorModule.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        private void _ApplyNoise(ParticleSystem particleSystem, ParticleLayer layer)
        {
            if (layer.NoiseStrength <= 0f)
                return;

            ParticleSystem.NoiseModule noise = particleSystem.noise;
            noise.enabled = true;
            noise.strength = layer.NoiseStrength * _viewScale;
            noise.frequency = Mathf.Max(0.01f, layer.NoiseFrequency);
            noise.scrollSpeed = 0.3f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Low; // 모바일 — 2D 노이즈면 충분
        }

        private static void _ApplyRotation(ParticleSystem particleSystem, ParticleLayer layer)
        {
            if (layer.IsStretched || layer.RotationSpeed <= 0f)
                return;

            float radiansPerSecond = layer.RotationSpeed * Mathf.Deg2Rad;

            ParticleSystem.RotationOverLifetimeModule rotation = particleSystem.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-radiansPerSecond, radiansPerSecond);
        }

        private void _UpdateAlpha(float deltaTime)
        {
            float duration = _fadeDirection > 0f ? FADE_IN_DURATION : FADE_OUT_DURATION;
            float previousAlpha = _alpha;
            _alpha = Mathf.Clamp01(_alpha + _fadeDirection * deltaTime / duration);

            if (!Mathf.Approximately(previousAlpha, _alpha))
                _ApplyAlpha();
        }

        // 레이어 머티리얼 색의 알파로 연출 전체를 페이드한다 (입자 개별 색은 그대로 둔다).
        private void _ApplyAlpha()
        {
            if (_tintRenderer != null)
            {
                Color tint = _style.TintColor;
                tint.a *= _alpha;
                _tintRenderer.color = tint;
            }

            var materialColor = new Color(1f, 1f, 1f, _alpha);

            foreach (Material material in _layerMaterials)
            {
                if (material != null)
                    material.color = materialColor;
            }
        }

        // 번개 — 대략 FlashInterval 간격으로 흰 화면이 빠르게 번쩍였다 사라진다. 사라지는 중엔 새로 번쩍이지 않는다.
        private void _UpdateFlash(float deltaTime)
        {
            if (_flashRenderer == null)
                return;

            _flashElapsedTime += deltaTime;

            if (_fadeDirection > 0f)
            {
                _flashCooldown -= deltaTime;

                if (_flashCooldown <= 0f)
                {
                    _flashCooldown = _GetNextFlashCooldown();
                    _flashElapsedTime = 0f;
                }
            }

            float progress = Mathf.Clamp01(_flashElapsedTime / FLASH_DURATION);
            // 두 번 깜빡이는 번개: 앞쪽 강한 번쩍 + 뒤쪽 약한 잔광
            float flicker = progress < 0.3f ? 1f - progress / 0.3f : (1f - progress) * 0.6f;
            _flashRenderer.color = new Color(1f, 1f, 1f, FLASH_PEAK_ALPHA * flicker * _alpha);
        }

        private float _GetNextFlashCooldown()
        {
            if (_style.FlashInterval <= 0f)
                return 0f;

            return _style.FlashInterval * Random.Range(1f - FLASH_INTERVAL_JITTER, 1f + FLASH_INTERVAL_JITTER);
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

        private static Texture2D _GetShapeTexture(ParticleShape shape)
        {
            if (_shapeTextures.TryGetValue(shape, out Texture2D cached) && cached != null)
                return cached;

            Texture2D texture = shape switch
            {
                ParticleShape.PixelSquare => _CreatePixelSquareTexture(),
                ParticleShape.Snowflake => _CreateSnowflakeTexture(),
                ParticleShape.Cloud => _CreateRadialTexture(CLOUD_TEXTURE_SIZE, 2.2f),
                _ => _CreateRadialTexture(SOFT_DOT_TEXTURE_SIZE, 1.5f),
            };

            _shapeTextures[shape] = texture;

            return texture;
        }

        // 가운데가 진하고 가장자리가 투명한 원. falloffPower가 클수록 가장자리가 더 부드럽게 빠진다.
        private static Texture2D _CreateRadialTexture(int size, float falloffPower)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), falloffPower);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();

            return texture;
        }

        private static Texture2D _CreatePixelSquareTexture()
        {
            var texture = new Texture2D(PIXEL_SQUARE_TEXTURE_SIZE, PIXEL_SQUARE_TEXTURE_SIZE, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color[PIXEL_SQUARE_TEXTURE_SIZE * PIXEL_SQUARE_TEXTURE_SIZE];

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }

        private static Texture2D _CreateSnowflakeTexture()
        {
            int size = SNOWFLAKE_PATTERN.Length;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            for (int y = 0; y < size; y++)
            {
                string row = SNOWFLAKE_PATTERN[size - 1 - y]; // 텍스처는 아래 행부터

                for (int x = 0; x < size; x++)
                {
                    bool isFilled = x < row.Length && row[x] == '#';
                    texture.SetPixel(x, y, isFilled ? Color.white : Color.clear);
                }
            }

            texture.Apply();

            return texture;
        }

        // 1유닛 크기 흰 사각형 — 화면 크기로 늘려 틴트·번쩍임으로 쓴다.
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
