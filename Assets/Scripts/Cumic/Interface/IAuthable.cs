namespace Cumic
{
    public interface IAuthable
    {
        bool TryLogin();//로그인
        bool TryLogout();//로그아웃
        bool IsLoggedIn();//로그인 상태
    }
}