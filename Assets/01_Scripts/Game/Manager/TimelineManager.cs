using System;
using Cumic;
using TrainDefense.Game;
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
        private bool startTimelineOnAwake = false;
        #endregion

        #region LifeCycle

        private void Start()
        {
            if (startTimelineOnAwake)
            {
                StartTimeline();
            }
        }

        #endregion

        public void StartTimeline(bool isMapChange = false, Action onComplete = null)
        {
            var director = isMapChange && mapChangeDirector != null ? mapChangeDirector : playableDirector;

            TimeManager.Instance?.Pause();

            // 자식 열차들의 localPosition을 오프셋했다가 복귀 (부모 위치 고정 → 카메라 따라오지 않음)
            // Timeline: 카메라 줌 0~1s / 열차 슬라이드: delay=1s, duration=2s → 총 3s
            if (isMapChange)
                TrainManager.Instance?.MainTrain?.SlideIn(-15f, 1f, 2f);

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
