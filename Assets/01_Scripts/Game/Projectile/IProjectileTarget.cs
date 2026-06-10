using UnityEngine;

namespace TrainDefense.Game
{
    public interface IProjectileTarget : IDamageable
    {
        Transform TargetTransform { get; }
        bool IsActive { get; }

        void Slow(float slowValue, float duration);
        void ResetMoveSpeed();
        void Shove(float shovePower, float shoveDuration);
        void Stun(float stunDuration);
    }
}