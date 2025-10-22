using UnityEngine;

[System.Serializable]
public class UpgradeData
{
    #region Fields
    [SerializeField]
    private string id;
    [SerializeField]
    private Sprite icon;
    [SerializeField]
    private string upgradeName;
    [SerializeField]
    private string description;
    [SerializeField]
    private int needMoney;
    [SerializeField]
    private float upgradeValue;
    [SerializeField]
    private int maxUpgradeCount;
    #endregion

    public string Id => id;
    public Sprite Icon => icon;
    public int NeedMoney => needMoney;
    public string UpgradeName => upgradeName;
    public string Description => description;
    public float UpgradeValue => upgradeValue;
    public int MaxUpgradeCount => maxUpgradeCount;
}