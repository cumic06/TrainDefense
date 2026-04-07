using Cumic.Events;
using TrainDefense.Game.Events;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    public class ShopButtonUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private Button shopButton;
        [SerializeField]
        private ShopUI shopUI;
        #endregion

        private void Start()
        {
            shopButton.onClick.AddListener(OnShopButtonClick);
            GameEventSystem.Subscribe<InspectionStartEvent>(OnInspectionStart);
            GameEventSystem.Subscribe<EngageStartEvent>(OnEngageStart);
            GameEventSystem.Subscribe<StageEndEvent>(OnStageEnd);
            GameEventSystem.Subscribe<GameEndEvent>(OnGameEnd);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<InspectionStartEvent>(OnInspectionStart);
            GameEventSystem.Unsubscribe<EngageStartEvent>(OnEngageStart);
            GameEventSystem.Unsubscribe<StageEndEvent>(OnStageEnd);
            GameEventSystem.Unsubscribe<GameEndEvent>(OnGameEnd);
        }

        private void OnInspectionStart(InspectionStartEvent inspectionStartEvent)
        {
            gameObject.SetActive(true);

            if (shopUI == null)
            {
                Debug.LogWarning("ShopButtonUI: shopUI reference is null.");
                return;
            }

            shopUI.gameObject.SetActive(true);
            OnShopOpen();
        }

        private void OnEngageStart(EngageStartEvent engageStartEvent)
        {
            gameObject.SetActive(false);
        }

        private void OnStageEnd(StageEndEvent stageEndEvent)
        {
            gameObject.SetActive(false);
        }

        private void OnGameEnd(GameEndEvent gameEndEvent)
        {
            gameObject.SetActive(false);
        }

        private void OnShopButtonClick()
        {
            if (shopUI == null)
            {
                Debug.LogWarning("ShopButtonUI: shopUI reference is null.");
                return;
            }

            if (shopUI.IsShopOpen)
            {
                shopUI.CloseShop();
                return;
            }
            shopUI.OpenShop();
        }

        public void OnShopOpen()
        {
            if (shopUI == null)
            {
                Debug.LogWarning("ShopButtonUI: shopUI reference is null.");
                return;
            }

            shopUI.OpenShop();
        }
    }
}
