using Cumic.Events;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game.UI
{
    public class DamageUISpawner : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private DamageUI damageUI;
        #endregion

        private void Start()
        {
            GameEventSystem.Subscribe<HitEvent>(OnHit);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<HitEvent>(OnHit);
        }

        private void OnHit(HitEvent hitEvent)
        {
            if (hitEvent.Damageable is Train train) return;

            DamageUI spawnDamageUI = ResourceManager.Instance.Spawn(damageUI, parent: transform);
            spawnDamageUI.transform.SetSiblingIndex(0);
            spawnDamageUI.SetPosition(Camera.main.WorldToScreenPoint(hitEvent.Position));
            spawnDamageUI.SetDamage(hitEvent.Damage);
        }
    }
}