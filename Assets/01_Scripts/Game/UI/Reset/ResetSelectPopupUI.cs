using UnityEngine;
using UnityEngine.UI;

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
        private Toggle tutorialToggle;
        [SerializeField]
        private Toggle achievementToggle;
        [SerializeField]
        private Toggle allToggle;

        [Header("버튼")]
        [SerializeField]
        private Button confirmButton;
        [SerializeField]
        private Button cancelButton;
        #endregion

        public static ResetSelectPopupUI Show()
        {
            GameObject prefab = Resources.Load<GameObject>(ResourceName);
            if (prefab == null)
            {
                Debug.LogError($"ResetSelectPopupUI: '{ResourceName}' 프리팹을 Resources에서 찾지 못했습니다.");

                return null;
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            GameObject instance = Instantiate(prefab, canvas != null ? canvas.transform : null);
            instance.transform.SetAsLastSibling();

            return instance.GetComponent<ResetSelectPopupUI>();
        }

        private void OnEnable()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(_Confirm);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(_Close);
        }

        private void OnDisable()
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(_Confirm);

            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(_Close);
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

                    if (tutorialToggle != null && tutorialToggle.isOn)
                        userData.ResetTutorialData();

                    if (achievementToggle != null && achievementToggle.isOn)
                        userData.ResetAchievements();
                }
            }

            _Close();
        }

        private void _Close()
        {
            Destroy(gameObject);
        }
    }
}
