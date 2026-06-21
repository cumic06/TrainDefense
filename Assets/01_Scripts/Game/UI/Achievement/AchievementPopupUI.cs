using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cumic.Achievement;
using TrainDefense.Localize;

namespace TrainDefense.Game.UI.Achievement
{
    /// <summary>
    /// 업적 팝업 컨트롤러. 카탈로그 정의와 저장된 진행 상태를 합쳐 그리드를 채우고,
    /// 슬롯 선택 시 오른쪽 상세 패널을 갱신한다. AchievementButton이 Popup_Achievement 프리팹을 띄운다.
    /// (도감 CollectionPopupUI 패턴, 탭 없이 전체 목록)
    /// </summary>
    public class AchievementPopupUI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Grid / Detail")]
        [SerializeField] private Transform gridContent;
        [SerializeField] private AchievementSlotUI slotPrefab;
        [SerializeField] private AchievementDetailUI detailUI;
        #endregion

        private readonly List<AchievementSlotUI> _slots = new();
        private AchievementSlotUI _selectedSlot;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

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

        private void Start()
        {
            _Refresh();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        public void Close()
        {
            Destroy(gameObject);
        }

        private void _OnLanguageChanged()
        {
            _ApplyStaticTexts();
            _Refresh();
        }

        private void _ApplyStaticTexts()
        {
            if (titleText != null)
                titleText.text = LocalizeHelper.GetByKey("Achievement_Title", "업적");
        }

        private void _Refresh()
        {
            List<AchievementEntry> entries = _BuildEntries();
            _PopulateGrid(entries);
        }

        private List<AchievementEntry> _BuildEntries()
        {
            // 진행 상태는 저장본에서 직접 읽어 인게임 트래커 없이도 로비에서 표시할 수 있게 한다.
            AchievementSaveData saveData = AchievementSaveData.Load();
            var result = new List<AchievementEntry>();

            foreach (AchievementData data in AchievementCatalog.All)
            {
                if (data == null)
                    continue;

                AchievementState state = saveData.GetState(data.Id);
                result.Add(new AchievementEntry(data, state));
            }

            return result;
        }

        private void _PopulateGrid(List<AchievementEntry> entries)
        {
            while (_slots.Count < entries.Count)
            {
                AchievementSlotUI slot = Instantiate(slotPrefab, gridContent);
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
                _OnSelectSlot(_slots[0]);
            else if (detailUI != null)
                detailUI.Show(null);
        }

        private void _OnSelectSlot(AchievementSlotUI slot)
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
