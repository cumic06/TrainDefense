namespace TrainDefense.Game.Events
{
    public class WarningRemovedEvent
    {
        private string _id;

        public string Id => _id;

        public WarningRemovedEvent(string id)
        {
            _id = id;
        }
    }
}

