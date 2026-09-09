using UnityEngine;
using UnityEngine.SceneManagement;
using TrainDefense.Game;

namespace Cumic
{
    public static class SceneController
    {
        private const string LoadingSceneName = "LoadingScene";

        // LoadingScene은 빌드 목록에서 꺼져 있을 수 있다. 그 상태로 이름 로드를 하면
        // 에러 로그만 남고 화면이 그대로 멈춰, 호출부는 전환에 성공한 줄 안다.
        // 쓸 수 없으면 로딩 화면을 건너뛰고 목표 씬으로 곧장 넘긴다.
        private static bool _CanUseLoadingScene()
        {
            if (Application.CanStreamedLevelBeLoaded(LoadingSceneName))
                return true;

            Debug.LogWarning($"SceneController: '{LoadingSceneName}'이 빌드 목록에 없어 로딩 화면을 건너뜁니다.");

            return false;
        }

        private static void PrepareForSceneChange()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.Resume();
            }
            else
            {
                Time.timeScale = 1f;
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.StopAllSFX();
            }
        }

        public static void LoadScene(int sceneIndex, bool isLoadingScene = true)
        {
            if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError($"SceneController: Invalid scene index {sceneIndex}");
                return;
            }

            PrepareForSceneChange();

            if (isLoadingScene && _CanUseLoadingScene())
            {
                SceneManager.LoadScene(LoadingSceneName);
                LoadingSceneController.SetTargetSceneIndex(sceneIndex);
            }
            else
            {
                SceneManager.LoadScene(sceneIndex);
            }
        }

        public static void NextScene(bool isLoadingScene = true)
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            if (currentIndex == SceneManager.sceneCountInBuildSettings - 1)
            {
                Debug.LogError("SceneController: No more scenes");
                return;
            }

            int nextIndex = currentIndex + 1;
            PrepareForSceneChange();

            if (isLoadingScene && _CanUseLoadingScene())
            {
                LoadingSceneController.SetTargetSceneIndex(nextIndex);
                SceneManager.LoadScene(LoadingSceneName);
            }
            else
            {
                SceneManager.LoadScene(nextIndex);
            }
        }

        public static void ResetScene(bool isLoadingScene = true)
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            PrepareForSceneChange();
            if (isLoadingScene && _CanUseLoadingScene())
            {
                LoadingSceneController.SetTargetSceneIndex(currentIndex);
                SceneManager.LoadScene(LoadingSceneName);
            }
            else
            {
                SceneManager.LoadScene(currentIndex);
            }
        }

        public static void PreviousScene(bool isLoadingScene = true)
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            if (currentIndex == 0)
            {
                Debug.LogError("SceneController: No more scenes");
                return;
            }

            int previousIndex = currentIndex - 1;
            PrepareForSceneChange();
            if (isLoadingScene)
            {
                LoadingSceneController.SetTargetSceneIndex(previousIndex);
                SceneManager.LoadScene(LoadingSceneName);
            }
            else
            {
                SceneManager.LoadScene(previousIndex);
            }
        }
    }
}