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
        // 효과 줄 속 엘리트 코인 아이콘 크기(기본 글자 크기 대비 %)
        private const int EFFECT_ICON_SIZE_PERCENT = 60;

        #endregion

        #region Fields

        [SerializeField] private TextMeshProUGUI titleText;

        [Tooltip("선택지 카드. 보여 줄 선택지보다 많은 카드는 숨긴다.")]
        [SerializeField] private OptionCard[] optionCards;

        private readonly List<TrackEventData> _options = new List<TrackEventData>();
        private Action<TrackEventData> _onSelected;
        private bool _isSelected;
        // 프리팹에 잡힌 카드 높이(가장 긴 문구 기준)와 효과 칸 최소 높이. 문구가 짧으면 카드를 이보다 줄인다.
        private float _maxCardHeight;
        private float _effectReservedHeight;

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

            if (optionCards.Length > 0)
            {
                _maxCardHeight = ((RectTransform)optionCards[0].button.transform).rect.height;

                if (optionCards[0].effectText.TryGetComponent(out LayoutElement effectLayout))
                    _effectReservedHeight = effectLayout.minHeight;
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
                card.effectText.text = TrackEventText.GetEffectText(data, EFFECT_SEPARATOR, EFFECT_ICON_SIZE_PERCENT);
                card.iconImage.sprite = icon;
                card.iconImage.gameObject.SetActive(icon != null);
            }

            _FitCardHeight();
        }

        // 두 카드 중 긴 문구에 맞춰 카드와 창 높이를 줄인다(짧은 문구 아래가 비어 보이지 않게). 프리팹 배치(카드 300, 효과 칸 확보,
        // 긴 이름은 글자가 줄어듦)를 먼저 그대로 계산해 이름이 실제로 차지하는 높이를 읽고, 그 높이 + 효과 문구 높이로 줄인다.
        // 프리팹 카드 높이가 상한이고 카드 중심은 그대로 둔다. TMP.GetPreferredValues는 자동 크기 글자를 최소 크기로 재는 경우가 있어 쓰지 않는다.
        private void _FitCardHeight()
        {
            float contentHeight = 0f;
            // 프리팹 배치에서 효과 문구가 실제로 차지한 높이(카드마다). 줄어든 카드의 효과 칸 높이로 쓴다.
            var effectHeights = new float[optionCards.Length];

            for (int i = 0; i < optionCards.Length; i++)
            {
                OptionCard card = optionCards[i];

                if (!card.button.gameObject.activeSelf || !card.button.TryGetComponent(out VerticalLayoutGroup layout))
                    continue;

                var cardRect = (RectTransform)card.button.transform;
                // 프리팹 배치로 글자 칸을 잡고, 그 칸에서 글자 자동 크기와 줄 수를 확정한다
                // (TMP의 preferredHeight는 폭이 아슬아슬한 줄을 실제 그리기와 다르게 접는 경우가 있어 그려진 줄로 잰다)
                LayoutRebuilder.ForceRebuildLayoutImmediate(cardRect);
                card.nameText.ForceMeshUpdate();
                card.effectText.ForceMeshUpdate();

                effectHeights[i] = _RenderedTextHeight(card.effectText);
                float height = layout.padding.vertical + _RenderedTextHeight(card.nameText) + layout.spacing + effectHeights[i];

                if (card.iconImage.gameObject.activeSelf)
                    height += LayoutUtility.GetPreferredHeight((RectTransform)card.iconImage.transform) + layout.spacing;

                contentHeight = Mathf.Max(contentHeight, height);
            }

            if (contentHeight <= 0f)
                return;

            float cardHeight = Mathf.Min(Mathf.Ceil(contentHeight), _maxCardHeight);
            bool isShrunk = cardHeight < _maxCardHeight;

            for (int i = 0; i < optionCards.Length; i++)
            {
                OptionCard card = optionCards[i];
                ((RectTransform)card.button.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cardHeight);

                // 줄어든 카드에선 효과 칸을 확보 높이 대신 문구 높이만큼만 잡는다. 이름이 길면 레이아웃이 이름 쪽을 줄인다.
                if (card.effectText.TryGetComponent(out LayoutElement effectLayout))
                    effectLayout.minHeight = isShrunk ? effectHeights[i] : _effectReservedHeight;
            }

            if (optionCards.Length > 0 && optionCards[0].button.transform.parent is RectTransform panel)
                panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panel.rect.height - (_maxCardHeight - cardHeight));
        }

        // 자동 크기가 확정된 글자가 실제로 차지하는 세로 높이(첫 줄 위선 ~ 마지막 줄 아래선 + 위아래 여백)
        private static float _RenderedTextHeight(TMP_Text text)
        {
            TMP_TextInfo info = text.textInfo;

            if (info == null || info.lineCount == 0)
                return 0f;

            float blockHeight = info.lineInfo[0].ascender - info.lineInfo[info.lineCount - 1].descender;

            // TMP 자동 크기는 칸 높이가 글자 높이와 딱 같으면 오차로 한 단계 줄이기도 한다 — 1px 여유
            return Mathf.Ceil(blockHeight + text.margin.y + text.margin.w) + 1f;
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
