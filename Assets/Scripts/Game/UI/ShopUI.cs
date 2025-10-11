using System.Collections.Generic;
using System.Linq;
using Cumic.Events;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

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

        public async void OpenShop()
        {
            if (UserDataManager.Instance == null) return;

            int money = UserDataManager.Instance.Coin;

            foreach (var shopItemUI in _shopItemUIs)
            {
                shopItemUI.SetVaild(money);
            }

            await _rectTransform.DOAnchorPosX(shopMoveXEndPos, shopMoveInterval).SetUpdate(true);
            backgroundImage.gameObject.SetActive(true);
            isShopOpen = true;
        }

        public async void CloseShop()
        {
            await _rectTransform.DOAnchorPosX(shopMoveXStartPos, shopMoveInterval).SetUpdate(true);
            isShopOpen = false;
            backgroundImage.gameObject.SetActive(false);
            GameEventSystem.Publish(new EngageStartEvent());
        }
    }
}