using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrainDefense.Game.Datas;
using TrainDefense.Localize;

namespace TrainDefense.Game.UI.Collection
{
    /// <summary>
    /// 도감 팝업 컨트롤러. 트레인/몬스터 탭을 전환하고, 왼쪽 그리드를 채우며,
    /// 슬롯 선택 시 오른쪽 상세 패널을 갱신한다. UIManager.ShowPopup("Popup_Collection")으로 띄운다.
    /// </summary>
    public class CollectionPopupUI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Button closeButton;

        [Header("Tabs")]
        [SerializeField] private Button trainTabButton;
        [SerializeField] private Button monsterTabButton;
        [SerializeField] private Color tabSelectedColor = Color.white;
        [SerializeField] private Color tabNormalColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        [Header("Localized Texts")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI trainTabText;
        [SerializeField] private TextMeshProUGUI monsterTabText;

        [Header("Grid / Detail")]
        [SerializeField] private Transform gridContent;
        [SerializeField] private CollectionSlotUI slotPrefab;
        [SerializeField] private CollectionDetailUI detailUI;
        #endregion

        private readonly List<CollectionSlotUI> _slots = new();
        private CollectionTabType _currentTab;
        private CollectionSlotUI _selectedSlot;

        private void OnEnable()
        {
            Localization.OnLanguageChanged += _OnLanguageChanged;
            Localization.OnInitialized += _OnLanguageChanged;
            _ApplyStaticTexts();
        }

        private void OnDisable()
        {
            Localization.OnLanguageChanged -= _OnLanguageChanged;
            Localization.OnInitialized -= _OnLanguageChanged;
        }

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (trainTabButton != null)
                trainTabButton.onClick.AddListener(_OnClickTrainTab);

            if (monsterTabButton != null)
                monsterTabButton.onClick.AddListener(_OnClickMonsterTab);
        }

        private void Start()
        {
            _ShowTab(CollectionTabType.Train);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);

            if (trainTabButton != null)
                trainTabButton.onClick.RemoveListener(_OnClickTrainTab);

            if (monsterTabButton != null)
                monsterTabButton.onClick.RemoveListener(_OnClickMonsterTab);
        }

        public void Close()
        {
            Destroy(gameObject);
        }

        private void _OnClickTrainTab()
        {
            _ShowTab(CollectionTabType.Train);
        }

        private void _OnClickMonsterTab()
        {
            _ShowTab(CollectionTabType.Monster);
        }

        private void _OnLanguageChanged()
        {
            _ApplyStaticTexts();

            // 그리드/상세/스탯 라벨도 새 언어로 다시 그린다.
            _ShowTab(_currentTab);
        }

        private void _ApplyStaticTexts()
        {
            if (titleText != null)
                titleText.text = LocalizeHelper.GetByKey("Collection_Title", "도감");

            if (trainTabText != null)
                trainTabText.text = LocalizeHelper.GetByKey("Collection_Tab_Train", "트레인");

            if (monsterTabText != null)
                monsterTabText.text = LocalizeHelper.GetByKey("Collection_Tab_Monster", "몬스터");
        }

        private void _ShowTab(CollectionTabType tab)
        {
            _currentTab = tab;
            _UpdateTabVisual();

            List<CollectionEntry> entries = tab == CollectionTabType.Train
                ? _BuildTrainEntries()
                : _BuildMonsterEntries();

            _PopulateGrid(entries);
        }

        private void _UpdateTabVisual()
        {
            bool isTrain = _currentTab == CollectionTabType.Train;

            if (trainTabButton != null)
                trainTabButton.image.color = isTrain ? tabSelectedColor : tabNormalColor;

            if (monsterTabButton != null)
                monsterTabButton.image.color = isTrain ? tabNormalColor : tabSelectedColor;
        }

        private List<CollectionEntry> _BuildTrainEntries()
        {
            var result = new List<CollectionEntry>();
            UserDataManager userDataManager = UserDataManager.Instance;

            foreach (TrainData data in DatabaseManager.Instance.GetAllTrainData())
            {
                if (data == null)
                    continue;

                bool discovered = userDataManager != null && userDataManager.IsTrainDiscovered(data.Id);
                result.Add(CollectionEntry.FromTrain(data, discovered));
            }

            return result;
        }

        private List<CollectionEntry> _BuildMonsterEntries()
        {
            var result = new List<CollectionEntry>();
            UserDataManager userDataManager = UserDataManager.Instance;

            foreach (MonsterData data in DatabaseManager.Instance.GetMonsterDatas())
            {
                if (data == null)
                    continue;

                bool discovered = userDataManager != null && userDataManager.IsMonsterDiscovered(data.Id);
                result.Add(CollectionEntry.FromMonster(data, discovered));
            }

            return result;
        }

        private void _PopulateGrid(List<CollectionEntry> entries)
        {
            while (_slots.Count < entries.Count)
            {
                CollectionSlotUI slot = Instantiate(slotPrefab, gridContent);
                _slots.Add(slot);
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                if (i < entries.Count)
                {
                    _slots[i].gameObject.SetActive(true);
                    _slots[i].Init(entries[i], _OnSelectSlot);
                }
                else
                {
                    _slots[i].gameObject.SetActive(false);
                }
            }

            _selectedSlot = null;

            if (entries.Count > 0)
            {
                _OnSelectSlot(_slots[0]);
            }
            else if (detailUI != null)
            {
                detailUI.Show(null);
            }
        }

        private void _OnSelectSlot(CollectionSlotUI slot)
        {
            if (slot == null)
                return;

            if (_selectedSlot != null)
                _selectedSlot.SetSelected(false);

            _selectedSlot = slot;
            _selectedSlot.SetSelected(true);

            if (detailUI != null)
                detailUI.Show(slot.Entry);
        }
    }
}
