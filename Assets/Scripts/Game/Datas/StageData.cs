using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "Data/StageData")]
public class StageData : ScriptableObject
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