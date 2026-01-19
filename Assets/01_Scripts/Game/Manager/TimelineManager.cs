using Cumic;
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
        #endregion

        private void Start()
        {
            if (startTimelineOnAwake)
            {
                StartTimeline();
            }
        }

        public void StartTimeline()
        {
            playableDirector.Play();
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
