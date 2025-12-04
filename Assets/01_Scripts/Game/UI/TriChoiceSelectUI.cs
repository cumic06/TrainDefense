using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game.UI
{
    public class TriChoiceSelectUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Fields
        [SerializeField]
        private TextMeshProUGUI nameText;
        [SerializeField]
        private TextMeshProUGUI descriptionText;
        [SerializeField]
        private Image iconImage;
        [SerializeField]
        private TextMeshProUGUI newText;
        #endregion

        private Button _selectButton;
        private IChoiceOption _choiceOption;
        private TriChoiceUI _triChoiceUI;

        private bool _isSelected = false;

        private void Awake()
        {
            _selectButton = GetComponent<Button>();
        }

        private void Start()
        {
            _selectButton.onClick.AddListener(OnSelectButtonClick);
        }

        public void SetData(IChoiceOption choiceOption, TriChoiceUI triChoiceUI)
        {
            if (choiceOption == null) return;

            _choiceOption = choiceOption;

            _triChoiceUI = triChoiceUI;
            SetUI(choiceOption);
        }

        private void SetUI(IChoiceOption choiceOption)
        {
            var uiInfo = choiceOption.GetUIInfo();

            if (string.IsNullOrEmpty(uiInfo.Name) || string.IsNullOrEmpty(uiInfo.Description))
            {
                Debug.LogError($"ChoiceOption [{choiceOption.Id}]: Name or Description is null");
                return;
            }

            iconImage.sprite = uiInfo.Icon;
            nameText.text = uiInfo.Name;

            if (choiceOption is UpgradeTrainChoice upgradeTrainChoice)
            {
                var selectedUpgrade = upgradeTrainChoice.SelectedUpgrade;
                if (selectedUpgrade != null)
                {
                    // 업그레이드 데이터 타입에 따라 스탯 값들을 추출
                    object[] formatArgs = GetUpgradeFormatArgs(selectedUpgrade);
                    descriptionText.text = string.Format(uiInfo.Description, formatArgs);
                }
                else
                {
                    descriptionText.text = uiInfo.Description;
                }
            }
            else
            {
                descriptionText.text = uiInfo.Description;
            }
        }

        private object[] GetUpgradeFormatArgs(ITrainUpgradeData upgradeData)
        {
            var statusUpgrade = upgradeData.StatusUpgrade;
            var args = new System.Collections.Generic.List<object>();

            // 기본 스탯 (모든 Train 타입에 공통)
            if (statusUpgrade.MaxHp != 0)
            {
                args.Add(statusUpgrade.MaxHp);
            }

            // TurretTrain 전용 스탯
            if (upgradeData is TurretTrainUpgradeData turretUpgrade)
            {
                var turretStatus = turretUpgrade.TurretStatusUpgrade;
                if (turretStatus.AttackRange != 0) args.Add(turretStatus.AttackRange);
                if (turretStatus.AttackDamage != 0) args.Add(turretStatus.AttackDamage);
                if (turretStatus.AttackCount != 0) args.Add(turretStatus.AttackCount);
                if (turretStatus.AttackInterval != 0) args.Add(turretStatus.AttackInterval);
                if (turretStatus.TargetCount != 0) args.Add(turretStatus.TargetCount);
            }
            // RangeTrain 전용 스탯
            else if (upgradeData is RangeTrainUpgradeData rangeUpgrade)
            {
                var rangeStatus = rangeUpgrade.RangeStatusUpgrade;
                if (rangeStatus.AttackRange != 0) args.Add(rangeStatus.AttackRange);
                if (rangeStatus.AttackDamage != 0) args.Add(rangeStatus.AttackDamage);
                if (rangeStatus.AttackCount != 0) args.Add(rangeStatus.AttackCount);
                if (rangeStatus.AttackInterval != 0) args.Add(rangeStatus.AttackInterval);
            }

            return args.ToArray();
        }

        private void OnSelectButtonClick()
        {
            _triChoiceUI.OnChoiceSelected(_choiceOption);
            SetSelected(true);
        }

        public void SetButtonInteractable(bool interactable)
        {
            _selectButton.interactable = interactable;
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
        }

        public void SetNewText(bool isNew)
        {
            newText.gameObject.SetActive(isNew);
        }

        #region Pointer Events
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isSelected) return;

            transform.DOKill();
            transform.DOScale(1.1f, 0.1f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isSelected) return;

            transform.DOKill();
            transform.DOScale(1f, 0.1f).SetEase(Ease.InBack).SetUpdate(true);
        }
        #endregion
    }
}