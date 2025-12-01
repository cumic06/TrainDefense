using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [Serializable]
    public class WeightedUpgradeData
    {
        [SerializeField]
        [Tooltip("업그레이드 데이터 ID")]
        private string upgradeDataId;

        [SerializeField]
        [Tooltip("선택 가중치 (높을수록 선택될 확률 증가)")]
        private float upgradeDataWeight = 1f;

        public string UpgradeDataId => upgradeDataId;
        public float UpgradeDataWeight => upgradeDataWeight;
    }
}