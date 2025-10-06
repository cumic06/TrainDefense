using Cumic.Events;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ShopButtonUI : MonoBehaviour
{
    #region Fields
    [SerializeField]
    private Button shopButton;
    [SerializeField]
    private Image shopImage;
    [SerializeField]
    private float shopMoveXStartPos = -100;
    [SerializeField]
    private float shopMoveXEndPos;
    [SerializeField]
    private float shopMoveInterval = 1f;
    #endregion

    private bool isShopOpen = false;

    private void Start()
    {
        shopButton.onClick.AddListener(OnShopButtonClick);
    }

    private async void OnShopButtonClick()
    {
        if (isShopOpen)
        {
            isShopOpen = false;
            await shopImage.rectTransform.DOAnchorPosX(shopMoveXStartPos, shopMoveInterval).SetUpdate(true);
            GameEventSystem.Publish(new EngageStartEvent());
            return;
        }

        await shopImage.rectTransform.DOAnchorPosX(shopMoveXEndPos, shopMoveInterval).SetUpdate(true);
        isShopOpen = true;
    }
}