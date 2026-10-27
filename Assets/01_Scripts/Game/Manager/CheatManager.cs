using System.Collections.Generic;
using Cumic.Events;
using TrainDefense.Game.Datas;
using TrainDefense.Game.Events;
using TrainDefense.Game.TrackEvent;
using UnityEngine;

namespace TrainDefense.Game.Manager
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public class CheatManager : MonoBehaviour
    {
        #region Variables

        // 선로 이벤트 치트 패널 레이아웃 (1080p 기준 — 화면 높이에 맞춰 GUI.matrix로 확대/축소한다)
        private const float REFERENCE_SCREEN_HEIGHT = 1080f;
        private const float PANEL_X = 20f;
        private const float PANEL_Y = 170f;
        private const float PANEL_WIDTH = 560f;
        private const float ROW_HEIGHT = 44f;
        private const float ROW_SPACING = 4f;
        private const int FONT_SIZE = 22;

        #endregion

        #region Fields

        // 패널이 덮고 있는 화면 영역(픽셀, 좌하단 원점). 패널 버튼 탭이 플레이어 포격으로 새지 않게 PlayerBombardManager가 묻는다.
        private static Rect _panelScreenRect;
        private static bool _isPanelVisible;

        private bool _isTrackEventPanelOpen;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;

        #endregion

        #region LifeCycle

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
                _AddMaxCoin();

            if (Input.GetKeyDown(KeyCode.Space))
                TimeManager.Instance?.SetFastForward(true);
            else if (Input.GetKeyUp(KeyCode.Space))
                TimeManager.Instance?.SetFastForward(false);

            // 다음 레벨까지 남은 경험치를 넣어 한 번 레벨업시킨다(카드는 역 도착 때 뜬다)
            if (Input.GetKeyDown(KeyCode.F) && UserDataManager.Instance != null)
                GameEventSystem.Publish(new AddExpEvent(Mathf.CeilToInt(UserDataManager.Instance.GetNextLevelUpExp() - UserDataManager.Instance.CurrentExp)));

            if (Input.GetKeyDown(KeyCode.M))
                StageManager.Instance?.ForceMapSelection();

            // 선로 이벤트 치트 패널 열기/닫기
            if (Input.GetKeyDown(KeyCode.T))
                _isTrackEventPanelOpen = !_isTrackEventPanelOpen;
        }

        private void OnDisable()
        {
            _isPanelVisible = false;
        }

        private void OnGUI()
        {
            _isPanelVisible = false;

            if (!_isTrackEventPanelOpen)
                return;

            TrackEventManager trackEventManager = TrackEventManager.Instance;

            if (trackEventManager == null)
                return;

            _EnsureStyles();

            float scale = Screen.height / REFERENCE_SCREEN_HEIGHT;
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            _DrawTrackEventPanel(trackEventManager);

            GUI.matrix = previousMatrix;
        }

        #endregion

        /// <summary>이 화면 좌표(픽셀, 좌하단 원점)가 열린 치트 패널 위인지.</summary>
        public static bool IsPointerOverPanel(Vector2 screenPosition)
        {
            return _isPanelVisible && _panelScreenRect.Contains(screenPosition);
        }

        private void _AddMaxCoin()
        {
            int current = UserDataManager.Instance.Coin;
            GameEventSystem.Publish(new ChangeCoinUIEvent(current, 999999999));
        }

        // 제목 + 상태 3줄(진행 중 이벤트·남은 시간·배율) + 종료 버튼 + 등록된 이벤트별 발동 버튼.
        private void _DrawTrackEventPanel(TrackEventManager trackEventManager)
        {
            IReadOnlyList<TrackEventData> datas = trackEventManager.EventDatas;
            int dataCount = datas != null ? datas.Count : 0;
            int rowCount = 5 + dataCount;
            float y = PANEL_Y;

            Rect panelRect = new Rect(PANEL_X - 10f, PANEL_Y - 10f, PANEL_WIDTH + 20f, rowCount * (ROW_HEIGHT + ROW_SPACING) + 20f);
            GUI.Box(panelRect, GUIContent.none);
            _CachePanelScreenRect(panelRect);

            GUI.Label(_NextRow(ref y), "[T] 선로 이벤트 치트 (버튼 = 즉시 발동)", _labelStyle);
            GUI.Label(_NextRow(ref y), _GetActiveEventText(trackEventManager), _labelStyle);
            GUI.Label(_NextRow(ref y),
                      $"골드 x{TrackEventModifiers.KillGoldMultiplier:0.##}  경험치 x{TrackEventModifiers.KillExpMultiplier:0.##}  이속 x{TrackEventModifiers.MonsterMoveSpeedMultiplier:0.##}",
                      _labelStyle);
            GUI.Label(_NextRow(ref y),
                      $"포탑 공속 x{TrackEventModifiers.TurretAttackSpeedMultiplier:0.##}  사거리 x{TrackEventModifiers.TurretRangeMultiplier:0.##}",
                      _labelStyle);

            GUI.enabled = trackEventManager.IsEventActive;

            if (GUI.Button(_NextRow(ref y), "진행 중 이벤트 종료", _buttonStyle))
                trackEventManager.ForceEndEvent();

            GUI.enabled = true;

            for (int i = 0; i < dataCount; i++)
            {
                TrackEventData data = datas[i];

                if (data == null)
                    continue;

                string durationText = data.IsUntilStation ? "역까지" : $"{data.Duration:0.#}초";

                if (GUI.Button(_NextRow(ref y), $"{data.Id}  ({durationText} · x{data.Magnitude:0.##})", _buttonStyle))
                    trackEventManager.ForceStartEvent(data);
            }
        }

        private string _GetActiveEventText(TrackEventManager trackEventManager)
        {
            TrackEventData activeData = trackEventManager.ActiveData;

            if (activeData == null)
                return "진행 중: 없음";

            if (activeData.IsUntilStation)
                return $"진행 중: {activeData.Id} (역까지)";

            float remainingTime = Mathf.Max(0f, activeData.Duration - trackEventManager.ActiveElapsedTime);

            return $"진행 중: {activeData.Id} ({remainingTime:0.0}초 남음)";
        }

        // GUI 좌표(좌상단 원점, 1080p 기준)를 입력 좌표(좌하단 원점, 실제 픽셀)로 바꿔 둔다.
        private void _CachePanelScreenRect(Rect guiRect)
        {
            float scale = Screen.height / REFERENCE_SCREEN_HEIGHT;
            float height = guiRect.height * scale;
            _panelScreenRect = new Rect(guiRect.x * scale, Screen.height - guiRect.y * scale - height, guiRect.width * scale, height);
            _isPanelVisible = true;
        }

        private Rect _NextRow(ref float y)
        {
            Rect rect = new Rect(PANEL_X, y, PANEL_WIDTH, ROW_HEIGHT);
            y += ROW_HEIGHT + ROW_SPACING;

            return rect;
        }

        private void _EnsureStyles()
        {
            if (_labelStyle != null)
                return;

            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = FONT_SIZE, alignment = TextAnchor.MiddleLeft };
            _labelStyle.normal.textColor = Color.white;
            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = FONT_SIZE, alignment = TextAnchor.MiddleLeft };
        }
    }
#endif
}
