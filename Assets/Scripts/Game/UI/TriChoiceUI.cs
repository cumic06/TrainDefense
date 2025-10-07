using System.Collections.Generic;
using Cumic.Events;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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
        private string triChoiceDBPath = "DB/TriChoiceDB";
        #endregion

        private int _choiceLeftCount;

        private void Awake()
        {
            if (choiceSelectUIs.Length == 0)
            {
                choiceSelectUIs = GetComponentsInChildren<TriChoiceSelectUI>(true);
            }
        }

        private void Start()
        {
            GameEventSystem.Subscribe<TriChoiceSelectEvent>(OnChoiceSelected);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<TriChoiceSelectEvent>(OnChoiceSelected);
        }

        public void OnInspectionEnter(int count)
        {
            OnChoiceUIPopup(count).Forget();
        }

        private async UniTask OnChoiceUIPopup(int count)
        {
            _choiceLeftCount = count;
            foreach (var choiceSelectUI in choiceSelectUIs)
            {
                var choiceOption = RandomChoice();

                if (choiceOption == null) continue;

                choiceSelectUI.SetData(choiceOption, _choiceLeftCount);

                choiceSelectUI.transform.localScale = Vector3.zero;

                await choiceSelectUI.transform.DOScale(1, uiActiveDelay).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        private void OnChoiceSelected(TriChoiceSelectEvent eventData)
        {
            // Sync remaining count from event
            _choiceLeftCount = eventData.ChoiceLeftCount;

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
        }

        private ChoiceOption RandomChoice()
        {
            TriChoiceDB triChoiceDB = Resources.Load<TriChoiceDB>(triChoiceDBPath);

            if (triChoiceDB == null)
            {
                Debug.LogError("TriChoiceDB not found");
                return null;
            }

            // 각 카테고리에서 유효한 선택지만 필터링
            List<ChoiceEntry> validAddTrain = GetValidChoices(triChoiceDB.AddTrainChoices);
            List<ChoiceEntry> validUpgradeTrain = GetValidChoices(triChoiceDB.UpgradeTrainChoices);

            // 유효한 카테고리 수집
            List<List<ChoiceEntry>> validCategories = new();
            if (validAddTrain.Count > 0) validCategories.Add(validAddTrain);
            if (validUpgradeTrain.Count > 0) validCategories.Add(validUpgradeTrain);

            if (validCategories.Count == 0)
            {
                Debug.LogWarning("No valid choices available");
                return null;
            }

            // 카테고리 중 하나를 균등 확률로 선택
            List<ChoiceEntry> selectedCategory = validCategories[Random.Range(0, validCategories.Count)];

            // 선택된 카테고리 내에서 가중치 기반 선택
            ChoiceOption selectedOption = SelectFromChoices(selectedCategory);

            // 선택지 초기화 (업그레이드 가중치 랜덤 선택)
            if (selectedOption != null)
            {
                selectedOption.Initialize();
            }

            return selectedOption;
        }

        private List<ChoiceEntry> GetValidChoices(IReadOnlyList<ChoiceEntry> entries)
        {
            List<ChoiceEntry> validChoices = new();

            foreach (var entry in entries)
            {
                if (entry.Option != null && entry.Option.IsValid())
                {
                    validChoices.Add(entry);
                }
            }

            return validChoices;
        }

        private ChoiceOption SelectFromChoices(List<ChoiceEntry> choices)
        {
            if (choices.Count == 0) return null;
            if (choices.Count == 1) return choices[0].Option;

            int totalWeight = 0;
            foreach (var entry in choices)
            {
                totalWeight += entry.Weight;
            }

            // 가중치가 모두 0이면 균등 확률로 선택
            if (totalWeight <= 0)
            {
                return choices[Random.Range(0, choices.Count)].Option;
            }

            // 가중치 기반 랜덤 선택
            int randomValue = Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var entry in choices)
            {
                currentWeight += entry.Weight;

                if (randomValue < currentWeight)
                {
                    return entry.Option;
                }
            }

            return choices[^1].Option;
        }
    }
}
