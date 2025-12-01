using Cysharp.Threading.Tasks;

namespace Cumic.Checker
{
    public interface IVersionable
    {
        UniTask<bool> CheckVersion();
    }
}