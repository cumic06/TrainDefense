using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cumic.Events;
using Cumic.Sequence;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.Manager;
using TrainDefense.Game.TrackEvent;
using TrainDefense.Game.Tutorial;
using TrainDefense.Localize;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 맵(스테이지)에 처음 들어설 때 화면 상단에 맵 이름을 잠깐 보여주는 배너.
    /// 표시 트리거는 EngageStartEvent 하나 — 첫 맵은 게임 진입 직후가 아니라
    /// "첫 삼중택일이 끝나야" InGameSequence가 EngageStartEvent를 발행하므로 선택지를 가리지 않는다.
    /// 튜토리얼 등 오버레이가 떠 있으면 걷힐 때까지 대기 후 표시한다.
    /// EngageStartEvent는 상점 퇴장마다 발행되므로 "직전에 보여준 스테이지와 다른 경우"에만 표시한다.
    /// 날씨와 무작위 선로 이벤트가 시작되면(TrackEventStartEvent) 이름 + 효과도 같은 배너로 차례로 알린다.
    /// 날씨 이벤트는 이름 왼쪽에 날씨 아이콘(TrackEventWeatherIcons)을 함께 띄운다.
    /// 시각 요소는 코드에서 직접 생성해 Inspector wiring 없이 동작한다(GameBootstrapper가 전역 캔버스에 부착).
    /// </summary>
    public class MapNameBannerUI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private float fadeDuration = 0.35f;
        [SerializeField] private float holdDuration = 1.8f;
        [Tooltip("오버레이(튜토리얼/삼중택일)가 걷힌 뒤 이 시간(초)만큼 연속으로 비어 있어야 표시 — 다중 선택 사이 프레임 틈 오발 방지")]
        [SerializeField] private float overlayClearSettle = 0.35f;
        #endregion

        #region Variables
        private const string WEATHER_EFFECT_SEPARATOR = "  /  ";
        // 줄 첫 글자의 획이 글자 칸 왼쪽 끝을 1px 미만 벗어나는 언어가 있어(스페인어 등) 좌우에 여유를 둔다.
        private const float TEXT_SIDE_MARGIN = 2f;
        // 효과 줄 속 엘리트 코인 아이콘 크기(기본 글자 크기 대비 %). 효과 줄이 65%라 카드보다 작게 둔다.
        private const int EFFECT_ICON_SIZE_PERCENT = 32;
        // 바 높이: 맵 이름 한 줄은 원래대로, 이름 + 효과 두 줄은 글자가 바 위·아래에 붙지 않게 높게
        private const float BAR_HEIGHT_NAME_ONLY = 110f;
        private const float BAR_HEIGHT_WITH_EFFECT = 140f;
        // 이벤트 이름 왼쪽에 붙는 날씨 아이콘 크기와 이름과의 간격. 이름 글자(46)와 비슷한 높이로 둔다.
        private const float EVENT_ICON_SIZE = 48f;
        private const float EVENT_ICON_GAP = 12f;

        private readonly Queue<(string message, TrackEventData eventData)> _pendingMessages = new Queue<(string, TrackEventData)>();
        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _nameText;
        private Image _eventIcon;
        private Coroutine _playRoutine;
        // EngageStartEvent가 역 정차(상점 퇴장)마다 발행되므로, 같은 맵에서의 중복 표시를 막는 가드.
        private string _lastShownStageId;
        #endregion

        #region LifeCycle
        private void OnEnable()
        {
            _SubscribeEvents();
        }

        private void OnDisable()
        {
            _UnsubscribeEvents();

            // 비활성화되면 코루틴이 멈추므로, 다시 켜졌을 때 대기열이 막히지 않게 진행 상태를 비운다.
            _playRoutine = null;
            _pendingMessages.Clear();

            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }

        private void Awake()
        {
            _BuildVisual();
        }

        private void OnDestroy()
        {
            if (_nameText != null)
                LocalizeFontSwitcher.Unregister(_nameText);
        }
        #endregion

        #region Sub/UnSub
        private void _SubscribeEvents()
        {
            GameEventSystem.Subscribe<GameEnterEvent>(_OnGameEnter);
            GameEventSystem.Subscribe<EngageStartEvent>(_OnEngageStart);
            GameEventSystem.Subscribe<TrackEventStartEvent>(_OnTrackEventStart);
        }

        private void _UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<GameEnterEvent>(_OnGameEnter);
            GameEventSystem.Unsubscribe<EngageStartEvent>(_OnEngageStart);
            GameEventSystem.Unsubscribe<TrackEventStartEvent>(_OnTrackEventStart);
        }
        #endregion

        private void _OnGameEnter(GameEnterEvent e)
        {
            // 로비 배경 시뮬레이션(LobbyGameSimulation)도 GameEnterEvent를 발행하므로 로비 분은 무시한다.
            if (e == null || e.IsLobby)
                return;

            // 새 판 시작 — 직전 판에서 보여준 맵 기록과 못 보여 준 대기 문구를 지운다. 실제 표시는 첫 삼중택일이
            // 끝나고 발행되는 EngageStartEvent(InGameSequence._OnTriChoiceSelect)에서 한다.
            _lastShownStageId = null;
            _pendingMessages.Clear();
        }

        private void _OnEngageStart(EngageStartEvent e)
        {
            _ShowCurrentStageName();
        }

        private void _ShowCurrentStageName()
        {
            var stageManager = StageManager.Instance;

            if (stageManager == null)
                return;

            var stage = stageManager.CurrentStageData;

            if (stage == null || stage.Id == _lastShownStageId)
                return;

            _lastShownStageId = stage.Id;

            string mapName = LocalizeHelper.GetByKey(stage.NameKey, stage.Id);

            _Enqueue(mapName, null);
        }

        // 날씨와 무작위 선로 이벤트가 시작되면 이름 + 효과 줄을 맵 이름 다음 차례로 보여 준다.
        // 선택 이벤트는 카드에 이미 문구가 있고, 보너스 강화는 레벨업 창이 바로 떠서 알릴 필요가 없다.
        private void _OnTrackEventStart(TrackEventStartEvent e)
        {
            if (e == null || e.Data == null || e.Data.Category == TrackEventCategory.Choice || e.Data.EventType == TrackEventType.FreeUpgrade)
                return;

            string effectText = TrackEventText.GetEffectText(e.Data, WEATHER_EFFECT_SEPARATOR, EFFECT_ICON_SIZE_PERCENT, true);
            string message = string.IsNullOrEmpty(effectText)
                ? TrackEventText.GetName(e.Data)
                : $"{TrackEventText.GetName(e.Data)}\n<size=65%>{effectText}</size>";

            _Enqueue(message, e.Data);
        }

        // 맵 이름과 날씨는 같은 EngageStartEvent에서 함께 오므로 겹쳐 지우지 않고 차례로 보여 준다.
        private void _Enqueue(string message, TrackEventData eventData)
        {
            _pendingMessages.Enqueue((message, eventData));

            if (_playRoutine == null)
                _playRoutine = StartCoroutine(_PlayQueueRoutine());
        }

        private IEnumerator _PlayQueueRoutine()
        {
            while (_pendingMessages.Count > 0)
            {
                (string message, TrackEventData eventData) = _pendingMessages.Dequeue();
                yield return _PlayRoutine(message, eventData);
            }

            _playRoutine = null;
        }

        private IEnumerator _PlayRoutine(string message, TrackEventData eventData)
        {
            // 튜토리얼/삼중택일(레벨업) 오버레이가 떠 있는 동안은 대기해 연출을 가리지 않는다.
            // 걷힌 뒤에도 overlayClearSettle만큼 연속으로 비어 있어야 표시한다.
            float clearTime = 0f;
            while (clearTime < overlayClearSettle)
            {
                clearTime = _IsBlocked() ? 0f : clearTime + Time.unscaledDeltaTime;
                yield return null;
            }

            // 기다리는 사이 역에 도착해 이벤트가 이미 끝났으면 알리지 않는다.
            if (eventData != null && !_IsEventActive(eventData))
                yield break;

            ((RectTransform)transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                message.IndexOf('\n') >= 0 ? BAR_HEIGHT_WITH_EFFECT : BAR_HEIGHT_NAME_ONLY);

            if (_nameText != null)
                _nameText.text = message;

            _ApplyEventIcon(eventData);

            yield return _Fade(0f, 1f, fadeDuration);
            yield return new WaitForSecondsRealtime(holdDuration);
            yield return _Fade(1f, 0f, fadeDuration);
        }

        // 날씨 이벤트면 이름(첫 줄) 바로 왼쪽에 아이콘을 붙인다. 아이콘 + 간격만큼 왼쪽 여백을 줘서
        // 아이콘과 이름 묶음이 바 가운데에 오게 한다. 맵 이름이나 아이콘 없는 이벤트는 여백을 원래대로 돌린다.
        private void _ApplyEventIcon(TrackEventData eventData)
        {
            if (_eventIcon == null || _nameText == null)
                return;

            Sprite icon = TrackEventWeatherIcons.GetIcon(eventData);
            bool hasIcon = icon != null;
            float leftMargin = hasIcon ? TEXT_SIDE_MARGIN + EVENT_ICON_SIZE + EVENT_ICON_GAP : TEXT_SIDE_MARGIN;

            _nameText.margin = new Vector4(leftMargin, 0f, TEXT_SIDE_MARGIN, 0f);
            _eventIcon.sprite = icon;
            _eventIcon.gameObject.SetActive(false);

            if (!hasIcon)
                return;

            // 자동 크기 조정이 끝난 실제 글자 배치를 읽어 첫 줄 왼쪽 끝에 맞춘다.
            _nameText.ForceMeshUpdate();
            TMP_TextInfo textInfo = _nameText.textInfo;

            if (textInfo == null || textInfo.lineCount == 0)
                return;

            TMP_LineInfo firstLine = textInfo.lineInfo[0];
            float lineCenterY = (firstLine.ascender + firstLine.descender) * 0.5f;

            _eventIcon.rectTransform.anchoredPosition =
                new Vector2(firstLine.lineExtents.min.x - EVENT_ICON_GAP - EVENT_ICON_SIZE * 0.5f, lineCenterY);
            _eventIcon.gameObject.SetActive(true);
        }

        private static bool _IsEventActive(TrackEventData eventData)
        {
            var trackEventManager = TrackEventManager.Instance;

            return trackEventManager != null && (trackEventManager.ActiveWeatherData == eventData || trackEventManager.ActiveData == eventData);
        }

        // 배너 표시를 막아야 하는 화면이 떠 있는지. 오버레이 스택(튜토리얼/삼중택일/일시정지/옵션) +
        // 시간을 멈추지 않는 튜토리얼(ShouldPauseTime=false, 오버레이 미푸시)도 IsActive로 잡는다.
        private static bool _IsBlocked()
        {
            var sequence = InGameSequence.Instance;

            if (sequence != null && sequence.CurrentOverlays != OverlayPhase.None)
                return true;

            var tutorial = TutorialManager.Instance;

            return tutorial != null && tutorial.IsActive;
        }

        private IEnumerator _Fade(float from, float to, float duration)
        {
            if (_canvasGroup == null || duration <= 0f)
            {
                if (_canvasGroup != null)
                    _canvasGroup.alpha = to;

                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }

            _canvasGroup.alpha = to;
        }

        // 배너 비주얼(반투명 바 + 이름 텍스트)을 코드에서 직접 구성한다.
        // 프리팹/Inspector 연결이 없어 wiring 누락이 원천적으로 발생하지 않는다.
        private void _BuildVisual()
        {
            var root = (RectTransform)transform;
            root.anchorMin = new Vector2(0.5f, 1f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            // 업적 토스트(y -140, 높이 96) 바로 아래에 배치해 서로 겹치지 않게 한다.
            root.anchoredPosition = new Vector2(0f, -240f);
            root.sizeDelta = new Vector2(820f, BAR_HEIGHT_NAME_ONLY);

            var background = gameObject.AddComponent<Image>();
            background.color = new Color(0.05f, 0.05f, 0.08f, 0.78f);
            background.raycastTarget = false;

            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            var textGo = new GameObject("NameText", typeof(RectTransform));
            textGo.layer = gameObject.layer;
            var textRect = (RectTransform)textGo.transform;
            textRect.SetParent(root, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = new Vector2(-32f, 0f);

            _nameText = textGo.AddComponent<TextMeshProUGUI>();
            _nameText.fontSize = 46f;
            // 날씨 알림은 이름 + 효과 두 줄이라 길어질 수 있다 — 넘치면 줄여서 바 안에 맞춘다.
            _nameText.enableAutoSizing = true;
            _nameText.fontSizeMin = 24f;
            _nameText.fontSizeMax = 46f;
            _nameText.fontWeight = FontWeight.Bold;
            _nameText.color = Color.white;
            _nameText.alignment = TextAlignmentOptions.Center;
            _nameText.margin = new Vector4(TEXT_SIDE_MARGIN, 0f, TEXT_SIDE_MARGIN, 0f);
            _nameText.raycastTarget = false;

            // 날씨 아이콘 — 글자와 같은 좌표계를 쓰도록 텍스트 아래에 두고, 위치는 표시할 때 첫 줄에 맞춘다(_ApplyEventIcon).
            var iconGo = new GameObject("EventIcon", typeof(RectTransform));
            iconGo.layer = gameObject.layer;
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.SetParent(textRect, false);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(EVENT_ICON_SIZE, EVENT_ICON_SIZE);

            _eventIcon = iconGo.AddComponent<Image>();
            _eventIcon.preserveAspect = true;
            _eventIcon.raycastTarget = false;
            iconGo.SetActive(false);

            // 언어별 폰트(라틴/아랍/태국 등) 교체 + RTL 방향 처리. 한국어는 기본 폰트(DNFBitBitv2) 유지.
            LocalizeFontSwitcher.Register(_nameText);
        }
    }
}
