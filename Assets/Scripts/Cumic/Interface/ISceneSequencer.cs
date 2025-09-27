using System;
using Cysharp.Threading.Tasks;

namespace Cumic.Sequence
{
    public interface ISceneSequencer : IDisposable //인터페이스 쓰는 이유 : 간단하게 만들 게임은 특정 씬 시퀀서가 필요 없어서
    {
        event Action<bool> OnCompleted;
        UniTask Run();
        void Dispose();
    }
}