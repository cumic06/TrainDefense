using System;
using Cysharp.Threading.Tasks;

namespace Cumic.Sequence
{
    public class LobbySceneSequencer : ISceneSequencer
    {
        private GameSceneSequence _gameSceneSequence;
        public event Action<bool> OnCompleted;

        public LobbySceneSequencer(GameSceneSequence gameSceneSequence)
        {
            _gameSceneSequence = gameSceneSequence;

            //대충 유저 정보 불러오는 코드
            //실패 시 PopupUI 표시
            //성공 시 인벤토리나 어디에 UI 반영하게 이벤트 전송.
        }

        public async UniTask Run()
        {
            OnCompleted?.Invoke(true);
        }

        public void Dispose()
        {

        }
    }
}