using System;
using Cumic;
using Cumic.Events;
using DG.Tweening;
using TrainDefense.Game;
using TrainDefense.Game.Events;
using UnityEngine;
using UnityEngine.Playables;

namespace TrainDefense
{
    public class TimelineManager : Singleton<TimelineManager>
    {
        #region Fields
        [SerializeField]
        private PlayableDirector playableDirector;
        [SerializeField]
        private PlayableDirector mapChangeDirector;
        [SerializeField]
        private PlayableDirector shopEnterDirector;
        [SerializeField]
        private PlayableDirector mapMoveDirector;
        [SerializeField]
        private bool startTimelineOnAwake = false;

        [Header("연출 오브젝트")]
        [SerializeField]
        [Tooltip("상점/포탈 오브젝트의 부모 앵커. 연출 시작 시 열차 위치로 옮겨 타임라인의 로컬 좌표 기준을 맞춘다.")]
        private Transform effectAnchor;
        [SerializeField]
        private GameObject shopArrivalObject;
        [SerializeField]
        private GameObject portalObject;
        [SerializeField]
        private CanvasGroup screenFade;

        [Header("맵 이동 연출 타이밍")]
        [SerializeField]
        [Tooltip("포탈 카메라 줌(0~1s) 후 페이드 아웃을 시작할 때까지의 지연")]
        private float mapMoveFadeDelay = 1f;
        [SerializeField]
        private float mapMoveFadeDuration = 1f;
        [SerializeField]
        private float mapMoveSlideInDuration = 1f;
        #endregion

        #region LifeCycle

        private void Start()
        {
            GameEventSystem.Subscribe<InspectionEndEvent>(_OnInspectionEnd);

            if (startTimelineOnAwake)
            {
                StartTimeline();
            }
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<InspectionEndEvent>(_OnInspectionEnd);
        }

        #endregion

        public bool CanPlayShopEnterTimeline => shopEnterDirector != null;
        public bool CanPlayMapMoveTimeline => mapMoveDirector != null;

        public void StartTimeline(bool isMapChange = false, Action onComplete = null)
        {
            var director = isMapChange && mapChangeDirector != null ? mapChangeDirector : playableDirector;

            TimeManager.Instance?.Pause();

            // 자식 열차들의 localPosition을 오프셋했다가 복귀 (부모 위치 고정 → 카메라 따라오지 않음)
            // Timeline: 카메라 줌 0~1s / 열차 슬라이드: delay=1s, duration=2s → 총 3s
            if (isMapChange)
                TrainManager.Instance?.MainTrain?.SlideIn(1f, 2f);

            _Play(director, onComplete);
        }

        /// <summary>
        /// 상점(점검) 진입 연출. 도달 위치에 상점 오브젝트를 활성화해 슬라이드로 도착시키고(0~2s),
        /// 도착하면 카메라가 줌인된다(2~3s). 연출이 끝나면 onComplete 호출(상점 오픈 타이밍).
        /// 상점 오브젝트는 상점이 닫힐 때(InspectionEndEvent) 비활성화된다.
        /// </summary>
        public void StartShopEnterTimeline(Action onComplete = null)
        {
            TimeManager.Instance?.Pause();
            _MoveAnchorToTrain();
            _Play(shopEnterDirector, onComplete);
        }

        /// <summary>
        /// 맵 이동 연출. 도달 위치에 포탈 오브젝트를 활성화하고 카메라 줌(0~1s) 후 화면이 어두워지며(1~2s),
        /// 암전 시점에 onMapSwitch로 실제 맵을 교체한다. 이후 페이드 인(2~3s)과 함께 열차가 슬라이드 인되고,
        /// 타임라인이 끝나면 onComplete 호출.
        /// </summary>
        public void StartMapMoveTimeline(Action onMapSwitch, Action onComplete = null)
        {
            TimeManager.Instance?.Pause();
            _MoveAnchorToTrain();

            if (screenFade != null)
            {
                screenFade.alpha = 0f;
                screenFade.gameObject.SetActive(true);
                screenFade.DOFade(1f, mapMoveFadeDuration)
                    .SetDelay(mapMoveFadeDelay)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        onMapSwitch?.Invoke();
                        TrainManager.Instance?.MainTrain?.SlideIn(mapMoveFadeDuration * 0.5f, mapMoveSlideInDuration);

                        screenFade.DOFade(0f, mapMoveFadeDuration)
                            .SetUpdate(true)
                            .OnComplete(() => screenFade.gameObject.SetActive(false));
                    });
            }
            else
            {
                onMapSwitch?.Invoke();
                TrainManager.Instance?.MainTrain?.SlideIn(mapMoveFadeDelay, mapMoveSlideInDuration);
            }

            _Play(mapMoveDirector, () =>
            {
                if (portalObject != null)
                    portalObject.SetActive(false);
                onComplete?.Invoke();
            });
        }

        private void _Play(PlayableDirector director, Action onComplete)
        {
            director.Stop();
            director.time = 0;

            if (onComplete != null)
            {
                void Handler(PlayableDirector _)
                {
                    director.stopped -= Handler;
                    onComplete();
                }

                director.stopped += Handler;
            }

            director.Play();
        }

        private void _MoveAnchorToTrain()
        {
            if (effectAnchor == null)
                return;

            var mainTrain = TrainManager.Instance?.MainTrain;
            if (mainTrain != null)
                effectAnchor.position = mainTrain.transform.position;
        }

        private void _OnInspectionEnd(InspectionEndEvent _)
        {
            // 상점 오브젝트는 상점이 열려 있는 동안 유지(PostPlaybackState: LeaveAsIs)되므로 닫힐 때 끈다.
            if (shopArrivalObject != null)
                shopArrivalObject.SetActive(false);
        }

        public bool IsTimelinePlaying()
        {
            Debug.Log("IsTimelinePlaying: " + playableDirector.state);
            return playableDirector.state == PlayState.Playing;
        }

        public bool IsTimelineEnd()
        {
            return playableDirector.state == PlayState.Paused;
        }
    }
}
