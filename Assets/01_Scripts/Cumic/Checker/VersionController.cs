using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Cumic.Checker
{
    public class VersionController : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private GameObject updatePopup;

        [SerializeField]
        private Button updateButton;
        #endregion

        // 최신 버전 문자열이 저장된 URL (redeyeshq.github.io)
        private readonly string _versionCheckUrl = "https://redeyeshq.github.io/trainDefenseVersion.txt";
        private readonly string _testVersionCheckUrl = "https://redeyeshq.github.io/trainDefenseTestVersion.txt";

        private void Start()
        {
            updateButton?.onClick.AddListener(OnClickUpdateButton);
            CheckVersion().Forget();
        }

        private async UniTask CheckVersion()
        {
            // 개발(Development) 빌드 또는 에디터에서는 테스트 버전 파일을 확인한다.
            bool isTestChannel = Debug.isDebugBuild;
            string checkUrl = isTestChannel ? _testVersionCheckUrl : _versionCheckUrl;

            // 정식 릴리즈 빌드(isDebugBuild=false)에서는 로그를 남기지 않는다.
            if (Debug.isDebugBuild)
                Debug.Log($"[Version] {(isTestChannel ? "테스트" : "정식")} 채널 / 확인 URL: {checkUrl}");

            UnityWebRequest www = UnityWebRequest.Get(checkUrl);

            await www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string latestVersion = www.downloadHandler.text.Trim();
                string currentVersion = Application.version;

                if (Debug.isDebugBuild)
                    Debug.Log($"[Version] 최신:{latestVersion} / 현재:{currentVersion}");

                if (latestVersion != currentVersion && updatePopup != null)
                {
                    updatePopup.SetActive(true);
                }
            }
            else
            {
                if (Debug.isDebugBuild)
                    Debug.LogWarning("[Version] 버전 확인 실패: " + www.error);
            }
        }

        private void OnClickUpdateButton()
        {
            Application.OpenURL("https://play.google.com/store/apps/details?id=" + Application.identifier);
        }
    }
}
