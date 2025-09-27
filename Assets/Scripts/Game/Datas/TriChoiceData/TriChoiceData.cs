using UnityEngine;

namespace TrainDefense.Game.Datas
{
    public abstract class TriChoiceData : ScriptableObject
    {
        #region Fields
        [SerializeField]
        private string id;
        [SerializeField]
        private Sprite icon;
        [SerializeField]
        private string choiceName;
        [SerializeField]
        private string description;
        #endregion

        public string Id => id;
        public Sprite Icon => icon;
        public string ChoiceName => choiceName;
        public string Description => description;
    }
}