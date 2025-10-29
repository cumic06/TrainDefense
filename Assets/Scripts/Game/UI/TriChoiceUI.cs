using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;

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
        private string dbPath = "DB/DB";
        #endregion

        private int _choiceLeftCount;

        private void Awake()
        {
            if (choiceSelectUIs.Length == 0)
            {
                choiceSelectUIs = GetComponentsInChildren<TriChoiceSelectUI>(true);
            }
        }

        public void OnInspectionEnter(int count)
        {
            OnChoiceUIPopup(count).Forget();
        }

        private async UniTask OnChoiceUIPopup(int count)
        {
            _choiceLeftCount = count;
            var db = Resources.Load<DB>(dbPath);

            if (db == null)
            {
                Debug.LogError("DB not found");
                return;
            }

            foreach (var choiceSelectUI in choiceSelectUIs)
            {
                var choiceOption = db.GetRandomChoice();

                if (choiceOption == null) continue;

                choiceSelectUI.SetData(choiceOption, this);

                choiceSelectUI.transform.localScale = Vector3.zero;

                await choiceSelectUI.transform.DOScale(1, uiActiveDelay).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        public void OnChoiceSelected(IChoiceOption choiceOption)
        {
            _choiceLeftCount--;

            foreach (var choiceSelectUI in choiceSelectUIs)
            {
                choiceSelectUI.transform.localScale = Vector3.one;
                choiceSelectUI.transform.DOScale(0, uiActiveDelay).SetEase(Ease.InBack).SetUpdate(true);
            }

            if (_choiceLeftCount > 0)
            {
                OnInspectionEnter(_choiceLeftCount);
                return;
            }

            TriChoiceSelectEvent eventData = new(choiceOption, _choiceLeftCount);
            GameEventSystem.Publish(eventData);
        }
    }
}
