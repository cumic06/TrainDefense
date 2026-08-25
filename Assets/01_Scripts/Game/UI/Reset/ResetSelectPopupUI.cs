using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TrainDefense.Localize;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 옵션의 "초기화" 버튼이 띄우는 선택형 초기화 팝업.
    /// 토글로 무엇을 초기화할지 고르고 "초기화"를 누르면 해당 데이터만 삭제한다. ("전체" 선택 시 모든 저장 데이터 삭제)
    /// </summary>
    public class ResetSelectPopupUI : MonoBehaviour
    {
        private const string ResourceName = "Popup_ResetSelect";

        #region Fields
        [Header("초기화 항목 토글")]
        [SerializeField]
        private Toggle collectionToggle;
        [SerializeField]
        private Toggle survivalToggle;
        [SerializeField]
        private Toggle optionToggle;
        [SerializeField]
        private Toggle achievementToggle;
        [SerializeField]
        private Toggle allToggle;

        [Header("버튼")]
        [SerializeField]
        private Button confirmButton;
        [SerializeField]
        private Button cancelButton;

        [Header("정적 라벨 (로컬라이즈)")]
        [SerializeField]
        private TextMeshProUGUI titleText;
        [SerializeField]
        private TextMeshProUGUI collectionLabel;
        [SerializeField]
        private TextMeshProUGUI survivalLabel;
        [SerializeField]
        private TextMeshProUGUI optionLabel;
        [SerializeField]
        private TextMeshProUGUI achievementLabel;
        [SerializeField]
        private TextMeshProUGUI allLabel;
        [SerializeField]
        private TextMeshProUGUI confirmText;
        [SerializeField]
        private TextMeshProUGUI cancelText;
        #endregion

        // 이미 열려 있는 팝업. 버튼에 리스너가 중복으로 걸려 Show가 두 번 불려도 한 장만 뜨게 한다.
        private static ResetSelectPopupUI _openedPopup;

        /// <param name="parent">팝업을 붙일 캔버스. 넘기지 않으면 씬에서 찾지만,
        /// DontDestroyOnLoad 캔버스가 잡히면 씬을 넘어가도 남으므로 호출자가 지정하는 편이 안전하다.</param>
        public static ResetSelectPopupUI Show(Transform parent = null)
        {
            if (_openedPopup != null)
            {
                _openedPopup.transform.SetAsLastSibling();

                return _openedPopup;
            }

            GameObject prefab = Resources.Load<GameObject>(ResourceName);
            if (prefab == null)
            {
                Debug.LogError($"ResetSelectPopupUI: '{ResourceName}' 프리팹을 Resources에서 찾지 못했습니다.");

                return null;
            }

            if (parent == null)
            {
                Canvas canvas = FindObjectOfType<Canvas>();
                parent = canvas != null ? canvas.transform : null;
            }

            GameObject instance = Instantiate(prefab, parent);
            instance.transform.SetAsLastSibling();

            _openedPopup = instance.GetComponent<ResetSelectPopupUI>();

            return _openedPopup;
        }

        private void OnDestroy()
        {
            if (_openedPopup == this)
                _openedPopup = null;
        }

        private void OnEnable()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(_Confirm);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(_Close);

            Localization.OnLanguageChanged += _ApplyStaticTexts;
            Localization.OnInitialized += _ApplyStaticTexts;
            _ApplyStaticTexts();

            PopupTween.PlayShow(gameObject);
        }


        private void OnDisable()
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(_Confirm);

            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(_Close);

            Localization.OnLanguageChanged -= _ApplyStaticTexts;
            Localization.OnInitialized -= _ApplyStaticTexts;
        }

        // 프리팹에 박힌 정적 라벨(타이틀·토글 라벨·버튼)을 현재 언어로 갱신한다.
        private void _ApplyStaticTexts()
        {
            if (titleText != null)
                titleText.text = LocalizeHelper.GetByKey("UI_Reset_Prompt", "무엇을 초기화할까요?");

            if (collectionLabel != null)
                collectionLabel.text = LocalizeHelper.GetByKey("Collection_Title", "도감");

            if (survivalLabel != null)
                survivalLabel.text = LocalizeHelper.GetByKey("UI_Reset_Survival", "최장 생존 기록");

            if (optionLabel != null)
                optionLabel.text = LocalizeHelper.GetByKey("UI_Reset_Option", "옵션");

            if (achievementLabel != null)
                achievementLabel.text = LocalizeHelper.GetByKey("Achievement_Title", "업적");

            if (allLabel != null)
                allLabel.text = LocalizeHelper.GetByKey("UI_Reset_All", "전체");

            if (confirmText != null)
                confirmText.text = LocalizeHelper.GetByKey("UI_Reset", "초기화");

            if (cancelText != null)
                cancelText.text = LocalizeHelper.GetByKey("UI_Cancel", "취소");
        }

        private void _Confirm()
        {
            var userData = UserDataManager.Instance;
            if (userData != null)
            {
                if (allToggle != null && allToggle.isOn)
                {
                    userData.ResetAllData();
                }
                else
                {
                    if (collectionToggle != null && collectionToggle.isOn)
                    {
                        userData.ResetDiscoveredMonsters();
                        userData.ResetDiscoveredTrains();
                    }

                    if (survivalToggle != null && survivalToggle.isOn)
                        userData.ResetAllSurvivalTimes();

                    if (optionToggle != null && optionToggle.isOn)
                        userData.ResetOptions();

                    if (achievementToggle != null && achievementToggle.isOn)
                        userData.ResetAchievements();
                }
            }

            _Close();
        }

        private void _Close()
        {
            PopupTween.PlayHide(gameObject, () => Destroy(gameObject));
        }
    }
}
