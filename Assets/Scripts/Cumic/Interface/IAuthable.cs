using Cysharp.Threading.Tasks;

namespace Cumic.Checker
{
    public interface IAuthable
    {
        UniTask<bool> TryLogin();//로그인
        UniTask<bool> TryLogout();//로그아웃
        UniTask<bool> IsLoggedIn();//로그인 상태
    }
}