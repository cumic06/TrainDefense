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

        /// <summary>섹션 JSON에서 점수·처치 수를 읽는다(로비 포기 결과창용). 없거나 깨졌으면 false.</summary>
        public static bool TryRead(string json, out int score, out int normalKill, out int eliteKill)
        {
            score = 0;
            normalKill = 0;
            eliteKill = 0;

            if (string.IsNullOrEmpty(json))
                return false;

            try
            {
                var payload = JsonUtility.FromJson<Payload>(json);

                if (payload == null)
                    return false;

                score = payload.currentScore;
                normalKill = payload.normalKillCount;
                eliteKill = payload.eliteKillCount;

                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                return false;
            }
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
