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
                // 세이브 로드는 외부 경계 — 업데이트로 형식이 바뀌었거나 손상된 세이브가 예외를 던지면
                // UserDataManager.Start()가 중간에 끊겨 TutorialSaveData가 null로 남고, 그걸 기다리는
                // IntroManager(WaitUntil)가 영영 진행하지 못해 게임이 멈춘다. 읽지 못하면 빈 진행으로 시작한다.
                SerializableData data = null;
                try
                {
                    data = JsonUtility.FromJson<SerializableData>(json);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[TutorialSaveData] 세이브를 읽지 못해 튜토리얼 진행을 초기 상태로 시작합니다: {json}");
                    Debug.LogException(e);
                }

                if (data == null)
                    return saveData;

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
