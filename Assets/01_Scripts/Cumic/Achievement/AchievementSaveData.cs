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

        /// <summary>
        /// 저장된 업적 진행/달성 데이터를 삭제한다. (초기화)
        /// </summary>
        public static void Delete()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        public static AchievementSaveData Load()
        {
            var saveData = new AchievementSaveData();

            if (PlayerPrefs.HasKey(SaveKey))
            {
                var json = PlayerPrefs.GetString(SaveKey);

                // 세이브 로드는 외부 경계 — 업데이트로 형식이 바뀌었거나 손상된 세이브가 예외를 던지면
                // 업적 트래커 초기화와 로비 업적 팝업이 통째로 끊긴다. 읽지 못하면 빈 진행으로 시작한다.
                SerializableData data = null;
                try
                {
                    data = JsonUtility.FromJson<SerializableData>(json);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AchievementSaveData] 세이브를 읽지 못해 업적 진행을 초기 상태로 시작합니다: {json}");
                    Debug.LogException(e);
                }

                if (data?.states != null)
                {
                    foreach (var state in data.states)
                    {
                        // 키가 비면 Dictionary 인덱서가 ArgumentNullException을 던진다 — 깨진 항목만 건너뛴다.
                        if (state == null || string.IsNullOrEmpty(state.AchievementId))
                            continue;

                        saveData._states[state.AchievementId] = state;
                    }
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
