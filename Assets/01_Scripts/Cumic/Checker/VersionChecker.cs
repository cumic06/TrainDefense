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
        private const int REQUEST_TIMEOUT_SECONDS = 5;

        public async UniTask<bool> CheckVersion()
        {
            // 개발(Development) 빌드 또는 에디터에서는 테스트 버전 파일을 확인한다.
            bool isTestChannel = Debug.isDebugBuild;
            string checkUrl = isTestChannel ? TestVersionUrl : VersionUrl;

            if (Debug.isDebugBuild)
                Debug.Log($"[Version] {(isTestChannel ? "테스트" : "정식")} 채널 / 확인 URL: {checkUrl}");

            using UnityWebRequest www = UnityWebRequest.Get(checkUrl);
            // 응답이 안 오는 망에서 무한 대기하지 않도록 상한을 둔다. 시간 초과는 아래 실패 분기와 같이 통과 처리된다.
            www.timeout = REQUEST_TIMEOUT_SECONDS;

            // 서버 응답 실패(오프라인·시간 초과 등) → 혼자 하는 게임이라 막지 않고 그냥 진입(true). 버전 불일치만 차단한다.
            // UniTask의 await는 결과가 Success가 아니면 UnityWebRequestException을 던지므로 result 검사가 아니라 catch로 받는다.
            try
            {
                await www.SendWebRequest();
            }
            catch (UnityWebRequestException exception)
            {
                if (Debug.isDebugBuild)
                    Debug.LogWarning("[Version] 버전 확인 실패(통과 처리): " + exception.Message);
                return true;
            }

            string latestVersion = www.downloadHandler.text.Trim();
            string currentVersion = Application.version;

            if (Debug.isDebugBuild)
                Debug.Log($"[Version] 최신:{latestVersion} / 현재:{currentVersion}");

            // 최신보다 낮으면 차단. 같거나 높으면 통과 — 단계 출시 중 새 빌드가 서버 파일보다 앞서도 막히지 않게.
            // 숫자 형식이 아닌 버전 문자열은 예전처럼 문자열 일치로만 판정한다.
            if (_TryCompareVersions(currentVersion, latestVersion, out int comparison))
                return comparison >= 0;

            return latestVersion == currentVersion;
        }

        // "1.3"과 "1.3.0"처럼 자릿수가 다른 버전을 같은 것으로 보도록, 빠진 자리를 0으로 채워 자리별로 비교한다.
        // (System.Version은 빠진 자리를 -1로 두어 "1.3" < "1.3.0"이 된다)
        // "1.3.2-hotfix"처럼 자리 뒤에 붙은 접미사는 무시하고 앞의 숫자만 본다. 숫자로 시작하지 않는 자리가 있으면 비교 포기.
        private static bool _TryCompareVersions(string left, string right, out int comparison)
        {
            comparison = 0;
            string[] leftParts = left.Split('.');
            string[] rightParts = right.Split('.');
            int length = System.Math.Max(leftParts.Length, rightParts.Length);

            for (int i = 0; i < length; i++)
            {
                int leftValue = 0;
                int rightValue = 0;

                if (i < leftParts.Length && !_TryParseLeadingNumber(leftParts[i], out leftValue))
                    return false;

                if (i < rightParts.Length && !_TryParseLeadingNumber(rightParts[i], out rightValue))
                    return false;

                if (leftValue == rightValue)
                    continue;

                comparison = leftValue < rightValue ? -1 : 1;
                return true;
            }

            return true;
        }

        private static bool _TryParseLeadingNumber(string segment, out int value)
        {
            int digitCount = 0;

            while (digitCount < segment.Length && char.IsDigit(segment[digitCount]))
                digitCount++;

            return int.TryParse(segment.Substring(0, digitCount), out value);
        }
    }
}
