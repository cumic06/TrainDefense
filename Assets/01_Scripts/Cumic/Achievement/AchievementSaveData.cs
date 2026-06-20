using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cumic.Achievement
{
    [Serializable]
    public class AchievementSaveData
    {
        private const string SaveKey = "AchievementSaveData";

        private Dictionary<string, AchievementState> _states = new();

        public AchievementState GetState(string achievementId)
        {
            if (!_states.TryGetValue(achievementId, out var state))
            {
                state = new AchievementState(achievementId);
                _states[achievementId] = state;
            }
            return state;
        }

        public void SetState(string achievementId, int currentValue, bool isUnlocked)
        {
            var state = GetState(achievementId);
            state.CurrentValue = currentValue;
            state.IsUnlocked = isUnlocked;
        }

        #region PlayerPrefs Persistence

        public void Save()
        {
            var data = new SerializableData
            {
                states = new List<AchievementState>(_states.Values)
            };
            var json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        public static AchievementSaveData Load()
        {
            var saveData = new AchievementSaveData();

            if (PlayerPrefs.HasKey(SaveKey))
            {
                var json = PlayerPrefs.GetString(SaveKey);
                var data = JsonUtility.FromJson<SerializableData>(json);

                if (data.states != null)
                {
                    foreach (var state in data.states)
                        saveData._states[state.AchievementId] = state;
                }
            }

            return saveData;
        }

        [Serializable]
        private class SerializableData
        {
            public List<AchievementState> states = new();
        }

        #endregion
    }
}
