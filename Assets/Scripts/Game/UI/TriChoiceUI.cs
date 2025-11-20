using System.Collections.Generic;
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
        private GameObject backgroundImage;

        [SerializeField]
        private float uiActiveDelay;
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
            backgroundImage.SetActive(true);

            OnChoiceUIPopup(count).Forget();
        }

        private async UniTask OnChoiceUIPopup(int count)
        {
            _choiceLeftCount = count;

            var triChoiceManager = TriChoiceManager.Instance;
            if (triChoiceManager == null)
            {
                Debug.LogError("TriChoiceManager not found");
                return;
            }

            // 선택지 풀을 미리 생성 (중복 없이)
            List<IChoiceOption> availableChoices = triChoiceManager.GetAvailableChoices(choiceSelectUIs.Length);

            if (availableChoices.Count == 0)
            {
                Debug.LogWarning("No available choices found");
                return;
            }

            // 사용 가능한 선택지 수만큼만 UI 표시
            for (int i = 0; i < choiceSelectUIs.Length; i++)
            {
                var choiceSelectUI = choiceSelectUIs[i];

                if (i < availableChoices.Count)
                {
                    // 선택지가 있으면 표시
                    IChoiceOption choiceOption = availableChoices[i];

                    choiceSelectUI.SetData(choiceOption, this);
                    choiceSelectUI.gameObject.SetActive(true);
                    choiceSelectUI.transform.localScale = Vector3.zero;
                    choiceSelectUI.SetButtonInteractable(true);

                    await choiceSelectUI.transform.DOScale(1, uiActiveDelay).SetEase(Ease.OutBack).SetUpdate(true);
                }
                else
                {
                    // 선택지가 부족하면 해당 슬롯 비활성화
                    choiceSelectUI.gameObject.SetActive(false);
                }
            }
        }

        public void OnChoiceSelected(IChoiceOption choiceOption)
        {
            _choiceLeftCount--;

            foreach (var choiceSelectUI in choiceSelectUIs)
            {
                choiceSelectUI.transform.localScale = Vector3.one;
                choiceSelectUI.SetButtonInteractable(false);
                choiceSelectUI.transform.DOScale(0, uiActiveDelay).SetEase(Ease.InBack).SetUpdate(true);
            }

            TriChoiceSelectEvent eventData = new(choiceOption, _choiceLeftCount);
            GameEventSystem.Publish(eventData);

            if (_choiceLeftCount > 0)
            {
                OnInspectionEnter(_choiceLeftCount);
                return;
            }

            backgroundImage.SetActive(false);
        }
    }
}