using Cumic.Events;
using TrainDefense;
using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game.UI
{
    public class LuckyUISpawner : MonoBehaviour
    {
        [SerializeField] private LuckyUI luckyUI;
        [SerializeField] private RectTransform canvasRect;

        private Camera _mainCamera;
        private Camera _uiCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _uiCamera = canvasRect.GetComponentInParent<Canvas>().worldCamera;
            if (_uiCamera == null) _uiCamera = _mainCamera;
        }

        private void Start()
        {
            GameEventSystem.Subscribe<LuckyEvent>(OnLucky);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<LuckyEvent>(OnLucky);
        }

        private void OnLucky(LuckyEvent e)
        {
            LuckyUI spawned = ResourceManager.Instance.Spawn(luckyUI, parent: transform);

            Vector2 screenPoint = _mainCamera.WorldToScreenPoint(e.Position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, _uiCamera, out Vector2 localPoint);

            spawned.SetLocalPosition(localPoint);
        }
    }
}
