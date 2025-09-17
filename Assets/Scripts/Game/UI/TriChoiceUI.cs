using Sirenix.OdinInspector;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TrainDefense.Game.UI
{
    public class TriChoiceUI : MonoBehaviour
    {
        #region Fields
        [SerializeField]
        private TriChoiceSelectUI[] choiceSelectUIs;

        [SerializeField]
        [FolderPath]
        private string triChoiceDataFolderPath = "Datas/TriChoiceDatas";
        #endregion

        private void Start()
        {
            GameEventSystem.Subscribe<LevelUpEvent>(OnLevelUp);
            GameEventSystem.Subscribe<TriChoiceSelectEvent>(OnChoiceSelected);

            if (choiceSelectUIs.Length == 0)
            {
                choiceSelectUIs = GetComponentsInChildren<TriChoiceSelectUI>(true);
            }
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<LevelUpEvent>(OnLevelUp);
            GameEventSystem.Unsubscribe<TriChoiceSelectEvent>(OnChoiceSelected);
        }

        private void OnLevelUp(LevelUpEvent eventData)
        {
            TimeManager.Instance.Pause();

            foreach (var choiceSelectUI in choiceSelectUIs)
            {
                var triChoiceData = RandomChoice();
                if (triChoiceData == null) continue;
                choiceSelectUI.SetData(triChoiceData);
                choiceSelectUI.gameObject.SetActive(true);
            }
        }

        private void OnChoiceSelected(TriChoiceSelectEvent eventData)
        {
            TimeManager.Instance.Resume();

            foreach (var choiceSelectUI in choiceSelectUIs)
            {
                choiceSelectUI.gameObject.SetActive(false);
            }
        }

        private TriChoiceData RandomChoice()
        {
            var triChoiceDatas = Resources.LoadAll<TriChoiceData>(triChoiceDataFolderPath);
            return triChoiceDatas[Random.Range(0, triChoiceDatas.Length)];
        }
    }
}