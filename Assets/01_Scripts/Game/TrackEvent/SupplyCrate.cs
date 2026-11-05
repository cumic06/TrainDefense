using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TrainDefense.Game.TrackEvent
{
    /// <summary>
    /// 보급 반짝이. 십자 별빛이 돌면서 커졌다 작아지고, 뒤의 둥근 빛은 고정이다. 천천히 떠가다 터치(모바일) / 클릭(에디터)되면
    /// Collected 이벤트를 한 번 발행하고 작은 별로 터진 뒤 사라진다. 수명/지급은 효과 쪽이 관리한다.
    /// </summary>
    public class SupplyCrate : MonoBehaviour
    {
        #region Variables
        private const string STAR_SPRITE_PATH = "Sprite/GameMadang/8.VFX/FXT_Hit_3_3/FXT_StarLight";
        private const string GLOW_SPRITE_PATH = "Sprite/GameMadang/8.VFX/FXT_Shoot_4_2/FXT_Glow";
        // 눈에 보이는 둥근 빛의 지름(스프라이트 1.28유닛 × GLOW_SCALE)과 같게 맞춘다.
        private const float COLLIDER_SIZE = 2.3f;
        // Default 레이어는 Map보다 아래라 안 보인다 — 보상 오브젝트용 Reward 레이어에 그린다.
        private const string SORTING_LAYER_NAME = "Reward";
        private const int GLOW_SORTING_ORDER = 0;
        private const int STAR_SORTING_ORDER = 1;

        private const float STAR_SCALE = 0.62f;
        private const float STAR_ALPHA = 0.92f;
        private const float STAR_PULSE_RATIO = 0.2f;
        private const float STAR_PULSE_SPEED = 5.5f;
        private const float STAR_ROTATION_SPEED = 60f;
        private const float GLOW_SCALE = 1.8f;
        private const float GLOW_ALPHA = 0.32f;

        private const float BOB_HEIGHT = 0.3f;
        private const float BOB_SPEED = 3f;
        private const float FADE_OUT_DURATION = 1f;

        private const int BURST_STAR_COUNT = 8;
        private const float BURST_DURATION = 0.7f;
        private const float BURST_START_DISTANCE = 0.6f;
        private const float BURST_END_DISTANCE = 3.8f;
        private const float BURST_START_SCALE = 0.55f;
        private const float BURST_END_SCALE = 0.22f;

        private static Sprite _starSprite;
        private static Sprite _glowSprite;
        private static readonly List<SupplyCrate> _activeCrates = new List<SupplyCrate>();
        #endregion

        #region Fields
        private Camera _mainCamera;
        private bool _isCollected;
        private PointerEventData _pointerEventData;
        private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();
        private Collider2D _collider;

        private SpriteRenderer _starRenderer;
        private SpriteRenderer _glowRenderer;
        private SpriteRenderer[] _burstRenderers;
        private Vector3 _basePosition;
        private Vector3 _velocity;
        private float _lifetime;
        private float _elapsedTime;
        private float _burstElapsedTime;
        #endregion

        public event Action<SupplyCrate> Collected;

        #region LifeCycle
        private void OnEnable()
        {
            _activeCrates.Add(this);
        }

        private void OnDisable()
        {
            _activeCrates.Remove(this);
        }

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
        }

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (_isCollected)
            {
                _UpdateBurst();

                return;
            }

            _UpdateMotion();

            if (_collider == null || !_TryGetTapWorldPosition(out Vector2 worldPosition))
            {
                return;
            }

            if (!_collider.OverlapPoint(worldPosition))
            {
                return;
            }

            // 콜라이더는 다음 프레임에 끈다. 이번 프레임에 PlayerBombardManager가 IsCrateAt을 물어도 탭이 포격으로 새지 않는다.
            _isCollected = true;
            _starRenderer.enabled = false;
            _glowRenderer.enabled = false;
            _CreateBurstStars();
            Collected?.Invoke(this);
        }
        #endregion

        /// <summary>
        /// 이 월드 좌표에 아직 줍지 않은 상자가 있는지. 상자 탭이 플레이어 포격으로도 새지 않게
        /// PlayerBombardManager가 발사 전에 묻는다(Update 실행 순서와 무관하게 동작).
        /// </summary>
        public static bool IsCrateAt(Vector2 worldPosition)
        {
            foreach (SupplyCrate crate in _activeCrates)
            {
                if (crate._collider != null && crate._collider.OverlapPoint(worldPosition))
                    return true;
            }

            return false;
        }

        /// <summary>런타임에 반짝이를 만든다. velocity로 떠가고 lifetime 끝 무렵 흐려진다.</summary>
        public static SupplyCrate Create(Vector3 position, Vector3 velocity, float lifetime)
        {
            var go = new GameObject("SupplyCrate");
            go.transform.position = position;

            var boxCollider = go.AddComponent<BoxCollider2D>();
            boxCollider.size = new Vector2(COLLIDER_SIZE, COLLIDER_SIZE);

            var crate = go.AddComponent<SupplyCrate>();
            crate._basePosition = position;
            crate._velocity = velocity;
            crate._lifetime = lifetime;
            crate._glowRenderer = _CreateRenderer(go.transform, "Glow", _GetGlowSprite(), GLOW_SORTING_ORDER);
            crate._glowRenderer.transform.localScale = Vector3.one * GLOW_SCALE;
            crate._starRenderer = _CreateRenderer(go.transform, "Star", GetStarSprite(), STAR_SORTING_ORDER);
            crate._UpdateMotion();

            return crate;
        }

        private void _UpdateMotion()
        {
            _elapsedTime += Time.deltaTime;
            _basePosition += _velocity * Time.deltaTime;
            transform.position = _basePosition + Vector3.up * (BOB_HEIGHT * Mathf.Sin(_elapsedTime * BOB_SPEED));

            _starRenderer.transform.localScale = Vector3.one * (STAR_SCALE * (1f + STAR_PULSE_RATIO * Mathf.Sin(_elapsedTime * STAR_PULSE_SPEED)));
            _starRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, _elapsedTime * STAR_ROTATION_SPEED);

            float fade = Mathf.Clamp01((_lifetime - _elapsedTime) / FADE_OUT_DURATION);
            _starRenderer.color = new Color(1f, 1f, 1f, STAR_ALPHA * fade);
            _glowRenderer.color = new Color(1f, 1f, 1f, GLOW_ALPHA * fade);
        }

        private void _UpdateBurst()
        {
            _collider.enabled = false;
            _burstElapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(_burstElapsedTime / BURST_DURATION);

            for (int i = 0; i < _burstRenderers.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / _burstRenderers.Length;
                float distance = Mathf.Lerp(BURST_START_DISTANCE, BURST_END_DISTANCE, progress);
                Transform burstTransform = _burstRenderers[i].transform;

                burstTransform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * distance;
                burstTransform.localScale = Vector3.one * Mathf.Lerp(BURST_START_SCALE, BURST_END_SCALE, progress);
                _burstRenderers[i].color = new Color(1f, 1f, 1f, 1f - progress);
            }

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private void _CreateBurstStars()
        {
            _burstRenderers = new SpriteRenderer[BURST_STAR_COUNT];

            for (int i = 0; i < BURST_STAR_COUNT; i++)
            {
                _burstRenderers[i] = _CreateRenderer(transform, "BurstStar", GetStarSprite(), STAR_SORTING_ORDER);
            }
        }

        private static SpriteRenderer _CreateRenderer(Transform parent, string objectName, Sprite sprite, int sortingOrder)
        {
            var rendererObject = new GameObject(objectName);
            rendererObject.transform.SetParent(parent, false);

            var spriteRenderer = rendererObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingLayerName = SORTING_LAYER_NAME;
            spriteRenderer.sortingOrder = sortingOrder;

            return spriteRenderer;
        }

        /// <summary>별빛 스프라이트. 황금 몬스터의 별빛도 같은 그림을 쓴다.</summary>
        public static Sprite GetStarSprite()
        {
            if (_starSprite == null)
            {
                _starSprite = _LoadFirstSprite(STAR_SPRITE_PATH);
            }

            return _starSprite;
        }

        private static Sprite _GetGlowSprite()
        {
            if (_glowSprite == null)
            {
                _glowSprite = _LoadFirstSprite(GLOW_SPRITE_PATH);
            }

            return _glowSprite;
        }

        private static Sprite _LoadFirstSprite(string path)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(path);

            return sprites.Length > 0 ? sprites[0] : null;
        }

        private bool _TryGetTapWorldPosition(out Vector2 worldPosition)
        {
            worldPosition = default;

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;

                if (_mainCamera == null)
                {
                    return false;
                }
            }

            Vector3 screenPosition;
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase != TouchPhase.Began)
                {
                    return false;
                }

                screenPosition = touch.position;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
            }
            else
            {
                return false;
            }

            if (_IsPointerOverUI(screenPosition))
            {
                return false;
            }

            Vector3 world = _mainCamera.ScreenToWorldPoint(screenPosition);
            worldPosition = new Vector2(world.x, world.y);

            return true;
        }

        // PlayerBombardManager와 동일한 이유로 탭 지점에 UI 레이캐스트를 직접 쏜다 (UI 위 탭은 상자로 안 간다).
        private bool _IsPointerOverUI(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            if (_pointerEventData == null)
            {
                _pointerEventData = new PointerEventData(eventSystem);
            }

            _pointerEventData.position = screenPosition;
            _uiRaycastResults.Clear();
            eventSystem.RaycastAll(_pointerEventData, _uiRaycastResults);

            return _uiRaycastResults.Count > 0;
        }
    }
}
