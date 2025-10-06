using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    #region Fields
    [SerializeField]
    private ShopItemData shopItemData;
    [SerializeField]
    private Button buyButton;
    [SerializeField]
    private TextMeshProUGUI itemNameText;
    #endregion

    private void Start()
    {
        buyButton.onClick.AddListener(OnBuyButtonClick);
        itemNameText.text = shopItemData.ItemName;
    }

    public void SetVaild(int currentMoney)
    {
        if (currentMoney >= shopItemData.NeedMoney)
        {
            buyButton.interactable = true;
        }
        else
        {
            buyButton.interactable = false;
        }
    }

    private void OnBuyButtonClick()
    {
        // GameEventSystem.Publish(new BuyShopItemEvent(shopItemData.NeedMoney));
    }
}