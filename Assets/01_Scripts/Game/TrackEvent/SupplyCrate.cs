using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TrainDefense.Game.TrackEvent
{
    /// <summary>
    /// 보급 상자. 터치(모바일) / 클릭(에디터)되면 Collected 이벤트를 한 번 발행하고 스스로 사라진다.
    /// 프리팹으로 교체할 때는 SpriteRenderer + Collider2D + 이 컴포넌트만 있으면 된다
    /// (SupplyDropTrackEventEffect._SpawnCrate의 Create 호출이 교체 지점). 수명/지급은 효과 쪽이 관리하고 상자는 입력만 담당한다.
    /// </summary>
    public class SupplyCrate : MonoBehaviour
    {
        #region Variables
        private const float COLLIDER_SIZE = 1.6f;
        // Default 레이어는 Map보다 아래라 안 보인다 — 보상 오브젝트용 Reward 레이어에 그린다.
        private const string SORTING_LAYER_NAME = "Reward";
        private const int SORTING_ORDER = 0;
        private const int SPRITE_TEXTURE_SIZE = 4;
        private const float SPRITE_PIXELS_PER_UNIT = 4f;

        private static readonly Color CRATE_COLOR = new Color(0.95f, 0.75f, 0.2f, 1f);
        private static Sprite _fallbackSprite;
        private static readonly List<SupplyCrate> _activeCrates = new List<SupplyCrate>();
        #endregion

        #region Fields
        private Camera _mainCamera;
        private bool _isCollected;
        private PointerEventData _pointerEventData;
        private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();
        private Collider2D _collider;
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
            if (_isCollected || _collider == null)
            {
                return;
            }

            if (!_TryGetTapWorldPosition(out Vector2 worldPosition))
            {
                return;
            }

            if (!_collider.OverlapPoint(worldPosition))
            {
                return;
            }

            _isCollected = true;
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

        /// <summary>
        /// 런타임에 상자 오브젝트를 만든다. SpriteRenderer + BoxCollider2D + SupplyCrate를 코드로 붙인다.
        /// </summary>
        public static SupplyCrate Create(Vector3 position, Sprite sprite = null)
        {
            var go = new GameObject("SupplyCrate");
            go.transform.position = position;

            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite != null ? sprite : _GetFallbackSprite();
            spriteRenderer.color = sprite != null ? Color.white : CRATE_COLOR;
            spriteRenderer.sortingLayerName = SORTING_LAYER_NAME;
            spriteRenderer.sortingOrder = SORTING_ORDER;

            var boxCollider = go.AddComponent<BoxCollider2D>();
            boxCollider.size = new Vector2(COLLIDER_SIZE, COLLIDER_SIZE);

            return go.AddComponent<SupplyCrate>();
        }

        private static Sprite _GetFallbackSprite()
        {
            if (_fallbackSprite != null)
            {
                return _fallbackSprite;
            }

            var texture = new Texture2D(SPRITE_TEXTURE_SIZE, SPRITE_TEXTURE_SIZE, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            var pixels = new Color[SPRITE_TEXTURE_SIZE * SPRITE_TEXTURE_SIZE];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            _fallbackSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, SPRITE_TEXTURE_SIZE, SPRITE_TEXTURE_SIZE),
                new Vector2(0.5f, 0.5f),
                SPRITE_PIXELS_PER_UNIT);

            return _fallbackSprite;
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
