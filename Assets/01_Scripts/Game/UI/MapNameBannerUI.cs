using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cumic.Events;
using Cumic.Sequence;
using TrainDefense.Game.Manager;
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
        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _nameText;
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
        }

        private void _UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<GameEnterEvent>(_OnGameEnter);
            GameEventSystem.Unsubscribe<EngageStartEvent>(_OnEngageStart);
        }
        #endregion

        private void _OnGameEnter(GameEnterEvent e)
        {
            // 로비 배경 시뮬레이션(LobbyGameSimulation)도 GameEnterEvent를 발행하므로 로비 분은 무시한다.
            if (e == null || e.IsLobby)
                return;

            // 새 판 시작 — 직전 판에서 보여준 맵 기록만 지운다. 실제 표시는 첫 삼중택일이
            // 끝나고 발행되는 EngageStartEvent(InGameSequence._OnTriChoiceSelect)에서 한다.
            _lastShownStageId = null;
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

            if (_playRoutine != null)
                StopCoroutine(_playRoutine);

            _playRoutine = StartCoroutine(_PlayRoutine(mapName));
        }

        private IEnumerator _PlayRoutine(string mapName)
        {
            // 튜토리얼/삼중택일(레벨업) 오버레이가 떠 있는 동안은 대기해 연출을 가리지 않는다.
            // 걷힌 뒤에도 overlayClearSettle만큼 연속으로 비어 있어야 표시한다.
            float clearTime = 0f;
            while (clearTime < overlayClearSettle)
            {
                clearTime = _IsBlocked() ? 0f : clearTime + Time.unscaledDeltaTime;
                yield return null;
            }

            if (_nameText != null)
                _nameText.text = mapName;

            yield return _Fade(0f, 1f, fadeDuration);
            yield return new WaitForSecondsRealtime(holdDuration);
            yield return _Fade(1f, 0f, fadeDuration);

            _playRoutine = null;
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
            root.sizeDelta = new Vector2(820f, 110f);

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
            _nameText.fontWeight = FontWeight.Bold;
            _nameText.color = Color.white;
            _nameText.alignment = TextAlignmentOptions.Center;
            _nameText.raycastTarget = false;

            // 언어별 폰트(라틴/아랍/태국 등) 교체 + RTL 방향 처리. 한국어는 기본 폰트(DNFBitBitv2) 유지.
            LocalizeFontSwitcher.Register(_nameText);
        }
    }
}
