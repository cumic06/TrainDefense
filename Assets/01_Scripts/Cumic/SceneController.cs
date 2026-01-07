using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cumic
{
    public static class SceneController
    {
        private const string LoadingSceneName = "LoadingScene";

        public static void LoadScene(int sceneIndex)
        {
            if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError($"SceneController: Invalid scene index {sceneIndex}");
                return;
            }

            SceneManager.LoadScene(LoadingSceneName);
            LoadingSceneController.SetTargetSceneIndex(sceneIndex);
        }

        public static void NextScene()
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            if (currentIndex == SceneManager.sceneCountInBuildSettings - 1)
            {
                Debug.LogError("SceneController: No more scenes");
                return;
            }

            int nextIndex = currentIndex + 1;
            LoadingSceneController.SetTargetSceneIndex(nextIndex);
            SceneManager.LoadScene(LoadingSceneName);
        }

        public static void ResetScene()
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            LoadingSceneController.SetTargetSceneIndex(currentIndex);
            SceneManager.LoadScene(LoadingSceneName);
        }

        public static void PreviousScene()
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            if (currentIndex == 0)
            {
                Debug.LogError("SceneController: No more scenes");
                return;
            }

            int previousIndex = currentIndex - 1;
            LoadingSceneController.SetTargetSceneIndex(previousIndex);
            SceneManager.LoadScene(LoadingSceneName);
        }
    }
}