using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.RunSave.Sections
{
    /// <summary>
    /// 플레이어 진행도 섹션 — 코인·경험치·레벨·삼중택일 선택 이력·상점 업그레이드 레벨·선택 포탑.
    /// 상점 판매 목록은 프리팹에 고정이고 표시 내용이 업그레이드 레벨에서 파생되므로,
    /// 이 섹션이 복원되면 상점도 저장 당시와 같은 상태가 된다.
    /// </summary>
    public class UserProgressSection : IRunStateSection
    {
        public string SectionId => "user_progress";

        [Serializable]
        private class Payload
        {
            public int coin;
            public int currentExp;
            public int currentLevel;
            public List<CountEntry> triChoiceCounts = new();
            public List<CountEntry> upgradeLevels = new();
        }

        [Serializable]
        private class CountEntry
        {
            public string id;
            public int count;
        }

        public string Capture()
        {
            var userDataManager = UserDataManager.Instance;

            if (userDataManager == null)
                return null;

            var payload = new Payload
            {
                coin = userDataManager.Coin,
                currentExp = userDataManager.CurrentExp,
                currentLevel = userDataManager.CurrentLevel,
                triChoiceCounts = _ToEntries(userDataManager.GetTriChoiceCounts()),
                upgradeLevels = _ToEntries(userDataManager.GetUpgradeLevels()),
            };

            return JsonUtility.ToJson(payload);
        }

        public void Restore(string json)
        {
            var userDataManager = UserDataManager.Instance;

            if (userDataManager == null)
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

            userDataManager.RestoreRunState(
                payload.coin,
                payload.currentExp,
                payload.currentLevel,
                _ToDictionary(payload.triChoiceCounts),
                _ToDictionary(payload.upgradeLevels));
        }

        private static List<CountEntry> _ToEntries(IReadOnlyDictionary<string, int> source)
        {
            var entries = new List<CountEntry>();

            if (source == null)
                return entries;

            foreach (var pair in source)
                entries.Add(new CountEntry { id = pair.Key, count = pair.Value });

            return entries;
        }

        private static Dictionary<string, int> _ToDictionary(List<CountEntry> entries)
        {
            var dictionary = new Dictionary<string, int>();

            if (entries == null)
                return dictionary;

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id))
                    continue;

                dictionary[entry.id] = entry.count;
            }

            return dictionary;
        }
    }
}
