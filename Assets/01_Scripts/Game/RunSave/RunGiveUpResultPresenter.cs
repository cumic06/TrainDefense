using System.Collections.Generic;
using TrainDefense.Game.Manager;
using TrainDefense.Game.RunSave.Sections;
using TrainDefense.Game.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.RunSave
{
    /// <summary>
    /// 로비에서 판을 포기했을 때 세이브에 적힌 값으로 결과창을 띄운다.
    /// 게임 씬의 결과창(StageResultSlideUI)은 살아 있는 매니저를 읽지만, 로비에는 그 매니저가 없으므로
    /// 점수·처치·획득 재화·무한모드 여부는 세이브 섹션에서, 개조 해금은 저장 시점에 미리 판정해 둔 목록에서 가져온다.
    /// 결과창 안의 로비 버튼은 로비 씬을 다시 불러오므로 캔버스는 씬과 함께 정리된다.
    /// </summary>
    public static class RunGiveUpResultPresenter
    {
        private const string CANVAS_NAME = "RunGiveUpResultCanvas";

        // 팝업 캔버스(100~500)보다 위, 전역 알림 캔버스(900)보다 아래.
        private const int SORTING_ORDER = 600;
        private static readonly Vector2 REFERENCE_RESOLUTION = new(1920f, 1080f);
        private const float MATCH_WIDTH_OR_HEIGHT = 0.5f;

        public static void Show(RunSaveData data, GameObject resultPrefab)
        {
            if (data == null)
                return;

            List<string> unlockedRemodelIds = _GrantUnlocks(data);
            var snapshot = _BuildSnapshot(data, unlockedRemodelIds);

            AnalyticsManager.LogCustomEvent("game_end",
                ("is_clear", snapshot.IsClear),
                ("kill_normal", snapshot.NormalKill),
                ("kill_elite", snapshot.EliteKill),
                ("score", snapshot.TotalScore),
                ("max_station", snapshot.StationCount),
                ("give_up_in_lobby", true));

            if (resultPrefab == null)
            {
                Debug.LogWarning("[RunSave] 결과창 프리팹이 연결되지 않아 로비 포기 결과창을 건너뜁니다.");

                return;
            }

            var canvasObject = new GameObject(CANVAS_NAME);
            canvasObject.layer = LayerMask.NameToLayer("UI");

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SORTING_ORDER;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = REFERENCE_RESOLUTION;
            scaler.matchWidthOrHeight = MATCH_WIDTH_OR_HEIGHT;

            canvasObject.AddComponent<GraphicRaycaster>();

            if (StageResultSlideUI.ShowStandalone(resultPrefab, canvasObject.transform, snapshot) == null)
                Object.Destroy(canvasObject);
        }

        // 저장 시점에 조건을 만족한 개조를 해금한다. 이미 열려 있던 것은 UserDataManager가 걸러 "새로 해금된 것"만 돌려준다.
        private static List<string> _GrantUnlocks(RunSaveData data)
        {
            var satisfiedIds = RemodelUnlockSection.ReadSatisfiedRemodelIds(data.GetSection("remodel_unlock"));
            var userDataManager = UserDataManager.Instance;

            if (userDataManager == null || satisfiedIds.Count == 0)
                return new List<string>();

            int before = userDataManager.LastRunUnlockedRemodelIds.Count;
            userDataManager.GrantRemodelUnlocks(satisfiedIds);

            var newlyUnlocked = new List<string>();

            for (int i = before; i < userDataManager.LastRunUnlockedRemodelIds.Count; i++)
                newlyUnlocked.Add(userDataManager.LastRunUnlockedRemodelIds[i]);

            return newlyUnlocked;
        }

        private static StageResultSlideUI.ResultSnapshot _BuildSnapshot(RunSaveData data, List<string> unlockedRemodelIds)
        {
            var summary = data.summary;

            ScoreSection.TryRead(data.GetSection("score"), out int score, out int normalKill, out int eliteKill);

            // 점수 섹션이 없는 구버전 세이브는 요약값으로 대신한다.
            if (score == 0 && summary != null)
                score = summary.score;

            return new StageResultSlideUI.ResultSnapshot
            {
                IsClear = StageProgressSection.ReadIsInfiniteMode(data.GetSection("stage_progress")),
                StationCount = summary != null ? summary.stationPassedCount : 0,
                Level = summary != null ? summary.playerLevel : 0,
                NormalKill = normalKill,
                EliteKill = eliteKill,
                EliteCoinEarned = EliteCoinSection.ReadRunEliteCoinEarned(data.GetSection("elite_coin")),
                TotalScore = score,
                UnlockedRemodelIds = unlockedRemodelIds,
            };
        }
    }
}
