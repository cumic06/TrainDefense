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
        private PlayableDirector shopExitDirector;
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
        [Tooltip("맵 이동 시 기차가 scale 0으로 빨려들어가듯 축소되는 시간(페이드 시작 전에 끝나야 함)")]
        private float mapMoveShrinkDuration = 1f;
        [SerializeField]
        [Tooltip("맵 이동 후 새 맵에서 기차가 scale을 회복하며 나타나는 시간")]
        private float mapMoveGrowDuration = 0.5f;

        [Header("상점 연출 타이밍")]
        [SerializeField]
        [Tooltip("상점 진입 시 기차가 화면 밖에서 정위치로 슬라이드 인하며 도착하는 시간(카메라 줌인 시작 전에 끝나야 함)")]
        private float shopEnterSlideInDuration = 2f;
        [SerializeField]
        [Tooltip("상점 퇴장 시 기차가 정위치에서 화면 밖으로 슬라이드 아웃하며 출발하는 시간")]
        private float shopExitSlideOutDuration = 1.5f;
        [SerializeField]
        [Tooltip("상점 퇴장 시 기차가 나간 뒤 정위치 복귀를 가리는 페이드 길이(아웃/인 각각)")]
        private float shopExitFadeDuration = 0.5f;
        [SerializeField]
        [Tooltip("상점 진입 시 카메라가 상점으로 줌인할 OrthographicSize(작을수록 가까이)")]
        private float shopZoomOrthoSize = 6f;
        [SerializeField]
        [Tooltip("상점 진입 시 카메라가 상점으로 줌인하는 시간")]
        private float shopZoomDuration = 1f;
        [SerializeField]
        [Tooltip("상점 퇴장 시 카메라가 기차로 복귀하며 줌아웃하는 시간")]
        private float shopCameraRestoreDuration = 0.8f;
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
        public bool CanPlayShopExitTimeline => shopExitDirector != null;
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
        /// 상점(점검) 진입 연출. 도달 위치에 상점 오브젝트가 고정 활성화되고, 기차가 화면 밖에서
        /// 정위치로 슬라이드 인하며 역에 도착한다(0~2s). 도착하면 카메라가 줌인된다(2~3s).
        /// 연출이 끝나면 onComplete 호출(상점 오픈 타이밍). 상점 오브젝트는 상점이 닫힐 때(InspectionEndEvent) 비활성화된다.
        /// </summary>
        public void StartShopEnterTimeline(Action onComplete = null)
        {
            TimeManager.Instance?.Pause();

            // 입장과 함께 화면의 적과 풀에서 소환된 오브젝트(파티클·투사체 등)를 정리한다.
            // StopSpawnMonster()는 호출하지 않는다 — 일반 상점은 전투 재개 시 스폰을 다시 켜는 흐름(TriChoiceSelect)이 없어
            // 한 번 멈추면 적이 영영 안 나온다. 상점 동안엔 TimeManager.Pause로 스폰 코루틴이 멈췄다가 재개 시 자동으로 다시 돈다.
            MonsterSpawner.Instance?.DestroyAllMonsters();
            ResourceManager.Instance?.ReturnAll();
            // ReturnAll은 persistent(기차)의 자식을 건너뛰므로, 기차 하위에 부착된 투사체(범위 공격·화염 파티클 등)는 따로 정리한다.
            _ClearAttachedProjectiles();

            _MoveAnchorToTrain();
            // 상점은 도착 위치에 고정되고, 기차가 화면 밖에서 정위치로 슬라이드 인하며 상점에 도착한다.
            TrainManager.Instance?.MainTrain?.SlideIn(0f, shopEnterSlideInDuration);

            // 기차가 도착한 뒤(SlideIn 시간 후) 카메라가 상점 오브젝트 쪽으로 줌인한다.
            if (shopArrivalObject != null)
                GameEventSystem.Publish(new CameraZoomEvent(
                    shopArrivalObject.transform, shopZoomOrthoSize, shopZoomDuration, shopEnterSlideInDuration));

            _Play(shopEnterDirector, onComplete);
        }

        /// <summary>
        /// 상점(점검) 퇴장 연출. 상점은 고정된 채 기차가 정위치에서 화면 밖으로 슬라이드 아웃하며 출발한다.
        /// 기차가 나간 뒤 화면을 페이드로 가린 채 기차를 정위치로 복귀시키고, 페이드 인하며 onComplete 호출(전투 재개 타이밍).
        /// </summary>
        public void StartShopExitTimeline(Action onComplete = null)
        {
            TimeManager.Instance?.Pause();

            var mainTrain = TrainManager.Instance?.MainTrain;

            // 상점으로 줌인됐던 카메라를 기차로 되돌리며 줌아웃한다(종횡비 보정도 재개).
            if (mainTrain != null)
                GameEventSystem.Publish(new CameraRestoreEvent(mainTrain.transform, shopCameraRestoreDuration));

            // 상점은 고정되고, 기차가 정위치에서 화면 밖으로 슬라이드 아웃하며 역을 떠난다.
            mainTrain?.SlideOut(0f, shopExitSlideOutDuration);

            if (screenFade != null)
            {
                _Play(shopExitDirector, null);

                // 기차가 나간 뒤 화면을 어둡게 가리고, 암전 동안 기차를 정위치로 즉시 복귀시킨 뒤 페이드 인하며 전투를 재개한다.
                screenFade.alpha = 0f;
                screenFade.gameObject.SetActive(true);
                screenFade.DOFade(1f, shopExitFadeDuration)
                    .SetDelay(shopExitSlideOutDuration)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        mainTrain?.ResetSlidePosition();
                        screenFade.DOFade(0f, shopExitFadeDuration)
                            .SetUpdate(true)
                            .OnComplete(() =>
                            {
                                screenFade.gameObject.SetActive(false);
                                onComplete?.Invoke();
                            });
                    });
            }
            else
            {
                // 폴백: 페이드가 없으면 슬라이드 아웃이 끝난 뒤 기차를 정위치로 복귀하고 즉시 재개한다.
                _Play(shopExitDirector, () =>
                {
                    mainTrain?.ResetSlidePosition();
                    onComplete?.Invoke();
                });
            }
        }

        /// <summary>
        /// 맵 이동 연출. 기차들이 제자리에서 scale 0으로 빨려들어가듯 축소되고(0~1s, 카메라 줌과 함께),
        /// 화면이 어두워지면(1~2s) 암전 시점에 onMapSwitch로 실제 맵을 교체한다.
        /// 이후 새 맵에서 기차가 scale을 회복하며 나타나고(페이드 인), 타임라인이 끝나면 onComplete 호출.
        /// </summary>
        public void StartMapMoveTimeline(Action onMapSwitch, Action onComplete = null)
        {
            TimeManager.Instance?.Pause();

            var mainTrain = TrainManager.Instance?.MainTrain;
            // 포탈 대신 기차들이 제자리에서 scale 0으로 줄어들며 어딘가로 빨려들어가듯 사라진다.
            mainTrain?.ShrinkOut(0f, mapMoveShrinkDuration);

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
                        // 새 맵에서 기차가 scale을 회복하며 다시 나타난다.
                        mainTrain?.GrowIn(0f, mapMoveGrowDuration);

                        screenFade.DOFade(0f, mapMoveFadeDuration)
                            .SetUpdate(true)
                            .OnComplete(() => screenFade.gameObject.SetActive(false));
                    });
            }
            else
            {
                onMapSwitch?.Invoke();
                mainTrain?.GrowIn(0f, mapMoveGrowDuration);
            }

            _Play(mapMoveDirector, () => onComplete?.Invoke());
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

        // 기차(또는 그 스폰포인트) 하위에 부착된 채 남는 공격 투사체(범위 공격·화염 파티클 등)를 정리한다. ReturnAll이 놓치는 persistent 자식 정리용.
        private void _ClearAttachedProjectiles()
        {
            var trains = TrainManager.Instance?.MainTrain?.CurrentAliveTrains;
            if (trains == null) return;

            foreach (var train in trains)
                train?.ClearAttachedProjectiles();
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
