using Cumic.UI;
using UnityEngine;

namespace Cumic
{
    public class GameSceneSequence : MonoBehaviour
    {
        private ISceneSequencer _authSceneSequencer;

        private void Start()
        {
            DontDestroyOnLoad(gameObject);

            _authSceneSequencer = new AuthSceneSequencer(this);
        }

        public void PopupUI(string uiName)
        {
            if (UIManager.Instance == null)
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                canvas.gameObject.AddComponent<UIManager>();
            }
            UIManager.Instance.ShowPopup(uiName);
        }
    }
}