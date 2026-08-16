using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrainDefense.Game.UI.Collection
{
    /// <summary>
    /// 도감 그리드의 한 칸. 발견한 유닛은 아이콘과 이름을, 미발견은 검은 실루엣과 "???"를 표시한다.
    /// 클릭하면 자신을 콜백으로 넘겨 상세 패널 갱신과 선택 하이라이트를 팝업이 처리하게 한다.
    /// </summary>
    public class CollectionSlotUI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Button slotButton;
        [SerializeField] private Image iconImage;
        [SerializeField] private Material outlineMaterial;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private GameObject selectedFrame;
        [SerializeField] private Color lockedColor = Color.black;
        #endregion

        private CollectionEntry _entry;
        private Vector2 _iconBaseSize;
        private float _iconBaselineY;
        private Action<CollectionSlotUI> _onSelect;

        public CollectionEntry Entry => _entry;

        private void Awake()
        {
            if (slotButton != null)
                slotButton.onClick.AddListener(_OnClick);

            if (iconImage != null)
            {
                _iconBaseSize = iconImage.rectTransform.sizeDelta;
                _iconBaselineY = iconImage.rectTransform.anchoredPosition.y;
            }
        }

        private void OnDestroy()
        {
            if (slotButton != null)
                slotButton.onClick.RemoveListener(_OnClick);
        }

        public void Init(CollectionEntry entry, Action<CollectionSlotUI> onSelect, Vector2 tabMaxBody)
        {
            _entry = entry;
            _onSelect = onSelect;

            if (iconImage != null)
            {
                iconImage.sprite = entry.Icon;
                iconImage.enabled = entry.Icon != null;
                iconImage.color = entry.IsDiscovered ? Color.white : lockedColor;
            }

            // 몬스터는 인게임 상대 크기·바닥 정렬 + 발견 시 흰 외곽선(인게임과 동일), 트레인은 칸 채움
            CollectionIconStyle.Fit(iconImage, entry, _iconBaseSize, _iconBaselineY, tabMaxBody);
            if (iconImage != null)
                iconImage.material = entry.IsMonster && entry.IsDiscovered ? outlineMaterial : null;

            if (nameText != null)
                nameText.text = entry.IsDiscovered ? entry.Name : "???";

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectedFrame != null)
                selectedFrame.SetActive(selected);
        }

        private void _OnClick()
        {
            _onSelect?.Invoke(this);
        }
    }
}
