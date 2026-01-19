using UnityEngine;
using UnityEngine.Playables;

namespace TrainDefense
{
    public class TimelineManager : MonoBehaviour
    {
        [SerializeField]
        private PlayableDirector playableDirector;
        [SerializeField]
        private bool startTimelineOnAwake = false;

        private void Awake()
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
    }
}
