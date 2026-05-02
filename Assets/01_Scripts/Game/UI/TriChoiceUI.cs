using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

        [Header("Reroll")]
        [SerializeField]
        private Button rerollButton;
        #endregion

        private int _choiceLeftCount;
        private bool _isSelecting = false;
        private int _popupRequestId = 0;

        private void Awake()
        {
            if (choiceSelectUIs.Length == 0)
            {
                choiceSelectUIs = GetComponentsInChildren<TriChoiceSelectUI>(true);
            }

            if (rerollButton != null)
            {
                rerollButton.onClick.AddListener(OnRerollButtonClick);
            }
        }

        private void Start()
        {
            GameEventSystem.Subscribe<GameEnterEvent>(OnGameEnter);
            GameEventSystem.Subscribe<LevelUpEvent>(OnLevelUp);
            GameEventSystem.Subscribe<StageEndEvent>(OnStageEnd);
            GameEventSystem.Subscribe<GameEndEvent>(OnGameEnd);
        }

        private void OnDestroy()
        {
            GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
            GameEventSystem.Unsubscribe<LevelUpEvent>(OnLevelUp);
            GameEventSystem.Unsubscribe<StageEndEvent>(OnStageEnd);
            GameEventSystem.Unsubscribe<GameEndEvent>(OnGameEnd);
        }

        private void OnGameEnter(GameEnterEvent gameEnterEvent)
        {
            OnInspectionEnter(1);
        }

        private void OnLevelUp(LevelUpEvent levelUpEvent)
        {
            OnInspectionEnter(levelUpEvent.LevelUpCount);
        }

        private void OnStageEnd(StageEndEvent stageEndEvent)
        {
            gameObject.SetActive(false);
        }

        private void OnGameEnd(GameEndEvent gameEndEvent)
        {
            gameObject.SetActive(false);
        }

        public void OnInspectionEnter(int count)
        {
            backgroundImage.SetActive(true);

            int requestId = ++_popupRequestId;
            OnChoiceUIPopup(count, requestId).Forget();
        }

        private async UniTask OnChoiceUIPopup(int count, int requestId)
        {
            _choiceLeftCount = count;
            _isSelecting = false;

            var triChoiceManager = TriChoiceManager.Instance;
            if (triChoiceManager == null)
            {
                Debug.LogError("TriChoiceManager not found");
                return;
            }

            SoundManager.Instance.SuppressSFX(true);

            List<ChoiceEntry> availableChoices = triChoiceManager.GetChoices(choiceSelectUIs.Length);

            if (requestId != _popupRequestId) return;

            if (availableChoices.Count == 0)
            {
                TriChoiceSelectEvent eventData = new(null, 0);
                GameEventSystem.Publish(eventData);
                backgroundImage.SetActive(false);
                Debug.LogWarning("No available choices found");
                return;
            }

            if (coinParticleSystem != null)
            {
                coinParticleSystem.gameObject.SetActive(true);
                coinParticleSystem.Play();
            }

            for (int i = 0; i < choiceSelectUIs.Length; i++)
            {
                if (requestId != _popupRequestId) return;

                var choiceSelectUI = choiceSelectUIs[i];
                choiceSelectUI.SetSelected(false);

                if (i < availableChoices.Count)
                {
                    IChoiceOption choiceOption = availableChoices[i].Option;

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

                            TrainPassiveSkillData passiveData = null;
                            if (trainData is RangeTrainData rangeData)
                                passiveData = rangeData.PassiveSkillData;
                            else if (trainData is TurretTrainData turretData)
                                passiveData = turretData.PassiveSkillData;

                            switch (addTrainChoice.SkillType)
                            {
                                case AddTrainChoiceSkillType.Passive:
                                    if (passiveData != null)
                                    {
                                        if (!string.IsNullOrEmpty(passiveData.Name))
                                            choiceUIInfo.PassiveName = passiveData.Name;
                                        if (!string.IsNullOrEmpty(passiveData.Description))
                                            choiceUIInfo.PassiveDescription = passiveData.Description;
                                    }
                                    break;
                                case AddTrainChoiceSkillType.Active:
                                    var skillData = trainData.TrainSkillData;
                                    if (skillData != null && skillData.HasActiveSkill)
                                    {
                                        if (!string.IsNullOrEmpty(skillData.Name))
                                            choiceUIInfo.ActiveSkillName = skillData.Name;
                                        if (!string.IsNullOrEmpty(skillData.Description))
                                            choiceUIInfo.PassiveDescription = skillData.Description;
                                    }
                                    break;
                                default:
                                    if (passiveData != null)
                                    {
                                        if (!string.IsNullOrEmpty(passiveData.Name))
                                            choiceUIInfo.PassiveName = passiveData.Name;
                                        if (!string.IsNullOrEmpty(passiveData.Description))
                                            choiceUIInfo.PassiveDescription = passiveData.Description;
                                    }
                                    break;
                            }
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
                        choiceSelectUI.gameObject.SetActive(false);
                        continue;
                    }

                    choiceSelectUI.SetData(choiceOption, choiceUIInfo, this);
                    choiceSelectUI.gameObject.SetActive(true);
                    choiceSelectUI.SetButtonInteractable(true);

                    choiceSelectUI.transform.DOKill();
                    choiceSelectUI.transform.localScale = Vector3.zero;

                    await choiceSelectUI.transform.DOScale(1, uiActiveDelay).SetEase(Ease.OutBack).OnComplete(() =>
                    {
                        choiceSelectUI.transform.localScale = Vector3.one;
                    }).SetUpdate(true);

                    if (requestId != _popupRequestId) return;
                }
                else
                {
                    choiceSelectUI.gameObject.SetActive(false);
                }
            }
        }

        public async UniTaskVoid OnChoiceSelected(IChoiceOption choiceOption)
        {
            if (_isSelecting) return;
            _isSelecting = true;
            _popupRequestId++;

            _choiceLeftCount--;

            var tasks = new List<UniTask>();

            foreach (var choiceSelectUI in choiceSelectUIs)
            {
                choiceSelectUI.transform.DOKill();
                choiceSelectUI.transform.localScale = Vector3.one;
                choiceSelectUI.SetButtonInteractable(false);
                choiceSelectUI.SetSelected(true);
                choiceSelectUI.SetNewText(false);

                tasks.Add(choiceSelectUI.transform.DOScale(0, uiActiveDelay)
                    .SetEase(Ease.InBack)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        choiceSelectUI.transform.localScale = Vector3.zero;
                    })
                    .ToUniTask());
            }

            await UniTask.WhenAll(tasks);

            TriChoiceSelectEvent eventData = new(choiceOption, _choiceLeftCount);
            GameEventSystem.Publish(eventData);

            _isSelecting = false;

            if (_choiceLeftCount > 0)
            {
                OnInspectionEnter(_choiceLeftCount);
                return;
            }

            if (coinParticleSystem != null)
            {
                coinParticleSystem.gameObject.SetActive(false);
            }

            SoundManager.Instance.SuppressSFX(false);

            backgroundImage.SetActive(false);
        }

        private void OnRerollButtonClick()
        {
            if (_isSelecting) return;

            var triChoiceManager = TriChoiceManager.Instance;
            if (triChoiceManager != null)
            {
                triChoiceManager.ClearSelectedUpgrades();
            }

            // 현재 선택지 UI를 숨기고 새로운 선택지로 다시 표시
            foreach (var choiceSelectUI in choiceSelectUIs)
            {
                choiceSelectUI.transform.DOKill();
                choiceSelectUI.gameObject.SetActive(false);
            }

            int requestId = ++_popupRequestId;
            OnChoiceUIPopup(_choiceLeftCount, requestId).Forget();
        }
    }
}