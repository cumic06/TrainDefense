using Cysharp.Threading.Tasks;
using TrainDefense.Game.Manager;
using UnityEngine;

namespace Cumic.Checker
{
    /// <summary>
    /// OAuthScene에서 개인정보처리방침 필수 동의를 받을 때까지 진행을 멈춘다.
    /// 팝업 표시·저장은 OAuthScene에 배치된 AnalyticsManager가 맡고, 여기서는 그 완료만 기다린다.
    /// </summary>
    public class PrivacyAgreementChecker : IAgreeable
    {
        public UniTask WaitForAgreement()
        {
            if (AnalyticsManager.Instance == null)
            {
                Debug.LogError("[PrivacyAgreement] AnalyticsManager가 없어 필수 동의 없이 진행합니다.");

                return UniTask.CompletedTask;
            }

            var completionSource = new UniTaskCompletionSource();
            AnalyticsManager.Instance.RequestPrivacyAgreement(() => completionSource.TrySetResult());

            return completionSource.Task;
        }
    }
}
