namespace TrainDefense.Game
{
    public interface IDamageable
    {
        void TakeDamage(float damage);
        void TakeDamage(float damage, bool isCritical);
    }
}