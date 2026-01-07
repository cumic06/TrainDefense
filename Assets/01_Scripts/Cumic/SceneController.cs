using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cumic
{
    public static class SceneController
    {
        private const string LoadingSceneName = "LoadingScene";

        public static void LoadScene(int sceneIndex, bool isLoadingScene = true)
        {
            if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError($"SceneController: Invalid scene index {sceneIndex}");
                return;
            }

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