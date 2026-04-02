using System.Linq;
using Cumic;
using TrainDefense;
using TrainDefense.Game.Datas;
using UnityEngine;

namespace TrainDefense.Game
{
    public class TrainProjectileSkillAction : TrainSkillAction
    {
        protected override bool OnUse()
        {
            if (owner is not TurretTrain)
            {
                Debug.LogWarning($"TrainProjectileSkillAction: [{owner?.name}] supports only TurretTrain in phase 1.");
                return false;
            }

            var projectileSkillData = trainSkillData?.ProjectileData;
            if (projectileSkillData == null)
                return false;

            Projectile projectilePrefab = projectileSkillData.ProjectilePrefab;
            if (projectilePrefab == null)
                return false;

            int projectileCount = Mathf.Max(1, projectileSkillData.ProjectileCount);
            float attackRange = projectileSkillData.Range;
            if (attackRange <= 0f)
                return false;

            var targets = Physics2D.OverlapCircleAll(owner.transform.position, attackRange)
                .Where(collider => collider != null && collider.TryGetComponent(out Monster monster) && monster.IsActive && monster.gameObject.activeInHierarchy)
                .Select(collider => collider.GetComponent<Monster>())
                .OrderBy(monster => owner.transform.position.SqrDistance(monster.transform.position))
                .ToArray();

            if (targets.Length == 0)
                return false;

            for (int i = 0; i < projectileCount; i++)
            {
                Monster target = targets[Mathf.Min(i, targets.Length - 1)];
                if (target == null)
                    continue;

                Transform spawnPoint = owner.GetSkillSpawnPoint(i);
                Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : owner.transform.position;
                Projectile projectile = ResourceManager.Instance.Spawn(projectilePrefab, spawnPosition, Quaternion.identity);
                if (projectile == null)
                    continue;

                projectile.transform.LookAt2D(target.transform);
                projectile.Init(projectileSkillData.Damage, target, attackRange);
            }

            return true;
        }
    }
}
