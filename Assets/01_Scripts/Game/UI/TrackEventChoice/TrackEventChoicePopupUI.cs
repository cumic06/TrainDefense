using System;
using System.Collections.Generic;
using TMPro;
using TrainDefense.Game.Datas;
using TrainDefense.Game.TrackEvent;
using TrainDefense.Localize;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI.TrackEventChoice
{
    /// <summary>
    /// 선로 선택 이벤트의 2지선다 팝업. 카드 2장(아이콘 + 이름 + 효과 줄)을 보여 주고, 탭한 카드를 콜백으로 돌려준다.
    /// 무엇을 보여 줄지·고른 뒤 무엇을 할지는 TrackEventManager가 정한다 — 이 팝업은 보여 주고 고르는 일만 한다.
    /// 닫기 버튼이 없어 반드시 하나를 골라야 끝난다(BackButtonHandler도 close/cancel 버튼이 없으면 건드리지 않는다).
    /// 자체 Screen Space Overlay 캔버스를 가진 Resources 프리팹이라 부모 없이 띄운다.
    /// </summary>
    public class TrackEventChoicePopupUI : MonoBehaviour
    {
        #region Variables

        [Serializable]
        private class OptionCard
        {
            public Button button;
            public Image iconImage;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI effectText;
        }

        private const string POPUP_RESOURCE_NAME = "Popup_TrackEventChoice";
        private const string TITLE_KEY = "TrackEvent_Choice_Title";
        private const string TITLE_FALLBACK = "갈림길";
        private const string EFFECT_SEPARATOR = "\n";

        #endregion

        #region Fields

        [SerializeField] private TextMeshProUGUI titleText;

        [Tooltip("선택지 카드. 보여 줄 선택지보다 많은 카드는 숨긴다.")]
        [SerializeField] private OptionCard[] optionCards;

        private readonly List<TrackEventData> _options = new List<TrackEventData>();
        private Action<TrackEventData> _onSelected;
        private bool _isSelected;

        #endregion

        #region LifeCycle

        private void OnEnable()
        {
            for (int i = 0; i < optionCards.Length; i++)
            {
                int index = i;
                optionCards[i].button.onClick.AddListener(() => _OnCardClicked(index));
            }

            PopupTween.PlayShow(gameObject);
        }

        private void OnDisable()
        {
            foreach (OptionCard card in optionCards)
                card.button.onClick.RemoveAllListeners();
        }

        private void Awake()
        {
            LocalizeFontSwitcher.Register(titleText);

            foreach (OptionCard card in optionCards)
            {
                LocalizeFontSwitcher.Register(card.nameText);
                LocalizeFontSwitcher.Register(card.effectText);
            }
        }

        private void OnDestroy()
        {
            LocalizeFontSwitcher.Unregister(titleText);

            foreach (OptionCard card in optionCards)
            {
                LocalizeFontSwitcher.Unregister(card.nameText);
                LocalizeFontSwitcher.Unregister(card.effectText);
            }
        }

        #endregion

        /// <summary>Resources의 프리팹을 띄우고 선택지를 채운다. 프리팹이 없으면 null.</summary>
        public static TrackEventChoicePopupUI Open(IReadOnlyList<TrackEventData> options, Action<TrackEventData> onSelected)
        {
            GameObject popupPrefab = Resources.Load<GameObject>(POPUP_RESOURCE_NAME);

            if (popupPrefab == null)
            {
                Debug.LogError($"[TrackEvent] Resources에서 {POPUP_RESOURCE_NAME} 프리팹을 찾을 수 없습니다.");

                return null;
            }

            GameObject popupObject = Instantiate(popupPrefab);

            if (!popupObject.TryGetComponent(out TrackEventChoicePopupUI popup))
            {
                Debug.LogError($"[TrackEvent] {POPUP_RESOURCE_NAME}에 TrackEventChoicePopupUI가 없습니다.");
                Destroy(popupObject);

                return null;
            }

            popup._Setup(options, onSelected);

            return popup;
        }

        /// <summary>고르지 않고 즉시 닫는다(게임 종료 등). 콜백은 부르지 않는다.</summary>
        public void Close()
        {
            _isSelected = true;
            _onSelected = null;
            Destroy(gameObject);
        }

        private void _Setup(IReadOnlyList<TrackEventData> options, Action<TrackEventData> onSelected)
        {
            _options.Clear();
            _options.AddRange(options);
            _onSelected = onSelected;
            _isSelected = false;

            titleText.text = LocalizeHelper.GetByKey(TITLE_KEY, TITLE_FALLBACK);

            for (int i = 0; i < optionCards.Length; i++)
            {
                OptionCard card = optionCards[i];
                bool hasOption = i < _options.Count;
                card.button.gameObject.SetActive(hasOption);

                if (!hasOption)
                    continue;

                TrackEventData data = _options[i];
                Sprite icon = data.Icon;

                card.nameText.text = TrackEventText.GetName(data);
                card.effectText.text = TrackEventText.GetEffectText(data, EFFECT_SEPARATOR);
                card.iconImage.sprite = icon;
                card.iconImage.gameObject.SetActive(icon != null);
            }
        }

        private void _OnCardClicked(int index)
        {
            if (_isSelected || index < 0 || index >= _options.Count)
                return;

            _isSelected = true;
            TrackEventData selected = _options[index];
            Action<TrackEventData> onSelected = _onSelected;
            _onSelected = null;

            PopupTween.PlayHide(gameObject, () => Destroy(gameObject));
            onSelected?.Invoke(selected);
        }
    }
}
