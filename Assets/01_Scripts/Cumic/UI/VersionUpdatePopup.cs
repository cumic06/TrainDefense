using UnityEngine;
using UnityEngine.UI;
using TrainDefense.Game.UI;

namespace Cumic.UI
{
    // 버전 불일치 시 뜨는 강제 업데이트 팝업. 업데이트 버튼을 누르면 스토어로 이동한다.
    // 메시지/버튼 텍스트는 프리팹의 LocalizeText가 담당한다.
    public class VersionUpdatePopup : MonoBehaviour
    {
        [SerializeField] private Button _confirmButton;

        private void OnEnable()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(OpenStore);

            PopupTween.PlayShow(gameObject);
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
