using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼 완료 상태 저장 데이터 (PlayerPrefs 기반)
    /// </summary>
    [Serializable]
    public class TutorialSaveData
    {
        private const string SaveKey = "TutorialSaveData";

        private HashSet<string> _completedSequenceIds = new();
        private HashSet<string> _completedStepIds = new();

        public bool IsSequenceCompleted(string sequenceId)
        {
            return _completedSequenceIds.Contains(sequenceId);
        }

        public bool IsStepCompleted(string stepId)
        {
            return _completedStepIds.Contains(stepId);
        }

        public void MarkSequenceCompleted(string sequenceId)
        {
            if (_completedSequenceIds.Add(sequenceId))
                Save();
        }

        public void MarkStepCompleted(string stepId)
        {
            if (_completedStepIds.Add(stepId))
                Save();
        }

        public void ResetAll()
        {
            _completedSequenceIds.Clear();
            _completedStepIds.Clear();
            Save();
        }

        #region PlayerPrefs Persistence

        public void Save()
        {
            var data = new SerializableData
            {
                completedSequenceIds = new List<string>(_completedSequenceIds),
                completedStepIds = new List<string>(_completedStepIds)
            };
            var json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        public static TutorialSaveData Load()
        {
            var saveData = new TutorialSaveData();

            if (PlayerPrefs.HasKey(SaveKey))
            {
                var json = PlayerPrefs.GetString(SaveKey);
                var data = JsonUtility.FromJson<SerializableData>(json);

                if (data.completedSequenceIds != null)
                    saveData._completedSequenceIds = new HashSet<string>(data.completedSequenceIds);
                if (data.completedStepIds != null)
                    saveData._completedStepIds = new HashSet<string>(data.completedStepIds);
            }

            return saveData;
        }

        [Serializable]
        private class SerializableData
        {
            public List<string> completedSequenceIds = new();
            public List<string> completedStepIds = new();
        }

        #endregion
    }
}
