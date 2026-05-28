using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    public class ConfirmPopup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private string _defaultMessage = "정말 진행하시겠습니까?";

        public event Action OnConfirmed;
        public event Action OnCancelled;

        private void OnEnable()
        {
            _confirmButton.onClick.AddListener(HandleConfirm);
            _cancelButton.onClick.AddListener(HandleCancel);
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
            gameObject.SetActive(false);
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
