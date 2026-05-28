namespace Cumic.Events
{
    public class GameEnterEvent
    {
        public bool IsLobby { get; }

        public GameEnterEvent(bool isLobby = false)
        {
            IsLobby = isLobby;
        }
    }
}