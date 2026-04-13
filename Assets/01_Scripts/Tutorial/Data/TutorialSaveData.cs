using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼 완료 상태 데이터 (순수 데이터, 저장은 UserDataManager에서 관리)
    /// </summary>
    [Serializable]
    public class TutorialSaveData
    {
        private HashSet<string> _completedSequenceIds = new();
        private HashSet<string> _completedStepIds = new();

        public event Action OnDataChanged;

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
                OnDataChanged?.Invoke();
        }

        public void MarkStepCompleted(string stepId)
        {
            if (_completedStepIds.Add(stepId))
                OnDataChanged?.Invoke();
        }

        public void ResetAll()
        {
            _completedSequenceIds.Clear();
            _completedStepIds.Clear();
            OnDataChanged?.Invoke();
        }

        #region Serialization

        public string ToJson()
        {
            var data = new SerializableData
            {
                completedSequenceIds = new List<string>(_completedSequenceIds),
                completedStepIds = new List<string>(_completedStepIds)
            };
            return JsonUtility.ToJson(data);
        }

        public static TutorialSaveData FromJson(string json)
        {
            var saveData = new TutorialSaveData();

            if (!string.IsNullOrEmpty(json))
            {
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
