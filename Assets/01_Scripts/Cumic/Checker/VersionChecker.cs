using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Cumic.Checker
{
    public class VersionChecker : IVersionable
    {
        // 최신 버전 문자열이 저장된 URL (redeyeshq.github.io)
        private const string VersionUrl = "https://redeyeshq.github.io/trainDefenseVersion.txt";
        private const string TestVersionUrl = "https://redeyeshq.github.io/trainDefenseTestVersion.txt";

        public async UniTask<bool> CheckVersion()
        {
            // 개발(Development) 빌드 또는 에디터에서는 테스트 버전 파일을 확인한다.
            bool isTestChannel = Debug.isDebugBuild;
            string checkUrl = isTestChannel ? TestVersionUrl : VersionUrl;

            if (Debug.isDebugBuild)
                Debug.Log($"[Version] {(isTestChannel ? "테스트" : "정식")} 채널 / 확인 URL: {checkUrl}");

            using UnityWebRequest www = UnityWebRequest.Get(checkUrl);
            await www.SendWebRequest();

            // 서버 응답 실패(네트워크 등) → 게임 진입 차단(false)
            if (www.result != UnityWebRequest.Result.Success)
            {
                if (Debug.isDebugBuild)
                    Debug.LogWarning("[Version] 버전 확인 실패: " + www.error);
                return false;
            }

            string latestVersion = www.downloadHandler.text.Trim();
            string currentVersion = Application.version;

            if (Debug.isDebugBuild)
                Debug.Log($"[Version] 최신:{latestVersion} / 현재:{currentVersion}");

            // 버전 일치 → 통과(true), 불일치 → 차단(false)
            return latestVersion == currentVersion;
        }
    }
}
