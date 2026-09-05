using System;
using System.Collections;
using Cumic;
using Cumic.Events;
using DG.Tweening;
using TrainDefense.Game;
using TrainDefense.Game.Events;
using TrainDefense.Game.UI;
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
        private bool startTimelineOnAwake = false;

        [Header("연출 오브젝트")]
        [SerializeField]
        private GameObject shopArrivalObject;
        [SerializeField]
        private CanvasGroup screenFade;

        [Header("상점 연출 타이밍")]
        [SerializeField]
        [Tooltip("상점 진입 시 기차가 화면 밖에서 정위치로 슬라이드 인하며 도착하는 시간(카메라 줌인 시작 전에 끝나야 함)")]
        private float shopEnterSlideInDuration = 2f;
        [SerializeField]
        [Tooltip("상점 퇴장 시 기차가 화면 왼쪽 밖에서 정위치(중앙)로 슬라이드 인하는 시간(클수록 천천히 중앙으로 들어옴)")]
        private float shopExitSlideOutDuration = 2.5f;

        [Header("등장 연출 타이밍")]
        [SerializeField]
        [Tooltip("게임 시작 기차 등장 타임라인(LoadingTimeline, 3초)의 재생 배속. 2면 절반, 1.2면 약 2.5초")]
        private float entranceTimelineSpeed = 1.2f;
        #endregion

        #region LifeCycle

        private void Start()
        {
            GameEventSystem.Subscribe<InspectionEndEvent>(_OnInspectionEnd);

            if (startTimelineOnAwake)
                StartTimeline();
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<InspectionEndEvent>(_OnInspectionEnd);
        }

        #endregion

        public void StartTimeline(Action onComplete = null)
        {
            TimeManager.Instance?.Pause();
            // 카메라 등장 줌인은 LoadingTimeline의 카메라 트랙(음소거) 대신 CameraController가 이 이벤트로 연출한다.
            // (타임라인이 Lens.OrthographicSize를 직접 키잉하면 연출 후 씬 카메라 크기 설정이 무시된 채 키값에 고정됨)
            GameEventSystem.Publish(new TrainEntranceStartEvent());
            _Play(playableDirector, onComplete);

            // 등장 타임라인만 배속한다. 그래프는 Play() 직후 만들어지므로 여기서 잡을 수 있다.
            if (entranceTimelineSpeed > 0f && playableDirector.playableGraph.IsValid() && playableDirector.playableGraph.GetRootPlayableCount() > 0)
                playableDirector.playableGraph.GetRootPlayable(0).SetSpeed(entranceTimelineSpeed);
        }

        /// <summary>
        /// 상점(점검) 진입 연출. 기차가 화면 오른쪽 밖으로 슬라이드 아웃하며 화면이 검게 페이드 아웃되고,
        /// 가려진 사이 onComplete로 상점을 띄운 뒤 페이드 인하며 상점을 드러낸다.
        /// 상점 오브젝트는 상점이 닫힐 때(InspectionEndEvent) 비활성화된다.
        /// </summary>
        public void StartShopEnterTimeline(Action onComplete = null)
        {
            // 1. 시간을 먼저 멈춘다. (정리 도중·직후 포탑이 한 발 더 발사하면 그 탄환이 정리에서 누락되므로, 정지 상태에서 정리한다.)
            TimeManager.Instance?.Pause();

            // 2. 몬스터를 제외한 모든 투사체·파티클을 정리한다(몬스터는 화면에 그대로 둔다 — ReturnAll이 Monster를 건너뜀).
            ResourceManager.Instance?.ReturnAll();
            // ReturnAll은 persistent(기차)의 자식을 건너뛰므로, 기차 하위에 부착된 투사체(범위 공격·화염 파티클 등)는 따로 정리한다.
            _ClearAttachedProjectiles();

            StartCoroutine(_ShopEnterCompletionRoutine(onComplete));
        }

        private IEnumerator _ShopEnterCompletionRoutine(Action onComplete)
        {
            // 1. 기차가 화면 오른쪽 밖으로 슬라이드 아웃하며 동시에 화면을 검게 페이드 아웃한다(퇴장의 역재생, 기차가 나가며 같이 어두워짐).
            TrainManager.Instance?.MainTrain?.SlideOut(0f, shopEnterSlideInDuration);
            yield return _Fade(1f, shopEnterSlideInDuration);

            // 2. 검게 가려진 화면 위에 상점을 띄운다.
            onComplete?.Invoke();
        }

        /// <summary>
        /// 상점(점검) 퇴장 연출. 화면을 검게 페이드 아웃해 상점을 가린 뒤, 기차가 화면 왼쪽 밖에서
        /// 정위치(중앙)로 슬라이드 인하며 화면을 페이드 인한다. 기차가 도착하면 onComplete 호출(전투 재개 타이밍).
        /// </summary>
        public void StartShopExitTimeline(Action onComplete = null)
        {
            TimeManager.Instance?.Pause();

            // 상점 들어가기 전 전투에서 남은 골드·투사체가 퇴장 후 화면에 그대로 남는 문제를 막는다.
            // 진입과 동일하게 정지 상태에서 정리한다(상점 중 부활/긴급수리 등으로 생긴 잔재, 진입 정리 직후의 막탄 포함).
            ResourceManager.Instance?.ReturnAll();
            _ClearAttachedProjectiles();

            StartCoroutine(_ShopExitRoutine(onComplete));
        }

        private IEnumerator _ShopExitRoutine(Action onComplete)
        {
            var mainTrain = TrainManager.Instance?.MainTrain;

            // 1. 진입에서 이미 검게 가려진 상태이므로, 그 사이 기차를 정위치로 즉시 복귀시킨다(추가 페이드 아웃 없음).
            mainTrain?.ResetSlidePosition();
            // 상점 중 부활·재배치로 편성이 바뀌었을 수 있으므로 전체를 정위치로 재확정한다(포탑 겹침 방지).
            mainTrain?.RearrangeAllTrainsToOriginalOrder();

            // 2. 기차가 화면 왼쪽 밖에서 정위치(중앙)로 들어오며 화면을 페이드 인한다.
            mainTrain?.SlideIn(0f, shopExitSlideOutDuration);
            yield return _Fade(0f, shopExitSlideOutDuration);

            // 3. 기차가 정위치에 도착하면 전투를 재개한다.
            onComplete?.Invoke();
        }

        // screenFade 오버레이를 targetAlpha(0=투명, 1=검정)로 페이드하고 끝날 때까지 실시간 대기한다.
        // 게임 정지(timeScale=0) 중에도 진행되도록 SetUpdate(true). 페이드 아웃(검정)은 항상 투명에서 시작한다.
        private IEnumerator _Fade(float targetAlpha, float duration)
        {
            if (screenFade == null)
                yield break;

            screenFade.gameObject.SetActive(true);
            if (targetAlpha >= 1f)
                screenFade.alpha = 0f;
            screenFade.DOFade(targetAlpha, duration).SetUpdate(true);

            yield return new WaitForSecondsRealtime(duration);

            if (targetAlpha <= 0f)
                screenFade.gameObject.SetActive(false);
        }

        /// <summary>
        /// 맵 이동 연출. 스테이지 선택 시점엔 화면이 (상점 진입 때 만든) 검정으로 가려진 상태이므로,
        /// 그 사이 onMapSwitch로 맵을 새 맵으로 교체하고, 기차가 화면 왼쪽 밖에서 정위치로 슬라이드 인하며
        /// 화면을 페이드 인한다(상점 퇴장과 동일). 기차가 도착하면 onComplete 호출(새 맵 전투 시작).
        /// </summary>
        public void StartMapMoveTimeline(Action onMapSwitch, Action onComplete = null)
        {
            TimeManager.Instance?.Pause();

            // 이전 맵에서 남은 골드·투사체가 새 맵으로 넘어오지 않도록 맵 교체 전 정지 상태에서 정리한다(몬스터는 별도 정리).
            ResourceManager.Instance?.ReturnAll();
            _ClearAttachedProjectiles();

            StartCoroutine(_MapMoveRoutine(onMapSwitch, onComplete));
        }

        private IEnumerator _MapMoveRoutine(Action onMapSwitch, Action onComplete)
        {
            var mainTrain = TrainManager.Instance?.MainTrain;

            // 1. 스테이지 선택 시점에 화면이 이미 검게 가려져 있으므로, 그 사이 맵을 새 맵으로 교체한다.
            onMapSwitch?.Invoke();

            // 2. 검게 가려진 사이 기차를 정위치로 즉시 복귀시킨다(상점 진입에서 오른쪽 밖에 나가 있던 상태).
            mainTrain?.ResetSlidePosition();
            // 부활·재배치로 편성이 바뀌었을 수 있으므로 전체를 정위치로 재확정한다(포탑 겹침 방지).
            mainTrain?.RearrangeAllTrainsToOriginalOrder();

            // 3. 기차가 화면 왼쪽 밖에서 정위치(중앙)로 들어오며 화면을 페이드 인한다(상점 퇴장과 동일).
            mainTrain?.SlideIn(0f, shopExitSlideOutDuration);
            yield return _Fade(0f, shopExitSlideOutDuration);

            // 4. 기차가 정위치에 도착하면 새 맵 전투를 시작한다.
            onComplete?.Invoke();
        }

        // 디렉터별 대기 중인 stopped 핸들러. 재생 중 같은 디렉터로 _Play가 재호출되면
        // Stop()이 직전 핸들러를 즉시 발화시켜 onComplete(EngageStart 등)가 조기 실행되므로 먼저 해제한다.
        private readonly System.Collections.Generic.Dictionary<PlayableDirector, Action<PlayableDirector>> _pendingStopHandlers = new();

        private void _Play(PlayableDirector director, Action onComplete)
        {
            if (_pendingStopHandlers.TryGetValue(director, out var previousHandler))
            {
                director.stopped -= previousHandler;
                _pendingStopHandlers.Remove(director);
            }

            director.Stop();
            director.time = 0;

            if (onComplete != null)
            {
                void Handler(PlayableDirector _)
                {
                    director.stopped -= Handler;
                    _pendingStopHandlers.Remove(director);
                    onComplete();
                }

                director.stopped += Handler;
                _pendingStopHandlers[director] = Handler;
            }

            director.Play();
        }

        // 기차(또는 그 스폰포인트) 하위에 부착된 채 남는 공격 투사체(범위 공격·화염 파티클 등)를 정리한다. ReturnAll이 놓치는 persistent 자식 정리용.
        private void _ClearAttachedProjectiles()
        {
            var trains = TrainManager.Instance?.MainTrain?.CurrentAliveTrains;
            if (trains == null) return;

            foreach (var train in trains)
                train?.ClearAttachedProjectiles();
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
