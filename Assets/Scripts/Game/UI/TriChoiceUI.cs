using TrainDefense.Game.Events;
using UnityEngine;

namespace TrainDefense.Game.UI
{
    public class TriChoiceUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private TriChoiceSelectUI[] choiceSelectUIs;
        #endregion

        private void Start()
        {
            GameEventSystem.Subscribe<LevelUpEvent>(OnLevelUp);

            if (choiceSelectUIs.Length == 0)
            {
                choiceSelectUIs = GetComponentsInChildren<TriChoiceSelectUI>(true);
            }
        }

        private void OnLevelUp(LevelUpEvent eventData)
        {
            foreach (var choiceSelectUI in choiceSelectUIs)
            {
                choiceSelectUI.gameObject.SetActive(true);
                choiceSelectUI.SetData();
            }
        }
    }
}