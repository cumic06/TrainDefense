using UnityEngine;

namespace TrainDefense.Game
{
    public class TimeManager : Singleton<TimeManager>
    {
        private void Start()
        {
            Resume();
        }

        public void Pause()
        {
            Time.timeScale = 0;
        }

        public void Resume()
        {
            Time.timeScale = 1;
        }
    }
}