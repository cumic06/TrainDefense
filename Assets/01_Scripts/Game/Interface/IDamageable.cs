namespace TrainDefense.Game
{
    public interface IDamageable
    {
        void TakeDamage(int damage);
        void TakeDamage(int damage, bool isCritical);
    }
}