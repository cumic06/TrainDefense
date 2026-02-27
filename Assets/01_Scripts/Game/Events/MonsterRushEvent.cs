namespace TrainDefense.Game.Events
{
    public class MonsterRushEvent
    {
        private float _spawnTimeMultiplier;

        public float SpawnTimeMultiplier => _spawnTimeMultiplier;

        public MonsterRushEvent(float spawnTimeMultiplier)
        {
            _spawnTimeMultiplier = spawnTimeMultiplier;
        }
    }
}