using Cumic.Events;
using TrainDefense.Game.Events;
using UnityEngine;
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
                _FrameTrain();
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
                _previewTexture = new RenderTexture(textureWidth, textureHeight, 0);

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
            _FrameTrain();
        }

        private void _Hide()
        {
            if (_previewCamera != null)
                _previewCamera.enabled = false;

            if (previewImage != null)
                previewImage.enabled = false;
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

        // 프리뷰 이미지의 화면 좌표 → 프리뷰 카메라의 월드 좌표로 옮긴 뒤, 그 점을 스프라이트 경계로 덮는 칸(포탑)을 찾는다.
        // 칸들은 선로를 따라 가로로 이어 붙어 있어 경계가 겹치지 않는다.
        private Train _FindTrainAt(Vector2 screenPosition, Camera eventCamera)
        {
            var mainTrain = TrainManager.Instance != null ? TrainManager.Instance.MainTrain : null;
            if (mainTrain == null || _previewCamera == null || !_previewCamera.enabled)
                return null;

            RectTransform imageRect = previewImage.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(imageRect, screenPosition, eventCamera, out Vector2 localPoint))
                return null;

            Rect rect = imageRect.rect;
            Vector3 viewportPoint = new Vector3((localPoint.x - rect.xMin) / rect.width, (localPoint.y - rect.yMin) / rect.height, 0f);
            Vector3 worldPoint = _previewCamera.ViewportToWorldPoint(viewportPoint);

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
