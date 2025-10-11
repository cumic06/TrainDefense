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
    [SerializeField]
    private TextMeshProUGUI needMoneyText;
    #endregion

    private void Start()
    {
        buyButton.onClick.AddListener(OnBuyButtonClick);

        if (shopItemData != null)
        {
            itemNameText.text = shopItemData.ItemName;
            needMoneyText.text = $"{shopItemData.NeedMoney}$";
        }
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