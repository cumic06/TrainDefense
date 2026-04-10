using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cumic.Events;
using TrainDefense.Game.Events;

namespace TrainDefense.Game.UI
{
    public class ShopUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private float shopMoveXStartPos = -100;
        [SerializeField]
        private float shopMoveXEndPos;
        [SerializeField]
        private float shopMoveInterval = 1f;
        [SerializeField]
        private Image backgroundImage;
        [SerializeField]
        private ShopButtonUI shopButtonUI;
        #endregion

        private RectTransform _rectTransform;

        private List<ShopItemUI> _shopItemUIs = new();

        private bool isShopOpen = false;
        public bool IsShopOpen => isShopOpen;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _shopItemUIs = GetComponentsInChildren<ShopItemUI>().ToList();
        }

        private void Start()
        {
            // 업그레이드가 실제로 적용된 이후에만 상점 UI를 갱신하기 위해 UpgradeAppliedEvent를 구독
            shopButtonUI.OnClickShopButton += _ShopOpenHandler;
            GameEventSystem.Subscribe<UpgradeAppliedEvent>(OnUpgradeApplied);
            GameEventSystem.Subscribe<InspectionStartEvent>(_OnInspectionStart);
        }

        private void OnDestroy()
        {
            shopButtonUI.OnClickShopButton -= _ShopOpenHandler;
            GameEventSystem.Unsubscribe<UpgradeAppliedEvent>(OnUpgradeApplied);
            GameEventSystem.Unsubscribe<InspectionStartEvent>(_OnInspectionStart);
        }

        private void OnUpgradeApplied(UpgradeAppliedEvent upgradeAppliedEvent)
        {
            foreach (var shopItemUI in _shopItemUIs)
            {
                shopItemUI.SetUp();
                shopItemUI.SetVaild(GetCurrentMoney());
            }
        }

        private void _OnInspectionStart(InspectionStartEvent inspectionStartEvent)
        {
            if (!isShopOpen)
            {
                _OpenShop();
                shopButtonUI.gameObject.SetActive(true);
            }
        }

        private void _ShopOpenHandler()
        {
            if (isShopOpen)
            {
                _CloseShop();
                shopButtonUI.gameObject.SetActive(false);
            }
            else
            {
                _OpenShop();
            }
        }

        private async void _OpenShop()
        {
            if (isShopOpen)
            {
                return;
            }

            gameObject.SetActive(true);

            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            foreach (var shopItemUI in _shopItemUIs)
            {
                shopItemUI.SetUp();
                shopItemUI.SetVaild(GetCurrentMoney());
            }

            await _rectTransform.DOAnchorPosX(shopMoveXEndPos, shopMoveInterval).SetUpdate(true);

            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(true);
            }

            isShopOpen = true;
        }

        private async void _CloseShop()
        {
            if (!isShopOpen)
            {
                return;
            }

            await _rectTransform.DOAnchorPosX(shopMoveXStartPos, shopMoveInterval).SetUpdate(true);

            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(false);
            }

            isShopOpen = false;

            GameEventSystem.Publish(new InspectionEndEvent());
            GameEventSystem.Publish(new EngageStartEvent());
        }

        private int GetCurrentMoney()
        {
            if (UserDataManager.Instance == null) return 0;

            return UserDataManager.Instance.Coin;
        }
    }
}