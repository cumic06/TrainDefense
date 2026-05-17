using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    /// <summary>
    /// 삼중택일 선택지 데이터베이스 (데이터 저장만 담당)
    /// </summary>
    [Serializable]
    public class TriChoiceDB
    {
        #region Fields
        [Header("Train 추가/엘리트 선택지")]
        [SerializeField]
        private List<ChoiceEntry> addTrainChoices = new();

        [Header("Train 업그레이드 선택지")]
        [SerializeField]
        private List<ChoiceEntry> upgradeTrainChoices = new();
        #endregion

        public IReadOnlyList<ChoiceEntry> TrainChoiceEntries => addTrainChoices;
        public IReadOnlyList<ChoiceEntry> AddTrainChoices => addTrainChoices;
        public IReadOnlyList<ChoiceEntry> UpgradeTrainChoices => upgradeTrainChoices;
    }
}
