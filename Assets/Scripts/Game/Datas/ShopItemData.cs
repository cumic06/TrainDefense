using UnityEngine;

[CreateAssetMenu(fileName = "ShopItemData", menuName = "Data/ShopItemData")]
public class ShopItemData : ScriptableObject
{
    #region Fields
    [SerializeField]
    private string id;
    [SerializeField]
    private Sprite icon;
    [SerializeField]
    private string itemName;
    [SerializeField]
    private string description;
    [SerializeField]
    private int needMoney;
    #endregion

    public string Id => id;
    public Sprite Icon => icon;
    public int NeedMoney => needMoney;
    public string ItemName => itemName;
    public string Description => description;
}