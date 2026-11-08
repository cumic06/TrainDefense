using Cysharp.Threading.Tasks;

namespace Cumic.Checker
{
    public interface IAgreeable
    {
        // 필수 동의가 끝날 때까지 기다린다. 이미 동의했으면 바로 끝난다.
        UniTask WaitForAgreement();
    }
}
