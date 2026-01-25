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
        [SerializeField]
        private ParticleSystem coinParticleSystem;
        #endregion

        private int _choiceLeftCount;

        // 중복 선택 방지를 위한 플래그
        private bool _isSelecting = false;

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

            // 팝업이 뜰 때 선택 가능 상태로 초기화 (약간의 딜레이 후 설정하거나 바로 설정)
            _isSelecting = false;

            var triChoiceManager = TriChoiceManager.Instance;
            if (triChoiceManager == null)
            {
                Debug.LogError("TriChoiceManager not found");
                return;
            }

            // 선택지 풀을 미리 생성 (중복 없이)
            List<ChoiceEntry> availableChoices = triChoiceManager.GetChoices(choiceSelectUIs.Length);

            if (availableChoices.Count == 0)
            {
                TriChoiceSelectEvent eventData = new(null, 0);
                GameEventSystem.Publish(eventData); //우선 선택지 없으면 이벤트 쏴서 시작되게.
                backgroundImage.SetActive(false);
                Debug.LogWarning("No available choices found");
                return;
            }

            if (coinParticleSystem != null)
            {
                coinParticleSystem.gameObject.SetActive(true);
                coinParticleSystem.Play();
            }

            // 사용 가능한 선택지 수만큼만 UI 표시
            for (int i = 0; i < choiceSelectUIs.Length; i++)
            {
                var choiceSelectUI = choiceSelectUIs[i];
                choiceSelectUI.SetSelected(false);

                if (i < availableChoices.Count)
                {
                    // 선택지가 있으면 표시
                    IChoiceOption choiceOption = availableChoices[i].Option;

                    // 처음 획득하는 ChoiceOption인지 확인
                    var userDataManager = UserDataManager.Instance;

                    if (userDataManager != null)
                    {
                        bool isFirstTime = userDataManager.IsFirstTimeSelected(choiceOption.Id);
                        choiceSelectUI.SetNewText(isFirstTime);
                    }

                    ChoiceUIInfo choiceUIInfo = new();
                    if (choiceOption is AddTrainChoice addTrainChoice)
                    {
                        var trainData = DatabaseManager.Instance.GetTrainData(addTrainChoice.TrainDataId);
                        if (trainData != null)
                        {
                            choiceUIInfo.Icon = trainData.Icon;
                            choiceUIInfo.Name = trainData.Name;
                            choiceUIInfo.Description = trainData.Description;
                        }
                    }
                    else if (choiceOption is UpgradeTrainChoice upgradeTrainChoice)
                    {
                        var upgradeData = triChoiceManager.GetSelectedUpgrade(upgradeTrainChoice);

                        if (upgradeData == null)
                        {
                            Debug.LogError($"UpgradeTrainChoice [{upgradeTrainChoice.Id}]: SelectedUpgrade is null");
                            choiceSelectUI.gameObject.SetActive(false);
                            continue;
                        }

                        choiceUIInfo.Icon = upgradeData.Icon;
                        choiceUIInfo.Name = upgradeData.Name;
                        choiceUIInfo.Description = upgradeData.Description;
                    }
                    else
                    {
                        Debug.LogError($"ChoiceOption [{choiceOption.Id}]: Unknown choice type");
                        return;
                    }

                    choiceSelectUI.SetData(choiceOption, choiceUIInfo, this);
                    choiceSelectUI.gameObject.SetActive(true);
                    choiceSelectUI.SetButtonInteractable(true);

                    choiceSelectUI.transform.localScale = Vector3.zero;

                    await choiceSelectUI.transform.DOScale(1, uiActiveDelay).SetEase(Ease.OutBack).OnComplete(() =>
                    {
                        choiceSelectUI.transform.localScale = Vector3.one;
                    }).SetUpdate(true);
                }
                else
                {
                    // 선택지가 부족하면 해당 슬롯 비활성화
                    choiceSelectUI.gameObject.SetActive(false);
                }
            }
        }

        public async UniTaskVoid OnChoiceSelected(IChoiceOption choiceOption)
        {
            if (_isSelecting) return;
            _isSelecting = true;

            _choiceLeftCount--;

            var tasks = new List<UniTask>();

            foreach (var choiceSelectUI in choiceSelectUIs)
            {
                choiceSelectUI.transform.localScale = Vector3.one;
                choiceSelectUI.SetButtonInteractable(false);
                choiceSelectUI.SetSelected(true);
                choiceSelectUI.SetNewText(false);

                // DOTween을 비동기로 대기
                tasks.Add(choiceSelectUI.transform.DOScale(0, uiActiveDelay)
                    .SetEase(Ease.InBack)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        choiceSelectUI.transform.localScale = Vector3.zero;
                    })
                    .ToUniTask());
            }

            // 모든 애니메이션이 끝날 때까지 대기
            await UniTask.WhenAll(tasks);

            TriChoiceSelectEvent eventData = new(choiceOption, _choiceLeftCount);
            GameEventSystem.Publish(eventData);

            if (_choiceLeftCount > 0)
            {
                OnInspectionEnter(_choiceLeftCount);
                return;
            }

            if (coinParticleSystem != null)
            {
                coinParticleSystem.gameObject.SetActive(false);
            }

            backgroundImage.SetActive(false);
        }
    }
}