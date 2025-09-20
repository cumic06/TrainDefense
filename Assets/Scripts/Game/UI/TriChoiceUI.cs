using Cumic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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
        private float uiActiveDelay;

        [SerializeField]
        [FolderPath]
        private string triChoiceDataFolderPath = "Datas/TriChoiceDatas";
        #endregion

        private void Start()
        {
            GameEventSystem.Subscribe<TriChoiceSelectEvent>(OnChoiceSelected);

            if (choiceSelectUIs.Length == 0)
            {
                choiceSelectUIs = GetComponentsInChildren<TriChoiceSelectUI>(true);
            }
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<TriChoiceSelectEvent>(OnChoiceSelected);
        }

        private async UniTask OnChoiceUIPopup()
        {
            foreach (var choiceSelectUI in choiceSelectUIs)
            {
                var triChoiceData = RandomChoice();
                if (triChoiceData == null) continue;

                choiceSelectUI.SetData(triChoiceData);

                choiceSelectUI.transform.localScale = Vector3.zero;

                await choiceSelectUI.transform.DOScale(1, uiActiveDelay).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        private void OnChoiceSelected(TriChoiceSelectEvent eventData)
        {
            TimeManager.Instance.Resume();

            foreach (var choiceSelectUI in choiceSelectUIs)
            {
                choiceSelectUI.transform.DOScale(0, uiActiveDelay).SetEase(Ease.InBack).SetUpdate(true);
            }
        }

        private TriChoiceData RandomChoice()
        {
            var triChoiceDatas = Resources.LoadAll<TriChoiceData>(triChoiceDataFolderPath);
            return triChoiceDatas[Random.Range(0, triChoiceDatas.Length)];
        }
    }
}