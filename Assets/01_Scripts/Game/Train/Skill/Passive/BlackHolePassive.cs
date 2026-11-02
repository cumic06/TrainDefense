using System;

namespace TrainDefense.Game
{
    /// <summary>
    /// 블랙홀 포탑 개조. 포탑의 BlackHoleModifier 값을 바꿔 착탄한 블랙홀 장판의 동작을 바꾼다.
    /// DSL: "BlackHole:Key:value[:Key:value...]"
    /// Key: Drift(흘러가는 속도) / Lifetime(유지 시간 초) / Collapse(사라질 때 폭발 피해 배율) / Pull(당기는 힘 배율) / Repulse(1이면 밀어내기)
    /// 예: "BlackHole:Drift:1.2:Lifetime:4" → 떠도는 블랙홀, "BlackHole:Repulse:1" → 화이트홀
    /// </summary>
    public class BlackHolePassive : TrainPassiveSkill
    {
        private float _driftSpeed;
        private float _lifetime;
        private float _collapseDamageMultiplier;
        private float _pullMultiplier = 1f;
        private bool _isRepulse;

        public static BlackHolePassive From(string[] parts)
        {
            if (parts.Length < 3 || (parts.Length - 1) % 2 != 0) return null;

            var passive = new BlackHolePassive();
            for (int i = 1; i + 1 < parts.Length; i += 2)
            {
                if (!TryParseFloat(parts[i + 1], out float value) || value < 0f) return null;

                switch (parts[i].Trim())
                {
                    case var key when key.Equals("Drift", StringComparison.OrdinalIgnoreCase):
                        passive._driftSpeed = value;
                        break;
                    case var key when key.Equals("Lifetime", StringComparison.OrdinalIgnoreCase):
                        passive._lifetime = value;
                        break;
                    case var key when key.Equals("Collapse", StringComparison.OrdinalIgnoreCase):
                        passive._collapseDamageMultiplier = value;
                        break;
                    case var key when key.Equals("Pull", StringComparison.OrdinalIgnoreCase):
                        passive._pullMultiplier = value;
                        break;
                    case var key when key.Equals("Repulse", StringComparison.OrdinalIgnoreCase):
                        passive._isRepulse = value > 0f;
                        break;
                    default:
                        return null;
                }
            }

            return passive;
        }

        public override void Subscribe()
        {
            if (Owner is not TurretTrain t) return;

            t.BlackHole.DriftSpeed = _driftSpeed;
            t.BlackHole.LifetimeOverride = _lifetime;
            t.BlackHole.CollapseDamageMultiplier = _collapseDamageMultiplier;
            t.BlackHole.PullMultiplier = _pullMultiplier;
            t.BlackHole.IsRepulse = _isRepulse;
        }

        public override void Unsubscribe()
        {
            if (Owner is TurretTrain t) t.BlackHole.Reset();
        }
    }
}
