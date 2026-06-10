using System.Collections;
using UnityEngine;

namespace TrainDefense.Game
{
    /// <summary>
    /// 포격 액티브 스킬. BuffDuration 초 동안 0.3s 간격으로 화면 내 랜덤 위치에 포탄 스폰.
    /// </summary>
    public class TrainBombardSkillAction : TrainSkillAction
    {
        private const float ShotInterval = 0.3f;
        private const float HalfX = 10f;
        private const float HalfY = 7f;
        private const int SoundEveryNShots = 2;

        protected override bool OnUse()
        {
            if (owner is not TurretTrain turret) return false;
            float duration = trainSkillData.BuffDuration > 0f ? trainSkillData.BuffDuration : 5f;
            owner.StartCoroutine(BombardCoroutine(turret, duration));
            return true;
        }

        private static IEnumerator BombardCoroutine(TurretTrain turret, float duration)
        {
            float elapsed = 0f;
            int shotIndex = 0;
            while (elapsed < duration)
            {
                yield return new WaitForSeconds(ShotInterval);
                elapsed += ShotInterval;
                if (turret == null || turret.IsDead) yield break;

                Vector3 center = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
                var pos = new Vector3(
                    center.x + Random.Range(-HalfX, HalfX),
                    center.y + Random.Range(-HalfY, HalfY),
                    0f);
                // 발사음은 N발마다 한 번만 (16발 사운드 겹침 방지, 폭격 리듬감).
                bool playSound = shotIndex % SoundEveryNShots == 0;
                turret.SpawnProjectileAtWorldPositionPublic(null, pos, playSound);
                shotIndex++;
            }
        }
    }
}
