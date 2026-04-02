using Cumic;
using UnityEngine;

namespace Cumic.UI
{
    public class ChangeSceneButton : MonoBehaviour
    {
        public void OnClickNextScene()
        {
            SceneController.NextScene(false);
        }

        public void OnClickPreviousScene()
        {
            SceneController.PreviousScene();
        }

        public void OnClickResetScene()
        {
            SceneController.ResetScene();
        }

        public void OnClickLoadScene(int sceneIndex)
        {
            SceneController.LoadScene(sceneIndex, false);
        }
    }
}