using System.Collections.Generic;
using Cumic;
using Cumic.Events;
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
        private string triChoiceDBPath = "DB/TriChoiceDB";
        #endregion

        private void Start()
        {
            GameEventSystem.Subscribe<EngageReadyEvent>(OnInspectionEnter);
            GameEventSystem.Subscribe<StageEndEvent>(OnStageEnd);
            GameEventSystem.Subscribe<TriChoiceSelectEvent>(OnChoiceSelected);

            if (choiceSelectUIs.Length == 0)
            {
                choiceSelectUIs = GetComponentsInChildren<TriChoiceSelectUI>(true);
            }
        }


        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<EngageReadyEvent>(OnInspectionEnter);
            GameEventSystem.Unsubscribe<TriChoiceSelectEvent>(OnChoiceSelected);
            GameEventSystem.Unsubscribe<StageEndEvent>(OnStageEnd);
        }

        private void OnStageEnd(StageEndEvent stageEndEvent)
        {

        }

        private void OnInspectionEnter(EngageReadyEvent eventData)
        {
            OnChoiceUIPopup().Forget();
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
            TriChoiceDB triChoiceDB = Resources.Load<TriChoiceDB>(triChoiceDBPath);

            if (triChoiceDB == null)
            {
                Debug.LogError("TriChoiceDB not found");
                return null;
            }

            IReadOnlyList<TriChoiceDBData> triChoiceDBDatas = triChoiceDB.TriChoiceDBDatas;

            if (triChoiceDBDatas.Count == 0)
            {
                Debug.LogError("TriChoiceDBDatas Count is 0");
                return null;
            }

            // 전체 가중치 합계 계산
            float totalWeight = 0f;
            foreach (var data in triChoiceDBDatas)
            {
                totalWeight += data.Weight;
            }

            // 가중치가 모두 0이면 균등 확률로 선택
            if (totalWeight <= 0f)
            {
                return triChoiceDBDatas[Random.Range(0, triChoiceDBDatas.Count)].TriChoiceData;
            }

            // 0부터 totalWeight까지의 랜덤 값 생성
            float randomValue = Random.Range(0f, totalWeight);

            // 누적 가중치를 계산하면서 해당 구간의 아이템 찾기
            float currentWeight = 0f;
            foreach (var data in triChoiceDBDatas)
            {
                currentWeight += data.Weight;
                if (randomValue <= currentWeight)
                {
                    return data.TriChoiceData;
                }
            }

            // 혹시나 하는 fallback (일반적으로 실행되지 않음)
            return triChoiceDBDatas[^1].TriChoiceData;
        }
    }
}