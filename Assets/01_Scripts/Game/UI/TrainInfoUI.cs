using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Cumic.Events;
using TrainDefense;
using TrainDefense.Game.Events;

namespace TrainDefense.Game.UI
{
    public class TrainInfoUI : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private TrainInfoSlotUI trainInfoSlotUI;
        #endregion

        // 슬롯 묶음이 화면(Canvas) 폭을 넘지 않도록 남기는 좌우 여백 비율
        private const float WidthFitRatio = 0.95f;

        private RectTransform _rectTransform;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private Coroutine _reflowCor;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            ResourceManager.Instance.RegisterPersistent(gameObject);
            GameEventSystem.Subscribe<AddTrainEvent>(OnAddTrain);
        }

        private void Start()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }

        private void Update()
        {
            // Unity엔 해상도 변경 전용 내장 이벤트가 없어 Screen 크기를 폴링해 폴더블 접힘/펼침을 감지한다.
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                _lastScreenWidth = Screen.width;
                _lastScreenHeight = Screen.height;
                _ScheduleReflow();
            }
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<AddTrainEvent>(OnAddTrain);
        }

        private void OnAddTrain(AddTrainEvent addTrainEvent)
        {
            TrainInfoSlotUI spawnTrainInfoSlotUI = ResourceManager.Instance.Spawn(trainInfoSlotUI, parent: transform);
            // 풀 재사용 시 SetParent(worldPositionStays=true)로 인해 캔버스 스케일만큼 깨진 localScale 복구
            spawnTrainInfoSlotUI.transform.localScale = Vector3.one;
            spawnTrainInfoSlotUI.Init(addTrainEvent.Train);
            spawnTrainInfoSlotUI.SetIcon(addTrainEvent.Icon);

            // 슬롯이 추가될 때마다 컨테이너 폭/슬롯 위치를 즉시 재계산하여 누락을 방지한다.
            _RebuildLayout();
        }

        private void _ScheduleReflow()
        {
            if (!isActiveAndEnabled || _rectTransform == null)
            {
                _RebuildLayout();
                return;
            }

            if (_reflowCor != null)
                StopCoroutine(_reflowCor);

            _reflowCor = StartCoroutine(_ReflowRoutine());
        }

        private IEnumerator _ReflowRoutine()
        {
            // 폴더블 전환은 해상도가 여러 프레임에 걸쳐 바뀌므로 캔버스가 안정될 때까지 몇 프레임 더 재정렬한다.
            for (int i = 0; i < 5; i++)
            {
                yield return null;
                _RebuildLayout();
            }
            _reflowCor = null;
        }

        private void _RebuildLayout()
        {
            if (_rectTransform == null)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
            _FitToAvailableWidth();
        }

        // 슬롯 묶음(ContentSizeFitter로 늘어난 폭)이 Canvas 폭을 넘으면 균등 축소해
        // 폴더블을 접어 화면이 좁아져도 모든 슬롯이 화면 안에 들어오게 한다.
        private void _FitToAvailableWidth()
        {
            RectTransform parentRect = _rectTransform.parent as RectTransform;
            if (parentRect == null)
                return;

            float available = parentRect.rect.width * WidthFitRatio;
            float content = _rectTransform.rect.width;

            if (available <= 0f || content <= 0f)
                return;

            float scale = content > available ? available / content : 1f;
            _rectTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
