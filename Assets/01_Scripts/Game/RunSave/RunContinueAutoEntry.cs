using Cumic;
using Cysharp.Threading.Tasks;
using TrainDefense.Game.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrainDefense.Game.RunSave
{
    /// <summary>
    /// 판 도중 앱을 껐다가 다시 켜서 로비 씬이 로드될 때 저장된 런이 남아 있으면
    /// "이어하기 / 포기하기" 선택 팝업(<see cref="RunContinuePopupUI"/>)을 띄운다.
    /// - 이어하기: 세이브를 싣고 곧바로 그 판으로 되돌아간다.
    /// - 포기하기: 세이브를 지우고 로비에서 바로 결과창을 띄운다(<see cref="RunGiveUpResultPresenter"/>).
    ///   게임 씬을 거치지 않으므로 결과·개조 해금은 세이브에 적힌 값으로 처리한다.
    ///
    /// 팝업은 화면 전체를 딤으로 막아, 고르기 전에는 로비를 만질 수 없다.
    /// <b>판이 살아 있는 동안 영구 강화를 사서 그 판에 반영시키는 구멍</b>을 막기 위해서다.
    /// 포탑 스탯은 스폰 시점에 <see cref="PermanentUpgradeManager"/>·SkillTreeManager를 그 자리에서 조회하므로
    /// (<c>Train.cs</c> 최대 체력 계산), 로비에서 강화를 사고 이어하면 그 판이 즉시 강해진다.
    /// 세이브가 살아남는 경로는 "판 도중 앱 종료" 하나뿐이라, 그 복귀 지점만 막으면 구멍이 닫힌다.
    ///
    /// 로비 씬 자체를 편집하지 않는 것이 이 프로젝트의 관례라 씬 로드 훅으로 붙인다.
    /// </summary>
    public static class RunContinueAutoEntry
    {
        private const string LOBBY_SCENE_NAME = "01_LobbyScene";

        // 02_GameScene.
        private const int GAME_SCENE_INDEX = 2;

        // 이 프로젝트의 다른 씬 전환도 전부 로딩 씬을 거치지 않는다.
        // (LoadingScene은 빌드 목록에서 꺼져 있다 — SceneController가 그 경우를 걸러낸다.)
        private const bool USE_LOADING_SCENE = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void _Bootstrap()
        {
            // 도메인 리로드 없이 플레이를 반복해도 중복 구독되지 않게 한 번 떼고 붙인다.
            SceneManager.sceneLoaded -= _OnSceneLoaded;
            SceneManager.sceneLoaded += _OnSceneLoaded;
        }

        private static void _OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != LOBBY_SCENE_NAME)
                return;

            _AskIfSaveExists().Forget();
        }

        // 씬 로드 콜백 안에서 UI를 띄우거나 다음 씬을 부르지 않도록 한 프레임 미룬다.
        private static async UniTaskVoid _AskIfSaveExists()
        {
            await UniTask.NextFrame();

            var summary = RunSaveManager.LoadSummary();

            if (summary == null)
                return;

            if (RunSaveManager.Instance == null)
            {
                Debug.LogWarning("[RunSave] RunSaveManager가 없어 이어하기 선택을 건너뜁니다. 로비에 남습니다.");

                return;
            }

            RunContinuePopupUI popup = null;
            popup = RunContinuePopupUI.Open(summary.stationPassedCount, summary.playerLevel, _Continue, () => _GiveUp(popup));

            // 팝업 프리팹을 못 찾으면 묻지 못한다. 로비에 그대로 두면 위의 강화 구멍이 열리므로 예전처럼 바로 이어간다.
            if (popup == null)
                _Continue();
        }

        private static void _Continue()
        {
            var runSaveManager = RunSaveManager.Instance;

            if (runSaveManager == null)
                return;

            // 세이브가 사라졌거나 읽지 못하면 로비에 남는다. BeginContinue가 디스크의 세이브를 이미 가져갔으므로
            // 복원이 뒤에서 실패해도 다음 실행은 로비로 들어온다(부팅 루프가 생기지 않는다).
            if (!runSaveManager.BeginContinue())
                return;

            SceneController.LoadScene(GAME_SCENE_INDEX, USE_LOADING_SCENE);
        }

        private static void _GiveUp(RunContinuePopupUI popup)
        {
            // 세이브를 먼저 디스크에서 지운다 — 결과창을 띄우다 실패해도 다음 실행이 다시 묻는 루프에 빠지지 않는다.
            var data = RunSaveStore.Load();
            RunSaveStore.Delete();

            if (data == null)
                return;

            RunGiveUpResultPresenter.Show(data, popup != null ? popup.ResultPrefab : null);
        }
    }
}
