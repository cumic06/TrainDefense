using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cumic.UI
{
    // 버전 불일치 시 뜨는 강제 업데이트 팝업. 업데이트 버튼을 누르면 스토어로 이동한다.
    public class VersionUpdatePopup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField, TextArea] private string _message = "새 버전이 출시되었어요.\n업데이트 후 이용해 주세요.";

        private void OnEnable()
        {
            if (_messageText != null)
                _messageText.text = _message;

            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(OpenStore);
        }

        private void OnDisable()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.RemoveListener(OpenStore);
        }

        private void OpenStore()
        {
            Application.OpenURL("https://play.google.com/store/apps/details?id=" + Application.identifier);
        }
    }
}
