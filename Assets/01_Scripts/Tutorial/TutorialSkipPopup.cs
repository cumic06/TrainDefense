using System;
using UnityEngine;
using UnityEngine.UI;
using TrainDefense.Game.UI;

namespace TrainDefense.Game.Tutorial
{
    public class TutorialSkipPopup : MonoBehaviour
    {
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        public event Action OnConfirmed;
        public event Action OnCancelled;

        private void OnEnable()
        {
            _confirmButton.onClick.AddListener(HandleConfirm);
            _cancelButton.onClick.AddListener(HandleCancel);

            PopupTween.PlayShow(gameObject);
        }

        private void OnDisable()
        {
            _confirmButton.onClick.RemoveListener(HandleConfirm);
            _cancelButton.onClick.RemoveListener(HandleCancel);
        }

        public void ShowPopup()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            PopupTween.PlayHide(gameObject, () => gameObject.SetActive(false));
        }

        private void HandleConfirm()
        {
            OnConfirmed?.Invoke();
            Hide();
        }

        private void HandleCancel()
        {
            OnCancelled?.Invoke();
            Hide();
        }
    }
}
