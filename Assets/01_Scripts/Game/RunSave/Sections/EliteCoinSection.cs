using System;
using UnityEngine;

namespace TrainDefense.Game.RunSave.Sections
{
    /// <summary>
    /// 이번 판에 번 엘리트 재화 합계 섹션. 재화 지갑은 PlayerPrefs에 있으므로 여기엔 "세이브 시점까지의 획득량"만 남기고,
    /// 이어하기 때 PermanentUpgradeManager가 세이브 이후에 번 만큼을 지갑에서 되돌린다(구간을 다시 하며 중복으로 버는 것 방지).
    /// </summary>
    public class EliteCoinSection : IRunStateSection
    {
        public string SectionId => "elite_coin";

        [Serializable]
        private class Payload
        {
            public int runEliteCoinEarned;
        }

        /// <summary>섹션 JSON에서 이번 판 획득 엘리트 코인을 읽는다(로비 포기 결과창용). 없거나 깨졌으면 0.</summary>
        public static int ReadRunEliteCoinEarned(string json)
        {
            if (string.IsNullOrEmpty(json))
                return 0;

            try
            {
                var payload = JsonUtility.FromJson<Payload>(json);

                return payload != null ? Mathf.Max(0, payload.runEliteCoinEarned) : 0;
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                return 0;
            }
        }

        public string Capture()
        {
            var permanentUpgradeManager = PermanentUpgradeManager.Instance;

            if (permanentUpgradeManager == null)
                return null;

            var payload = new Payload
            {
                runEliteCoinEarned = permanentUpgradeManager.RunEliteCoinEarned,
            };

            return JsonUtility.ToJson(payload);
        }

        public void Restore(string json)
        {
            var permanentUpgradeManager = PermanentUpgradeManager.Instance;

            if (permanentUpgradeManager == null)
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

            permanentUpgradeManager.RestoreRunState(payload.runEliteCoinEarned);
        }
    }
}
