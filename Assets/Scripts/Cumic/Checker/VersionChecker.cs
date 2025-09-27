using Cysharp.Threading.Tasks;

namespace Cumic.Checker
{
    public class VersionChecker : IVersionable
    {
        public UniTask<bool> CheckVersion()
        {
            return UniTask.FromResult(true);
        }
    }
}