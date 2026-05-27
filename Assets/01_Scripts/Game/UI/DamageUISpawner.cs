using Cumic.Events;
using TrainDefense;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game.UI
{
    public class DamageUISpawner : MonoBehaviour
    {
        #region Field
        [SerializeField]
        private DamageUI damageUI;

        [SerializeField]
        private RectTransform canvasRect;

        [SerializeField]
        private float spawnRandomRadius = 5f;

        private Camera _mainCamera;
        private Camera _uiCamera;
        #endregion

        private void Awake()
        {
            _mainCamera = Camera.main;
            _uiCamera = canvasRect.GetComponentInParent<Canvas>().worldCamera;
            if (_uiCamera == null) _uiCamera = _mainCamera;
        }

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

            Vector2 screenPoint = _mainCamera.WorldToScreenPoint(hitEvent.Position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, _uiCamera, out Vector2 localPoint);

            Vector2 randomOffset = Random.insideUnitCircle * spawnRandomRadius;
            localPoint += randomOffset;
            spawnDamageUI.SetLocalPosition(localPoint);
            spawnDamageUI.SetDamage(hitEvent.Damage, hitEvent.IsCritical);
        }
    }
}