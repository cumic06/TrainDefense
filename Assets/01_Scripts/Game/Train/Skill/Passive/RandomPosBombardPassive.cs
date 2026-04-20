using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// TargetPos 공격 시 화면 내 무작위 위치에 추가 폭격. (엘리트 포격)
    /// DSL: "RandomPosBombard:count:halfX:halfY[:centerX:centerY]"
    /// 기본 중심은 (0,0). bounds = [-halfX, halfX] × [-halfY, halfY] (+center)
    /// </summary>
    public class RandomPosBombardPassive : TrainPassiveSkill
    {
        public int Count { get; private set; } = 1;
        public Bounds Area { get; private set; }

        public static RandomPosBombardPassive From(string[] parts)
        {
            if (parts.Length < 4) return null;
            if (!int.TryParse(parts[1], out int count) || count <= 0) return null;
            if (!float.TryParse(parts[2], out float hx) || hx <= 0f) return null;
            if (!float.TryParse(parts[3], out float hy) || hy <= 0f) return null;

            float cx = 0f, cy = 0f;
            if (parts.Length >= 6)
            {
                float.TryParse(parts[4], out cx);
                float.TryParse(parts[5], out cy);
            }

            return new RandomPosBombardPassive
            {
                Count = count,
                Area = new Bounds(new Vector3(cx, cy, 0f), new Vector3(hx * 2f, hy * 2f, 0f))
            };
        }

        public override void Subscribe()
        {
            if (Owner is TurretTrain t) t.OnTargetPosAttacked += HandleTargetPosAttacked;
        }

        public override void Unsubscribe()
        {
            if (Owner is TurretTrain t) t.OnTargetPosAttacked -= HandleTargetPosAttacked;
        }

        private void HandleTargetPosAttacked()
        {
            if (Owner is not TurretTrain turret) return;
            var target = turret.GetNearTargetMonsterPublic();
            if (target == null) return;

            for (int i = 0; i < Count; i++)
            {
                var pos = new Vector3(
                    Random.Range(Area.min.x, Area.max.x),
                    Random.Range(Area.min.y, Area.max.y),
                    0f);
                turret.SpawnProjectileAtWorldPositionPublic(target, pos);
            }
        }
    }
}
