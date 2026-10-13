using Cumic;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 공격할 때 빔을 켜 둔 채 포탑이 조준한 적에서 시작해 적이 많은 쪽으로 부채꼴로 돌아, 빔이 앞쪽 적들을 휩쓴다. 빔에 들어온 적은 한 번씩 맞는다.
    /// 쓸기가 끝나면 그 자리에서 잠깐 멈췄다가 쓸기 속도로 천천히 돌아가 다음 적을 조준한다. (엘리트 레이저 섬멸: 빔 0.8초)
    /// DSL: "SweepAttack:seconds"
    /// </summary>
    public class SweepAttackPassive : TrainPassiveSkill
    {
        // 빔이 한 번에 쓸고 지나가는 부채꼴 각도(도).
        private const float SWEEP_ANGLE = 120f;
        // 쓸기를 마친 뒤 그 자리에서 멈춰 있는 시간(초). 바로 돌아서면 공격이 끝난 건지 알아보기 어렵다.
        private const float PAUSE_SECONDS = 0.1f;
        // 다음 적을 다 조준했다고 보는 각도 차(도). 여기서부터 포탑 평소 조준 회전에 넘긴다.
        private const float RETURN_DONE_ANGLE = 1f;

        private enum SweepPhase { None, Sweeping, Pausing, Returning }

        public float Duration { get; private set; }

        private SweepPhase _phase;
        private float _elapsed;
        private float _startAngle;
        // +1 = 반시계, -1 = 시계.
        private float _direction;

        public static SweepAttackPassive From(string[] parts)
        {
            if (parts.Length < 2) return null;
            if (!TryParseFloat(parts[1], out float duration) || duration <= 0f) return null;
            return new SweepAttackPassive { Duration = duration };
        }

        public override void Subscribe()
        {
            if (Owner is not TurretTrain t) return;
            t.BeamLifetimeOverride = Duration;
            t.OnAttacked += _BeginSweep;
            t.RotationOverride = _RotateTurret;
        }

        public override void Unsubscribe()
        {
            if (Owner is not TurretTrain t) return;
            t.BeamLifetimeOverride = 0f;
            t.OnAttacked -= _BeginSweep;
            t.RotationOverride = null;
            _phase = SweepPhase.None;
        }

        // 쏘는 순간 지금 조준한 방향(= 적 방향)에서 점프 없이 시작하고, 조준한 적을 뺀 사거리 안 적이 더 많은 쪽으로 돈다. (같으면 시계 방향)
        private void _BeginSweep(Monster target)
        {
            if (Owner is not TurretTrain t || t.TurretVisualRoot == null) return;

            Transform turret = t.TurretVisualRoot.transform;
            _startAngle = turret.eulerAngles.z;
            int counterClockwiseLead = 0;
            foreach (Monster monster in t.TargetMonsters)
            {
                if (monster == target)
                    continue;
                counterClockwiseLead += Mathf.DeltaAngle(_startAngle, turret.GetAngle2D(monster.transform.position)) > 0f ? 1 : -1;
            }
            _direction = counterClockwiseLead > 0 ? 1f : -1f;
            _EnterPhase(SweepPhase.Sweeping);
        }

        // 포탑 FixedUpdate에서 불린다. 회전을 맡은 틱이면 true.
        private bool _RotateTurret()
        {
            if (_phase == SweepPhase.None || Owner is not TurretTrain t || t.TurretVisualRoot == null)
                return false;

            Transform turret = t.TurretVisualRoot.transform;
            _elapsed += Time.fixedDeltaTime;

            switch (_phase)
            {
                case SweepPhase.Sweeping:
                    // 양 끝을 부드럽게(SmoothStep) 해서 뚝 시작하고 뚝 멈추지 않게 한다.
                    float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_elapsed / Duration));
                    turret.rotation = Quaternion.Euler(0f, 0f, _startAngle + _direction * SWEEP_ANGLE * progress);
                    if (_elapsed >= Duration)
                        _EnterPhase(SweepPhase.Pausing);
                    return true;

                case SweepPhase.Pausing:
                    if (_elapsed >= PAUSE_SECONDS)
                        _EnterPhase(SweepPhase.Returning);
                    return true;

                default:
                    // 쓸기 속도로 천천히 다음 적을 조준한다. (쓸자마자 휙 돌아 조준하면 어색)
                    Monster nearTarget = t.GetNearTargetMonsterPublic();
                    if (nearTarget == null)
                    {
                        _phase = SweepPhase.None;
                        return false;
                    }
                    Quaternion aimRotation = Quaternion.Euler(0f, 0f, turret.GetAngle2D(nearTarget.transform.position));
                    turret.rotation = Quaternion.RotateTowards(turret.rotation, aimRotation, SWEEP_ANGLE / Duration * Time.fixedDeltaTime);
                    if (Quaternion.Angle(turret.rotation, aimRotation) <= RETURN_DONE_ANGLE)
                        _phase = SweepPhase.None;
                    return true;
            }
        }

        private void _EnterPhase(SweepPhase phase)
        {
            _phase = phase;
            _elapsed = 0f;
        }
    }
}
