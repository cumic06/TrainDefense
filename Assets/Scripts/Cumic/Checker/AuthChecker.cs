namespace Cumic
{
    public class AuthChecker : IAuthable
    {
        public bool TryLogin()
        {
            return true;
        }

        public bool TryLogout()
        {
            throw new System.NotImplementedException();
        }

        public bool IsLoggedIn()
        {
            throw new System.NotImplementedException();
        }
    }
}