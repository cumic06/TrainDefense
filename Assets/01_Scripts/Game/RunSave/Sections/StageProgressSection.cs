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
            public int totalStationPassedCount;
            // 단계 기능 전 세이브에는 없는 필드 → JsonUtility가 0으로 채우고 StageManager가 1단계로 본다.
            public int difficultyStage;
            public bool shouldShowStageSelectionOnStageEnd;
            public bool pendingStageSelectionAfterShop;
            // 무한모드 기능 전 세이브에는 없는 필드 → false(종착역에서 다시 묻는다).
            public bool isInfiniteMode;
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

        /// <summary>섹션 JSON에서 무한모드 여부를 읽는다(로비 포기 결과창의 클리어/패배 제목용). 없거나 깨졌으면 false.</summary>
        public static bool ReadIsInfiniteMode(string json)
        {
            if (string.IsNullOrEmpty(json))
                return false;

            try
            {
                var payload = JsonUtility.FromJson<Payload>(json);

                return payload != null && payload.isInfiniteMode;
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                return false;
            }
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
                totalStationPassedCount = state.TotalStationPassedCount,
                difficultyStage = state.DifficultyStage,
                shouldShowStageSelectionOnStageEnd = state.ShouldShowStageSelectionOnStageEnd,
                pendingStageSelectionAfterShop = state.PendingStageSelectionAfterShop,
                isInfiniteMode = state.IsInfiniteMode,
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
                TotalStationPassedCount = payload.totalStationPassedCount,
                DifficultyStage = payload.difficultyStage,
                ShouldShowStageSelectionOnStageEnd = payload.shouldShowStageSelectionOnStageEnd,
                PendingStageSelectionAfterShop = payload.pendingStageSelectionAfterShop,
                IsInfiniteMode = payload.isInfiniteMode,
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
