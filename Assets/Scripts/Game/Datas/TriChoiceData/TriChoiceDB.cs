using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainDefense.Game.Datas
{
    [CreateAssetMenu(fileName = "TriChoiceDB", menuName = "Data/TriChoiceDB")]
    public class TriChoiceDB : ScriptableObject
    {
        #region Fields
        [Header("Train 추가 선택지")]
        [SerializeField]
        private List<ChoiceEntry> addTrainChoices = new();

        [Header("Train 업그레이드 선택지")]
        [SerializeField]
        private List<ChoiceEntry> upgradeTrainChoices = new();
        #endregion

        public IReadOnlyList<ChoiceEntry> AddTrainChoices => addTrainChoices;
        public IReadOnlyList<ChoiceEntry> UpgradeTrainChoices => upgradeTrainChoices;
    }

    [Serializable]
    public class ChoiceEntry
    {
        public ChoiceOption Option;
        public int Weight;
    }
}