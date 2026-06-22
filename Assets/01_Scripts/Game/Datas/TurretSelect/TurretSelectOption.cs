using System;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 포탑 선택창에 노출할 한 항목. 표시용 아이콘/이름은 선택창 전용으로 따로 두고,
    /// 실제 장착·스탯은 turretDataId로 연결한 TurretTrainData를 따른다.
    /// </summary>
    [Serializable]
    public class TurretSelectOption
    {
        #region Fields
        [SerializeField]
        [Tooltip("선택 시 MainTrain에 장착·스탯 출처가 되는 TurretTrainData의 Id")]
        private string turretDataId;
        [SerializeField]
        [Tooltip("선택창 표시용 아이콘(기존 터렛과 다른 이미지). 비우면 TurretTrainData.Icon 사용")]
        private Sprite icon;
        [SerializeField]
        [Tooltip("선택창 표시용 이름. 비우면 TurretTrainData.Name 사용")]
        private string displayName;
        #endregion

        public string TurretDataId => turretDataId;
        public Sprite Icon => icon;
        public string DisplayName => displayName;
    }
}
