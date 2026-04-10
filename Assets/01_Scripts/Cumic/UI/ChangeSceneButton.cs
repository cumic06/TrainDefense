using Cumic;
using TrainDefense.Game;
using TrainDefense.Game.Datas;
using UnityEngine;

namespace Cumic.UI
{
    public class ChangeSceneButton : MonoBehaviour
    {
        private const int LobbySceneIndex = 1;

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
            if (sceneIndex == LobbySceneIndex && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBGM(SoundType.BGM_Lobby);
            }

            SceneController.LoadScene(sceneIndex, false);
        }
    }
}