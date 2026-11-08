using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.RunSave.Sections
{
    /// <summary>
    /// 저장 시점에 해금 조건을 만족한 개조 ID 섹션.
    /// 개조 해금은 판이 끝날 때 살아 있는 포탑의 스탯·전투 기록으로 판정하는데(UserDataManager), 로비에서 포기하면
    /// 포탑이 없어 판정할 수 없다. 그래서 저장할 때 미리 판정해 두고, 로비 포기 결과창은 이 목록으로 해금한다.
    /// 복원은 하지 않는다 — 이어하기로 판을 끝내면 평소처럼 살아 있는 포탑으로 다시 판정한다.
    /// </summary>
    public class RemodelUnlockSection : IRunStateSection
    {
        public string SectionId => "remodel_unlock";

        [Serializable]
        private class Payload
        {
            public List<string> satisfiedRemodelIds = new();
        }

        public string Capture()
        {
            var mainTrain = TrainManager.Instance != null ? TrainManager.Instance.MainTrain : null;

            if (mainTrain == null)
                return null;

            var payload = new Payload();
            payload.satisfiedRemodelIds.AddRange(RemodelUnlockEvaluator.FindSatisfiedRemodels(mainTrain.CurrentTrains));

            return JsonUtility.ToJson(payload);
        }

        public void Restore(string json)
        {
        }

        /// <summary>섹션 JSON에서 만족한 개조 ID 목록을 읽는다. 없거나 깨졌으면 빈 목록.</summary>
        public static List<string> ReadSatisfiedRemodelIds(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new List<string>();

            try
            {
                var payload = JsonUtility.FromJson<Payload>(json);

                return payload?.satisfiedRemodelIds ?? new List<string>();
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                return new List<string>();
            }
        }
    }
}
