using Cumic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrainDefense.Game.RunSave
{
    /// <summary>
    /// 로비 씬이 로드될 때 저장된 런이 남아 있으면 로비에 머무르지 않고 곧바로 그 판으로 되돌아간다.
    ///
    /// 로비를 거치지 않는 이유는 <b>판이 살아 있는 동안 영구 강화를 사서 그 판에 반영시키는 구멍</b>을 막기 위해서다.
    /// 포탑 스탯은 스폰 시점에 <see cref="PermanentUpgradeManager"/>·SkillTreeManager를 그 자리에서 조회하므로
    /// (<c>Train.cs</c> 최대 체력 계산), 로비에서 강화를 사고 이어하면 그 판이 즉시 강해진다.
    /// 세이브가 살아남는 경로는 "판 도중 앱 종료" 하나뿐이라, 그 복귀 지점만 막으면 구멍이 닫힌다.
    ///
    /// 판을 그만두려면 인게임 일시정지의 '포기'를 쓴다. 그때 세이브가 지워지므로 다음 실행은 평소대로 로비로 들어온다.
    /// 로비 씬 자체를 편집하지 않는 것이 이 프로젝트의 관례라 씬 로드 훅으로 붙인다.
    /// </summary>
    public static class RunContinueAutoEntry
    {
        private const string LOBBY_SCENE_NAME = "01_LobbyScene";

        // 02_GameScene. RunContinueButton이 쓰던 값과 같다.
        private const int GAME_SCENE_INDEX = 2;

        // LoadingScene은 빌드 목록에서 꺼져 있어 이름으로 로드하면 실패한다("couldn't be loaded because it has not
        // been added to the active build profile"). 이 프로젝트의 다른 씬 전환도 전부 로딩 씬을 거치지 않는다.
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

            _ResumeIfSaveExists().Forget();
        }

        // 씬 로드 콜백 안에서 다음 씬을 부르지 않도록 한 프레임 미룬다.
        private static async UniTaskVoid _ResumeIfSaveExists()
        {
            await UniTask.NextFrame();

            if (RunSaveManager.LoadSummary() == null)
                return;

            var runSaveManager = RunSaveManager.Instance;

            if (runSaveManager == null)
            {
                Debug.LogWarning("[RunSave] RunSaveManager가 없어 자동 이어하기를 건너뜁니다. 로비에 남습니다.");

                return;
            }

            // 세이브가 사라졌거나 읽지 못하면 로비에 남는다. BeginContinue가 디스크의 세이브를 이미 가져갔으므로
            // 복원이 뒤에서 실패해도 다음 실행은 로비로 들어온다(부팅 루프가 생기지 않는다).
            if (!runSaveManager.BeginContinue())
                return;

            SceneController.LoadScene(GAME_SCENE_INDEX, USE_LOADING_SCENE);
        }
    }
}
