using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cumic
{
    public static class SceneController
    {
        public static void LoadScene(int sceneIndex)
        {
            SceneManager.LoadScene(sceneIndex);
        }

        public static void NextScene()
        {
            if (SceneManager.GetActiveScene().buildIndex == SceneManager.sceneCountInBuildSettings - 1)
            {
                Debug.LogError("No more scenes");
                return;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

        public static void ResetScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public static void PreviousScene()
        {
            if (SceneManager.GetActiveScene().buildIndex == 0)
            {
                Debug.LogError("No more scenes");
                return;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
        }
    }
}