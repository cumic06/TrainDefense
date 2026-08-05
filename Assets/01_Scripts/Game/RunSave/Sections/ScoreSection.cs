using System;
using TrainDefense.Game.Manager;
using UnityEngine;

namespace TrainDefense.Game.RunSave.Sections
{
    /// <summary>점수·처치 수 섹션.</summary>
    public class ScoreSection : IRunStateSection
    {
        public string SectionId => "score";

        [Serializable]
        private class Payload
        {
            public int currentScore;
            public int normalKillCount;
            public int eliteKillCount;
        }

        public string Capture()
        {
            var scoreManager = ScoreManager.Instance;

            if (scoreManager == null)
                return null;

            var payload = new Payload
            {
                currentScore = scoreManager.CurrentScore,
                normalKillCount = scoreManager.NormalKillCount,
                eliteKillCount = scoreManager.EliteKillCount,
            };

            return JsonUtility.ToJson(payload);
        }

        public void Restore(string json)
        {
            var scoreManager = ScoreManager.Instance;

            if (scoreManager == null)
                return;

            Payload payload = null;
            try
            {
                payload = JsonUtility.FromJson<Payload>(json);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (payload == null)
                return;

            scoreManager.RestoreRunState(payload.currentScore, payload.normalKillCount, payload.eliteKillCount);
        }
    }
}
