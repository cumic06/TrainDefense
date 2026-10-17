using System.Collections.Generic;
using Cumic.Events;
using TMPro;
using TrainDefense.Game.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    // 상점 화면 하단에 현재 기차(엔진 + 포탑 편성)를 그대로 비춰주는 프리뷰.
    // 상점 중 기차는 화면 오른쪽 밖에 나가 있으므로, 전용 카메라가 Train 레이어만 RenderTexture로 찍어 RawImage에 띄운다.
    public class ShopTrainPreviewUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private RawImage previewImage;

        [SerializeField]
        [Tooltip("프리뷰 카메라가 찍을 레이어. 기차·포탑 비주얼(Train)만 켜서 맵·몬스터가 안 찍히게 한다.")]
        private LayerMask cullingMask;

        [SerializeField]
        [Tooltip("RenderTexture 세로 해상도. 가로는 RawImage 사각형 비율에 맞춰 자동 산출")]
        private int textureHeight = 600;

        [SerializeField]
        [Tooltip("기차 전체 경계에 곱해 여백을 두는 배율. 편성이 길어 경계 맞춤이 걸리는 구간에서는 "
            + "화면상 기차 폭이 프리뷰 폭 / 이 값으로 고정되므로, 편성 길이와 무관한 좌우 여백을 정한다")]
        private float paddingRatio = 1.15f;

        [SerializeField]
        [Tooltip("인게임과 같은 배율에 곱하는 값. 1이면 인게임 크기 그대로, 작을수록 크게 보인다.")]
        [Range(0.15f, 1f)]
        private float previewZoomRatio = 0.25f;
        #endregion

        private Camera _previewCamera;
        private RenderTexture _previewTexture;
        private float _aspect = 1f;
        private TrainDetailPopupUI _detailPopup;
        private LongPressHandler _longPressHandler;

        // 기차 칸 위 개조 진행 배지 — HUD 포탑 칸의 레벨 배지(TrainInfoSlotUI의 Img_Level)를 복제해 쓴다.
        // 상점에서는 HUD 포탑 칸이 안 보이므로 같은 배지를 칸 위에 올려 등급 합(개조 진행도)을 보여 준다.
        private const string BADGE_SOURCE_PREFAB_PATH = "Prefabs/UI/TrainInfoSlotUI";
        private const string BADGE_SOURCE_OBJECT_NAME = "Img_Level";
        // 상점 기차 칸이 HUD 칸보다 커서 같은 크기면 숫자가 흐리다.
        private const float BADGE_SCALE = 1.5f;
        // 칸 위쪽 가운데에서 위로 띄우는 정도(프리뷰 이미지 로컬 단위). 배지는 포탑 바로 위에 뜬다.
        private static readonly Vector2 BadgeAboveOffset = new Vector2(0f, 4f);

        private class TrainBadge
        {
            public RectTransform Root;
            public Image Ribbon;
            public TextMeshProUGUI Text;
        }

        private readonly List<TrainBadge> _badges = new List<TrainBadge>();
        private GameObject _badgeSource;
        private bool _badgeSourceSearched;

        // 포탑 위치 이동 — 프리뷰의 포탑을 끌어 다른 포탑 위에 놓으면 두 칸 자리를 맞바꾼다.
        // 드래그 중인 칸은 손가락을 따라 가로로만 움직이고(선로 높이 y 고정), 놓으면 원위치로 돌아간 뒤 교환한다.
        // 끌린 칸은 이웃 칸 위에 그려지도록 SortingGroup으로 정렬을 올리고, 놓일 자리에는 그림자(반투명 실루엣)를 띄운다.
        private const int DRAG_SORTING_ORDER = 1000;
        private const int GHOST_SORTING_ORDER = DRAG_SORTING_ORDER - 1;
        private static readonly Color GhostColor = new Color(0f, 0f, 0f, 0.45f);
        private PointerDragHandler _dragHandler;
        private Train _draggingTrain;
        private Vector3 _dragOriginLocalPosition;
        private float _dragStartWorldX;
        private SortingGroup _dragSortingGroup;
        private GameObject _dragGhost;

        /// <summary>
        /// 프리뷰의 포탑을 꾹 누르면 스탯을 띄울 상점 공용 팝업. ShopUI가 넘겨준다.
        /// 프리뷰 이미지(RawImage)가 입력을 받아야 하므로 롱프레스 수신도 여기서 붙인다.
        /// </summary>
        public void SetDetailPopup(TrainDetailPopupUI detailPopup)
        {
            _detailPopup = detailPopup;

            if (_longPressHandler != null || previewImage == null)
                return;

            _longPressHandler = previewImage.gameObject.AddComponent<LongPressHandler>();
            _longPressHandler.Initialize(_OnLongPress, _OnLongPressRelease);
        }

        private void OnEnable()
        {
            GameEventSystem.Subscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Subscribe<InspectionEndEvent>(_OnInspectionEnd);
        }

        private void OnDisable()
        {
            GameEventSystem.Unsubscribe<InspectionStartEvent>(_OnInspectionStart);
            GameEventSystem.Unsubscribe<InspectionEndEvent>(_OnInspectionEnd);
            _Hide();
        }

        /// <summary>
        /// 상점이 배경을 켠 직후 ShopUI가 직접 부른다.
        /// ★ 이 오브젝트는 상점 배경(Img_Background)의 자식이라 상점이 닫혀 있는 동안 비활성이고,
        ///   비활성 오브젝트는 Awake·OnEnable이 실행되지 않아 InspectionStartEvent를 놓친다.
        ///   (이벤트 구독으로는 첫 표시를 보장할 수 없어 진입 경로를 하나 더 둔다)
        /// </summary>
        public void ShowNow()
        {
            _Show();
        }

        private void OnDestroy()
        {
            if (_previewCamera != null)
                Destroy(_previewCamera.gameObject);

            if (_previewTexture != null)
                _previewTexture.Release();
        }

        private void LateUpdate()
        {
            if (_previewCamera != null && _previewCamera.enabled)
            {
                // 드래그 중엔 끌린 칸 때문에 경계가 바뀌어 화면이 흔들리므로 프레이밍을 멈춘다.
                if (_draggingTrain == null)
                    _FrameTrain();

                _RefreshBadges();
            }
        }

        private void _OnInspectionStart(InspectionStartEvent _)
        {
            _Show();
        }

        private void _OnInspectionEnd(InspectionEndEvent _)
        {
            _Hide();
        }

        private void _Show()
        {
            if (previewImage == null)
                return;

            // RawImage 사각형 비율대로 RT를 만들어야 기차가 늘어나 보이지 않는다.
            Rect rect = previewImage.rectTransform.rect;
            _aspect = rect.height > 0f ? rect.width / rect.height : 1f;
            int textureWidth = Mathf.Max(1, Mathf.RoundToInt(textureHeight * _aspect));

            if (_previewTexture != null && _previewTexture.width != textureWidth)
            {
                _previewTexture.Release();
                _previewTexture = null;
            }

            if (_previewTexture == null)
                _previewTexture = new RenderTexture(textureWidth, textureHeight, 16);

            if (_previewCamera == null)
            {
                var cameraObject = new GameObject("ShopTrainPreviewCamera");
                _previewCamera = cameraObject.AddComponent<Camera>();
                _previewCamera.orthographic = true;
                _previewCamera.clearFlags = CameraClearFlags.SolidColor;
                _previewCamera.backgroundColor = Color.clear;
                _previewCamera.cullingMask = cullingMask;
            }

            _previewCamera.targetTexture = _previewTexture;

            previewImage.texture = _previewTexture;
            previewImage.enabled = true;
            _previewCamera.enabled = true;
            _EnsureDragHandler();
            _FrameTrain();
        }

        private void _Hide()
        {
            _CancelDrag();

            if (_previewCamera != null)
                _previewCamera.enabled = false;

            if (previewImage != null)
                previewImage.enabled = false;

            _HideBadges();
        }

        // 칸(포탑)마다 배지 하나를 칸 오른쪽 위에 놓고 숫자·색을 갱신한다. 편성이 바뀌면 배지 수도 맞춘다.
        private void _RefreshBadges()
        {
            var mainTrain = TrainManager.Instance != null ? TrainManager.Instance.MainTrain : null;

            if (mainTrain == null || previewImage == null || !_EnsureBadgeSource())
                return;

            var trains = mainTrain.CurrentTrains;
            int badgeIndex = 0;
            RectTransform imageRect = previewImage.rectTransform;
            Rect rect = imageRect.rect;

            foreach (var train in trains)
            {
                if (train == null || !_TryGetTrainBounds(train, out Bounds bounds))
                    continue;

                var badge = _GetBadge(badgeIndex++);
                Vector3 viewportTopCenter = _previewCamera.WorldToViewportPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));

                badge.Root.gameObject.SetActive(true);
                // 앵커가 프리뷰 이미지 좌하단이라 뷰포트 비율 × 크기가 곧 앵커드 포지션이다.
                badge.Root.anchoredPosition = new Vector2(viewportTopCenter.x * rect.width, viewportTopCenter.y * rect.height) + BadgeAboveOffset;
                TrainRemodelInfo.ApplyBadge(badge.Ribbon, badge.Text, train);
            }

            for (int i = badgeIndex; i < _badges.Count; i++)
                _badges[i].Root.gameObject.SetActive(false);
        }

        private void _HideBadges()
        {
            foreach (var badge in _badges)
            {
                if (badge.Root != null)
                    badge.Root.gameObject.SetActive(false);
            }
        }

        private TrainBadge _GetBadge(int index)
        {
            while (_badges.Count <= index)
            {
                var badgeObject = Instantiate(_badgeSource, previewImage.rectTransform, false);
                badgeObject.name = "Badge_RemodelProgress";
                badgeObject.SetActive(true);

                var root = (RectTransform)badgeObject.transform;
                // 칸 위 가운데에 놓을 수 있게 앵커를 프리뷰 이미지 좌하단, 피벗을 배지 아래 가운데로 맞춘다.
                root.anchorMin = root.anchorMax = Vector2.zero;
                root.pivot = new Vector2(0.5f, 0f);
                root.localScale = Vector3.one * BADGE_SCALE;

                var ribbon = badgeObject.GetComponent<Image>();

                _badges.Add(new TrainBadge
                {
                    Root = root,
                    Ribbon = ribbon,
                    Text = badgeObject.GetComponentInChildren<TextMeshProUGUI>(true)
                });

                // 배지가 프리뷰 이미지의 롱프레스(포탑 스탯 보기)를 막지 않게 한다.
                if (ribbon != null)
                    ribbon.raycastTarget = false;

                foreach (var graphic in badgeObject.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget = false;
            }

            return _badges[index];
        }

        // HUD 포탑 칸 프리팹에서 레벨 배지 오브젝트를 한 번만 찾아 둔다. 프리팹은 Resources에 있다.
        private bool _EnsureBadgeSource()
        {
            if (_badgeSource != null)
                return true;

            if (_badgeSourceSearched)
                return false;

            _badgeSourceSearched = true;
            var slotPrefab = Resources.Load<GameObject>(BADGE_SOURCE_PREFAB_PATH);

            if (slotPrefab == null)
            {
                Debug.LogWarning($"ShopTrainPreviewUI: 배지 원본 프리팹을 찾지 못했습니다 ({BADGE_SOURCE_PREFAB_PATH})");
                return false;
            }

            foreach (var image in slotPrefab.GetComponentsInChildren<Image>(true))
            {
                if (image.gameObject.name == BADGE_SOURCE_OBJECT_NAME)
                {
                    _badgeSource = image.gameObject;
                    return true;
                }
            }

            Debug.LogWarning($"ShopTrainPreviewUI: {BADGE_SOURCE_PREFAB_PATH}에 {BADGE_SOURCE_OBJECT_NAME}이 없습니다");
            return false;
        }

        // 기차 하위 스프라이트(프리뷰 레이어에 속한 것) 전체 경계에 카메라를 맞춘다. 상점 중 구매·수리로 편성이 바뀌어도 매 프레임 따라간다.
        private void _FrameTrain()
        {
            var mainTrain = TrainManager.Instance != null ? TrainManager.Instance.MainTrain : null;
            if (mainTrain == null)
                return;

            bool hasBounds = false;
            Bounds bounds = default;

            foreach (var spriteRenderer in mainTrain.GetComponentsInChildren<SpriteRenderer>())
            {
                if (spriteRenderer.sprite == null || !spriteRenderer.enabled)
                    continue;

                if ((cullingMask.value & (1 << spriteRenderer.gameObject.layer)) == 0)
                    continue;

                if (!hasBounds)
                {
                    bounds = spriteRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(spriteRenderer.bounds);
                }
            }

            if (!hasBounds)
                return;

            // 세로는 스프라이트 경계 중심이 아니라 선로(기차 트랜스폼) y에 맞춘다. 칸은 전부 MainTrain 기준 y 0에 놓이므로
            // 이 값이 곧 선로 높이다. 경계 중심을 쓰면 포탑이 높은 칸이 있을 때 차체가 프리뷰 중심(=상점 UI 선로)보다 아래로 내려앉는다.
            _previewCamera.transform.position = new Vector3(bounds.center.x, mainTrain.transform.position.y, bounds.center.z - 10f);

            // 기본은 인게임 화면과 같은 배율(메인 카메라가 화면 전체에 담는 높이 × 프리뷰가 화면에서 차지하는 높이 비율)로 보여주고,
            // 편성이 길어져 프리뷰 폭을 넘치면 그때만 전체가 들어오도록 축소한다. (예전엔 항상 경계에 맞춰 확대돼 기차·포탑이 인게임보다 훨씬 크게 보였음)
            float fitSize = Mathf.Max(bounds.extents.y, bounds.extents.x / _aspect) * paddingRatio;

            // 인게임 배율을 그대로 쓰면 게임 화면이 넓어진 뒤로 기차가 프리뷰 영역의 1/7밖에 안 찬다.
            // 상점은 편성을 확인하는 화면이라 그만큼 당겨서 보여준다.
            float sameScaleSize = _GetSameScaleOrthographicSize() * previewZoomRatio;

            _previewCamera.orthographicSize = Mathf.Max(fitSize, sameScaleSize);
        }

        private void _OnLongPress(Vector2 screenPosition, Camera eventCamera)
        {
            if (_detailPopup == null)
                return;

            var train = _FindTrainAt(screenPosition, eventCamera);
            if (train == null)
                return;

            _detailPopup.Show(train);
            _detailPopup.SetNameVisible(true);

            // 누른 칸 왼쪽 위(카드 판과의 구분선에 걸치게)에 띄운다.
            if (_TryGetTrainImageCorners(train, out Vector3 anchorMin, out Vector3 anchorMax))
                _detailPopup.PlaceBeside(anchorMin, anchorMax, true);
        }

        // 칸의 스프라이트 경계 → 프리뷰 카메라 뷰포트 → 프리뷰 이미지 위의 캔버스 월드 좌표.
        private bool _TryGetTrainImageCorners(Train train, out Vector3 anchorMin, out Vector3 anchorMax)
        {
            anchorMin = anchorMax = default;
            if (!_TryGetTrainBounds(train, out Bounds bounds))
                return false;

            RectTransform imageRect = previewImage.rectTransform;
            Rect rect = imageRect.rect;
            Vector3 viewportMin = _previewCamera.WorldToViewportPoint(bounds.min);
            Vector3 viewportMax = _previewCamera.WorldToViewportPoint(bounds.max);
            anchorMin = imageRect.TransformPoint(new Vector3(rect.xMin + viewportMin.x * rect.width, rect.yMin + viewportMin.y * rect.height, 0f));
            anchorMax = imageRect.TransformPoint(new Vector3(rect.xMin + viewportMax.x * rect.width, rect.yMin + viewportMax.y * rect.height, 0f));
            return true;
        }

        private void _OnLongPressRelease()
        {
            if (_detailPopup != null && _detailPopup.gameObject.activeSelf)
                _detailPopup.Hide();
        }

        // 롱프레스와 같은 프리뷰 이미지에 드래그 수신을 붙인다. 상점 팝업 연결(SetDetailPopup)과 무관하게 동작해야 해서 표시 시점에 붙인다.
        private void _EnsureDragHandler()
        {
            if (_dragHandler != null || previewImage == null)
                return;

            _dragHandler = previewImage.gameObject.AddComponent<PointerDragHandler>();
            _dragHandler.Initialize(_OnBeginDrag, _OnDrag, _OnEndDrag, _CancelDrag);
        }

        private void _OnBeginDrag(PointerEventData eventData)
        {
            // 누른 채 움직이기 시작했으면 롱프레스(스탯 보기)가 아니라 위치 이동이다.
            _longPressHandler?.Cancel();

            var train = _FindTrainAt(eventData.pressPosition, eventData.pressEventCamera);

            if (train == null || train.IsDead || !_TryScreenToWorld(eventData.pressPosition, eventData.pressEventCamera, out Vector3 worldPoint))
                return;

            _draggingTrain = train;
            _dragOriginLocalPosition = train.transform.localPosition;
            _dragStartWorldX = worldPoint.x;
            // 그림자는 원위치에서 찍어야 칸 하위 스프라이트의 상대 배치가 그대로 복제된다.
            _CreateDragGhost(train);
            _BringDraggingTrainToFront(train);
            _OnDrag(eventData);
        }

        private void _OnDrag(PointerEventData eventData)
        {
            if (_draggingTrain == null)
                return;

            if (!_TryScreenToWorld(eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
                return;

            // 칸은 MainTrain의 자식이고 MainTrain은 회전·스케일 없이 가로로만 움직이므로 월드 x 이동량을 그대로 로컬에 더한다.
            // y·z는 원위치 그대로 둬 선로 높이에서 벗어나지 않게 한다.
            _draggingTrain.transform.localPosition = _dragOriginLocalPosition + new Vector3(worldPoint.x - _dragStartWorldX, 0f, 0f);
            _UpdateDragGhost(_FindTrainAtWorldX(worldPoint.x, _draggingTrain));
        }

        private void _OnEndDrag(PointerEventData eventData)
        {
            if (_draggingTrain == null)
                return;

            Train draggedTrain = _draggingTrain;
            Train targetTrain = _TryScreenToWorld(eventData.position, eventData.pressEventCamera, out Vector3 worldPoint)
                ? _FindTrainAtWorldX(worldPoint.x, draggedTrain)
                : null;

            // 교환은 두 칸의 현재 좌표를 맞바꾸므로, 끌던 칸을 먼저 원위치로 돌려놓아야 한다.
            _CancelDrag();

            var mainTrain = TrainManager.Instance != null ? TrainManager.Instance.MainTrain : null;

            if (mainTrain == null || targetTrain == null)
                return;

            mainTrain.SwapTrainOrder(draggedTrain, targetTrain);
        }

        // 끌던 칸을 원위치로 되돌린다(상점 닫힘·드래그 중단 시에도 호출).
        private void _CancelDrag()
        {
            if (_draggingTrain != null)
                _draggingTrain.transform.localPosition = _dragOriginLocalPosition;

            _draggingTrain = null;
            _RestoreDraggingTrainSorting();
            _DestroyDragGhost();
        }

        // 끌린 칸 전체를 하나로 묶어 이웃 칸(차체·포탑) 위에 그린다. 칸 안의 차체/포탑 순서는 그룹 안에서 그대로 유지된다.
        private void _BringDraggingTrainToFront(Train train)
        {
            if (!_TryGetTopSortingLayerID(train, out int sortingLayerID))
                return;

            _dragSortingGroup = train.gameObject.AddComponent<SortingGroup>();
            _dragSortingGroup.sortingLayerID = sortingLayerID;
            _dragSortingGroup.sortingOrder = DRAG_SORTING_ORDER;
        }

        private void _RestoreDraggingTrainSorting()
        {
            if (_dragSortingGroup != null)
                Destroy(_dragSortingGroup);

            _dragSortingGroup = null;
        }

        // 끌린 칸의 스프라이트(프리뷰 레이어에 찍히는 것)만 복제해 검은 반투명 실루엣을 만든다.
        // 칸 오브젝트를 통째로 복제하면 포탑 로직까지 따라오므로 SpriteRenderer만 옮긴다.
        private void _CreateDragGhost(Train train)
        {
            _DestroyDragGhost();

            var mainTrain = TrainManager.Instance != null ? TrainManager.Instance.MainTrain : null;

            if (mainTrain == null || !_TryGetTopSortingLayerID(train, out int sortingLayerID))
                return;

            Transform trainTransform = train.transform;
            _dragGhost = new GameObject("DragGhost");
            _dragGhost.layer = train.gameObject.layer;

            Transform ghostTransform = _dragGhost.transform;
            ghostTransform.SetParent(mainTrain.transform, false);
            ghostTransform.localPosition = trainTransform.localPosition;
            ghostTransform.localRotation = trainTransform.localRotation;
            ghostTransform.localScale = trainTransform.localScale;

            // 끌린 칸 바로 아래, 다른 칸들보다는 위에 그려 놓일 자리의 칸 위에 그림자가 보이게 한다.
            var ghostSortingGroup = _dragGhost.AddComponent<SortingGroup>();
            ghostSortingGroup.sortingLayerID = sortingLayerID;
            ghostSortingGroup.sortingOrder = GHOST_SORTING_ORDER;

            Vector3 trainScale = trainTransform.lossyScale;

            foreach (var source in train.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!_IsPreviewSprite(source))
                    continue;

                var part = new GameObject(source.gameObject.name);
                // 프리뷰 카메라 cullingMask에 걸리도록 원본 스프라이트와 같은 레이어(Train)로 맞춘다.
                part.layer = source.gameObject.layer;

                Transform partTransform = part.transform;
                Transform sourceTransform = source.transform;
                Vector3 sourceScale = sourceTransform.lossyScale;
                partTransform.SetParent(ghostTransform, false);
                partTransform.localPosition = trainTransform.InverseTransformPoint(sourceTransform.position);
                partTransform.localRotation = Quaternion.Inverse(trainTransform.rotation) * sourceTransform.rotation;
                partTransform.localScale = new Vector3(
                    trainScale.x != 0f ? sourceScale.x / trainScale.x : 0f,
                    trainScale.y != 0f ? sourceScale.y / trainScale.y : 0f,
                    trainScale.z != 0f ? sourceScale.z / trainScale.z : 0f);

                var ghostRenderer = part.AddComponent<SpriteRenderer>();
                ghostRenderer.sprite = source.sprite;
                ghostRenderer.flipX = source.flipX;
                ghostRenderer.flipY = source.flipY;
                ghostRenderer.drawMode = source.drawMode;
                ghostRenderer.size = source.size;
                ghostRenderer.sortingLayerID = source.sortingLayerID;
                ghostRenderer.sortingOrder = source.sortingOrder;
                ghostRenderer.color = GhostColor;
            }
        }

        // 놓으면 갈 자리에 그림자를 둔다. 교환 대상이 있으면 그 칸 자리, 없으면 되돌아갈 원위치.
        private void _UpdateDragGhost(Train targetTrain)
        {
            if (_dragGhost == null)
                return;

            _dragGhost.transform.localPosition = targetTrain != null ? targetTrain.transform.localPosition : _dragOriginLocalPosition;
        }

        private void _DestroyDragGhost()
        {
            if (_dragGhost != null)
                Destroy(_dragGhost);

            _dragGhost = null;
        }

        // 칸 스프라이트 중 가장 위에 그려지는 정렬 레이어(차체 < 포탑). 끌린 칸·그림자를 이 레이어 맨 위로 올린다.
        private bool _TryGetTopSortingLayerID(Train train, out int sortingLayerID)
        {
            sortingLayerID = 0;
            bool hasLayer = false;
            int topLayerValue = int.MinValue;

            foreach (var spriteRenderer in train.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!_IsPreviewSprite(spriteRenderer))
                    continue;

                int layerValue = SortingLayer.GetLayerValueFromID(spriteRenderer.sortingLayerID);

                if (hasLayer && layerValue <= topLayerValue)
                    continue;

                topLayerValue = layerValue;
                sortingLayerID = spriteRenderer.sortingLayerID;
                hasLayer = true;
            }

            return hasLayer;
        }

        private bool _IsPreviewSprite(SpriteRenderer spriteRenderer)
        {
            return spriteRenderer.sprite != null && spriteRenderer.enabled
                && (cullingMask.value & (1 << spriteRenderer.gameObject.layer)) != 0;
        }

        // 놓은 지점의 가로 위치를 덮는 칸. 세로는 따지지 않아 포탑 높이가 달라도 칸 위아래 어디에 놓든 인정한다.
        private Train _FindTrainAtWorldX(float worldX, Train exclude)
        {
            var mainTrain = TrainManager.Instance != null ? TrainManager.Instance.MainTrain : null;

            if (mainTrain == null)
                return null;

            foreach (var train in mainTrain.CurrentTrains)
            {
                if (train == null || train == exclude || train.IsDead || !_TryGetTrainBounds(train, out Bounds bounds))
                    continue;

                if (worldX >= bounds.min.x && worldX <= bounds.max.x)
                    return train;
            }

            return null;
        }

        // 프리뷰 이미지의 화면 좌표 → 프리뷰 카메라의 월드 좌표.
        private bool _TryScreenToWorld(Vector2 screenPosition, Camera eventCamera, out Vector3 worldPoint)
        {
            worldPoint = default;

            if (_previewCamera == null || !_previewCamera.enabled || previewImage == null)
                return false;

            RectTransform imageRect = previewImage.rectTransform;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(imageRect, screenPosition, eventCamera, out Vector2 localPoint))
                return false;

            Rect rect = imageRect.rect;
            Vector3 viewportPoint = new Vector3((localPoint.x - rect.xMin) / rect.width, (localPoint.y - rect.yMin) / rect.height, 0f);
            worldPoint = _previewCamera.ViewportToWorldPoint(viewportPoint);

            return true;
        }

        // 프리뷰 이미지의 화면 좌표 → 프리뷰 카메라의 월드 좌표로 옮긴 뒤, 그 점을 스프라이트 경계로 덮는 칸(포탑)을 찾는다.
        // 칸들은 선로를 따라 가로로 이어 붙어 있어 경계가 겹치지 않는다.
        private Train _FindTrainAt(Vector2 screenPosition, Camera eventCamera)
        {
            var mainTrain = TrainManager.Instance != null ? TrainManager.Instance.MainTrain : null;
            if (mainTrain == null || !_TryScreenToWorld(screenPosition, eventCamera, out Vector3 worldPoint))
                return null;

            foreach (var train in mainTrain.CurrentTrains)
            {
                if (train != null && _TryGetTrainBounds(train, out Bounds bounds)
                    && worldPoint.x >= bounds.min.x && worldPoint.x <= bounds.max.x
                    && worldPoint.y >= bounds.min.y && worldPoint.y <= bounds.max.y)
                    return train;
            }

            return null;
        }

        // 프리뷰에 찍히는(cullingMask 레이어) 스프라이트만 합친 칸 하나의 경계.
        private bool _TryGetTrainBounds(Train train, out Bounds bounds)
        {
            bool hasBounds = false;
            bounds = default;

            foreach (var spriteRenderer in train.GetComponentsInChildren<SpriteRenderer>())
            {
                if (spriteRenderer.sprite == null || !spriteRenderer.enabled)
                    continue;

                if ((cullingMask.value & (1 << spriteRenderer.gameObject.layer)) == 0)
                    continue;

                if (!hasBounds)
                {
                    bounds = spriteRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(spriteRenderer.bounds);
                }
            }

            return hasBounds;
        }

        // 인게임과 같은 배율이 되는 직교 크기. 메인 카메라가 없거나 원근이면 0(→ 경계 맞춤만 적용).
        private float _GetSameScaleOrthographicSize()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null || !mainCamera.orthographic || previewImage == null || previewImage.canvas == null || Screen.height <= 0)
                return 0f;

            float previewScreenHeight = previewImage.rectTransform.rect.height * previewImage.canvas.scaleFactor;
            return mainCamera.orthographicSize * (previewScreenHeight / Screen.height);
        }
    }
}
