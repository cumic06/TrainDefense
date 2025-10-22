using UnityEngine;

[System.Serializable]
public class StageData
{
    #region Fields
    [SerializeField]
    private string id;
    [SerializeField]
    private float[] stageInspectionTime;
    [SerializeField]
    private float stageEndTime;
    #endregion

    public string Id => id;
    public float[] StageInspectionTime => stageInspectionTime;
    public float StageEndTime => stageEndTime;
}