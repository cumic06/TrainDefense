using System;
using Cumic;

namespace TrainDefense.Game
{
    /// <summary>
    /// 이 포탑의 피해가 들어간 적에게 추가 효과를 건다. 투사체·장판·즉시 피해 모두 적용된다.
    /// DSL: "HitEffect:Type:value:duration"
    /// Type:
    ///   Stun   — value% 확률로 duration초 기절                                  예: "HitEffect:Stun:25:0.8"
    ///   Slow   — duration초 동안 value% 둔화                                    예: "HitEffect:Slow:30:1"
    ///   Mark   — duration초 동안 모든 포탑에게 받는 피해 +value% (표적 지정)      예: "HitEffect:Mark:25:3"
    ///   Weaken — duration초 동안 그 적이 기차에 주는 피해 -value% (매연)          예: "HitEffect:Weaken:30:1"
    /// </summary>
    public class HitEffectPassive : TrainPassiveSkill
    {
        private enum HitEffectType
        {
            Stun,
            Slow,
            Mark,
            Weaken,
        }

        private HitEffectType _type;
        private float _value;
        private float _duration;

        public static HitEffectPassive From(string[] parts)
        {
            if (parts.Length < 4) return null;
            if (!Enum.TryParse(parts[1].Trim(), true, out HitEffectType type)) return null;
            if (!TryParseFloat(parts[2], out float value) || value <= 0f) return null;
            if (!TryParseFloat(parts[3], out float duration) || duration <= 0f) return null;
            if (type != HitEffectType.Mark && value > 100f) return null;

            return new HitEffectPassive { _type = type, _value = value, _duration = duration };
        }

        public override void Subscribe()
        {
            if (Owner != null) Owner.OnDamagedMonster += _OnDamagedMonster;
        }

        public override void Unsubscribe()
        {
            if (Owner != null) Owner.OnDamagedMonster -= _OnDamagedMonster;
        }

        private void _OnDamagedMonster(Monster target, float damage)
        {
            if (target == null) return;

            switch (_type)
            {
                case HitEffectType.Stun:
                    if (UtilMath.CheckProbability(_value))
                        TrainCombatRecorder.ApplyStun(target, _duration, Owner);
                    break;

                case HitEffectType.Slow:
                    // value는 %(30 = 30% 느려짐), 몬스터는 유지 속도 비율(0.7)을 쓴다.
                    TrainCombatRecorder.ApplySlow(target, 1f - _value / 100f, _duration, Owner);
                    break;

                case HitEffectType.Mark:
                    target.Mark(1f + _value / 100f, _duration);
                    break;

                case HitEffectType.Weaken:
                    target.Weaken(1f - _value / 100f, _duration);
                    break;
            }
        }
    }
}
