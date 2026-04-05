using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrainDefense.Game.Tutorial
{
    /// <summary>
    /// 튜토리얼 스킵 확인 팝업
    /// </summary>
    public class TutorialSkipPopup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private string _defaultMessage = "튜토리얼을 건너뛰시겠습니까?";

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

        public void ShowPopup(string message = null)
        {
            _messageText.text = string.IsNullOrEmpty(message) ? _defaultMessage : message;
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
