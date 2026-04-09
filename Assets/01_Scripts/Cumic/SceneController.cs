using UnityEngine;
using UnityEngine.SceneManagement;
using TrainDefense.Game;

namespace Cumic
{
    public static class SceneController
    {
        private const string LoadingSceneName = "LoadingScene";

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

            if (isLoadingScene)
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

            if (isLoadingScene)
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
            if (isLoadingScene)
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