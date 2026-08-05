using System;
using System.Collections.Generic;
using TrainDefense.Game.Manager;
using UnityEngine;

namespace TrainDefense.Game.RunSave.Sections
{
    /// <summary>
    /// 스테이지 진행 섹션 — 현재 맵·역 진행 인덱스·누적 통과 역 수·맵별 결산 기록·스테이지 선택 대기 상태.
    /// 스테이지는 배열 인덱스가 아니라 StageId로 저장한다(DB 순서가 바뀌어도 엉뚱한 맵으로 복원되지 않게).
    /// </summary>
    public class StageProgressSection : IRunStateSection
    {
        public string SectionId => "stage_progress";

        [Serializable]
        private class Payload
        {
            public string currentStageId;
            public float currentStageTime;
            public int currentStageInspectionTimeIndex;
            public int inspectionCount;
            public int totalStationPassedCount;
            public bool shouldShowStageSelectionOnStageEnd;
            public bool pendingStageSelectionAfterShop;
            public int mapStartScore;
            public int mapStartNormalKill;
            public int mapStartEliteKill;
            public List<RecordEntry> runRecords = new();
        }

        [Serializable]
        private class RecordEntry
        {
            public string stageId;
            public int scoreEarned;
            public int normalKill;
            public int eliteKill;
        }

        public string Capture()
        {
            var stageManager = StageManager.Instance;

            if (stageManager == null)
                return null;

            var state = stageManager.CaptureRunState();
            var payload = new Payload
            {
                currentStageId = state.CurrentStageId,
                currentStageTime = state.CurrentStageTime,
                currentStageInspectionTimeIndex = state.CurrentStageInspectionTimeIndex,
                inspectionCount = state.InspectionCount,
                totalStationPassedCount = state.TotalStationPassedCount,
                shouldShowStageSelectionOnStageEnd = state.ShouldShowStageSelectionOnStageEnd,
                pendingStageSelectionAfterShop = state.PendingStageSelectionAfterShop,
                mapStartScore = state.MapStartScore,
                mapStartNormalKill = state.MapStartNormalKill,
                mapStartEliteKill = state.MapStartEliteKill,
            };

            foreach (var record in state.RunRecords)
            {
                payload.runRecords.Add(new RecordEntry
                {
                    stageId = record.StageId,
                    scoreEarned = record.ScoreEarned,
                    normalKill = record.NormalKill,
                    eliteKill = record.EliteKill,
                });
            }

            return JsonUtility.ToJson(payload);
        }

        public void Restore(string json)
        {
            var stageManager = StageManager.Instance;

            if (stageManager == null)
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

            var state = new StageManager.RunState
            {
                CurrentStageId = payload.currentStageId,
                CurrentStageTime = payload.currentStageTime,
                CurrentStageInspectionTimeIndex = payload.currentStageInspectionTimeIndex,
                InspectionCount = payload.inspectionCount,
                TotalStationPassedCount = payload.totalStationPassedCount,
                ShouldShowStageSelectionOnStageEnd = payload.shouldShowStageSelectionOnStageEnd,
                PendingStageSelectionAfterShop = payload.pendingStageSelectionAfterShop,
                MapStartScore = payload.mapStartScore,
                MapStartNormalKill = payload.mapStartNormalKill,
                MapStartEliteKill = payload.mapStartEliteKill,
                RunRecords = new List<StageManager.RunRecordState>(),
            };

            if (payload.runRecords != null)
            {
                foreach (var record in payload.runRecords)
                {
                    if (record == null)
                        continue;

                    state.RunRecords.Add(new StageManager.RunRecordState
                    {
                        StageId = record.stageId,
                        ScoreEarned = record.scoreEarned,
                        NormalKill = record.normalKill,
                        EliteKill = record.eliteKill,
                    });
                }
            }

            stageManager.RestoreRunState(state);
        }
    }
}
