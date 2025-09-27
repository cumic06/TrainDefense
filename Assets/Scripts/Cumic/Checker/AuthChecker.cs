using Cysharp.Threading.Tasks;

namespace Cumic.Checker
{
    public class AuthChecker : IAuthable
    {
        public async UniTask<bool> TryLogin()
        {
            return true;
        }

        public async UniTask<bool> TryLogout()
        {
            throw new System.NotImplementedException();
        }

        public async UniTask<bool> IsLoggedIn()
        {
            throw new System.NotImplementedException();
        }
    }
}