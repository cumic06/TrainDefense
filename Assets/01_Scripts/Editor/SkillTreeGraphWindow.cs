#if UNITY_EDITOR
using System.Collections.Generic;
using TrainDefense.Game.Datas;
using UnityEditor;
using UnityEngine;

namespace TrainDefense.Editor
{
    /// <summary>
    /// 스킬트리 노드 그래프 에디터 (Tools > SkillTree Graph Editor).
    /// DB.asset의 skillNodeDataList를 (lane, row, col) 그리드 위에 노드·레일로 시각화한다.
    /// - 좌클릭: 선택 / 드래그: 그리드 스냅 이동 (lane·row·col 갱신)
    /// - Ctrl+클릭: 클릭한 노드를 선택 노드의 선행(prerequisite)으로 연결/해제 (순환 연결 차단)
    /// - 우클릭(노드): 위에 연결된 새 노드 추가 · 선행 해제 · 삭제 / 우클릭(빈 칸): 노드 추가
    /// 모든 편집은 SerializedProperty를 통해 이뤄져 Undo·에셋 저장이 Unity 표준으로 동작한다.
    /// </summary>
    public class SkillTreeGraphWindow : EditorWindow
    {
        #region Consts
        private const string DB_ASSET_PATH = "Assets/Resources/Data/DB.asset";

        private const float TOOLBAR_HEIGHT = 22f;
        private const float HELP_HEIGHT = 18f;
        private const float INSPECTOR_WIDTH = 330f;

        private const float CELL_WIDTH = 150f;
        private const float CELL_HEIGHT = 78f;
        private const float CELL_PITCH_X = 168f;
        private const float CELL_PITCH_Y = 108f;
        private const int COLS_PER_LANE = 3;      // col = -1, 0, +1
        private const float LANE_GAP = 36f;
        private const float CANVAS_MARGIN = 34f;
        private const float LANE_HEADER_HEIGHT = 30f;
        private const int MIN_ROWS = 8;
        private const int EXTRA_ROWS = 2;         // 최상단 위 여유 칸 (확장 드롭용)

        private static readonly int LaneCount = System.Enum.GetValues(typeof(SkillTreeLane)).Length;
        private static readonly string[] LaneNames = { "화력 (Firepower)", "방어 (Defense)", "유틸 (Utility)" };
        private static readonly Color[] LaneColors =
        {
            new Color(0.85f, 0.35f, 0.30f),
            new Color(0.30f, 0.55f, 0.90f),
            new Color(0.35f, 0.75f, 0.45f),
        };
        private static readonly Color[] CategoryColors =
        {
            new Color(0.95f, 0.60f, 0.20f),   // TurretStat
            new Color(0.35f, 0.60f, 0.95f),   // Passive
            new Color(0.95f, 0.80f, 0.25f),   // TurretUnlock
        };
        private static readonly string[] CategoryShortNames = { "스탯", "패시브", "포탑 해금" };
        #endregion

        #region Variables
        private DB _db;
        private SerializedObject _serializedDb;
        private SerializedProperty _nodeList;

        private Vector2 _canvasScroll;
        private Vector2 _inspectorScroll;
        private int _selectedIndex = -1;
        private int _draggingIndex = -1;
        private bool _dragMoved;
        private Vector2 _dragOffset;
        private Vector2 _dragPosition;

        private static GUIStyle _idStyle;
        private static GUIStyle _infoStyle;
        private static GUIStyle _costStyle;
        private static GUIStyle _laneHeaderStyle;
        private static GUIStyle _rowLabelStyle;
        private static GUIStyle[] _nodeStyles;      // 카테고리별 내장 "flow node" 스타일 (둥근 모서리)
        private static GUIStyle[] _nodeStylesOn;    // 선택 글로우 변형
        #endregion

        #region LifeCycle
        [MenuItem("Tools/SkillTree Graph Editor")]
        public static void Open()
        {
            var window = GetWindow<SkillTreeGraphWindow>("스킬트리 그래프");
            window.minSize = new Vector2(960f, 540f);
            window.Show();
        }

        private void OnEnable()
        {
            _LoadDb();
            Undo.undoRedoPerformed += Repaint;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
        }

        private void OnGUI()
        {
            _EnsureStyles();

            if (_db == null || _serializedDb == null) _LoadDb();

            if (_db == null)
            {
                EditorGUILayout.HelpBox($"DB 에셋을 찾을 수 없습니다: {DB_ASSET_PATH}", MessageType.Error);
                _db = (DB)EditorGUILayout.ObjectField("DB 에셋", _db, typeof(DB), false);

                if (_db != null) _LoadDb();

                return;
            }

            _serializedDb.Update();

            _DrawToolbar();

            float top = TOOLBAR_HEIGHT + HELP_HEIGHT;
            var canvasRect = new Rect(0f, top, position.width - INSPECTOR_WIDTH, position.height - top);
            var inspectorRect = new Rect(position.width - INSPECTOR_WIDTH, top, INSPECTOR_WIDTH, position.height - top);

            _DrawCanvas(canvasRect);
            _DrawInspector(inspectorRect);

            _serializedDb.ApplyModifiedProperties();
        }
        #endregion

        #region Load & Styles
        private void _LoadDb()
        {
            if (_db == null) _db = AssetDatabase.LoadAssetAtPath<DB>(DB_ASSET_PATH);

            if (_db == null) return;

            _serializedDb = new SerializedObject(_db);
            _nodeList = _serializedDb.FindProperty("skillNodeDataList");
        }

        private static void _EnsureStyles()
        {
            if (_idStyle != null) return;

            _idStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                wordWrap = true,
                clipping = TextClipping.Clip,
            };
            _idStyle.normal.textColor = Color.white;

            _infoStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
            };
            _infoStyle.normal.textColor = new Color(0.82f, 0.82f, 0.85f);

            _costStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
            };
            _costStyle.normal.textColor = new Color(0.6f, 0.6f, 0.64f);

            _laneHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
            };

            _rowLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
            };
            _rowLabelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.35f);

            // Unity 내장 노드 스타일 (둥근 모서리 + 선택 글로우). 카테고리: 스탯=주황(6), 패시브=파랑(1), 해금=노랑(4)
            _nodeStyles = new[]
            {
                GUI.skin.FindStyle("flow node 6"),
                GUI.skin.FindStyle("flow node 1"),
                GUI.skin.FindStyle("flow node 4"),
            };
            _nodeStylesOn = new[]
            {
                GUI.skin.FindStyle("flow node 6 on"),
                GUI.skin.FindStyle("flow node 1 on"),
                GUI.skin.FindStyle("flow node 4 on"),
            };
        }
        #endregion

        #region Toolbar
        private void _DrawToolbar()
        {
            var toolbarRect = new Rect(0f, 0f, position.width, TOOLBAR_HEIGHT);
            GUILayout.BeginArea(toolbarRect, EditorStyles.toolbar);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"노드 {_nodeList.arraySize}개", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("DB 선택", EditorStyles.toolbarButton))
                EditorGUIUtility.PingObject(_db);

            if (GUILayout.Button("에셋 저장", EditorStyles.toolbarButton))
                AssetDatabase.SaveAssetIfDirty(_db);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            var helpRect = new Rect(4f, TOOLBAR_HEIGHT, position.width - 8f, HELP_HEIGHT);
            GUI.Label(helpRect,
                "좌클릭: 선택 · 드래그: 이동(그리드 스냅) | Ctrl+클릭: 클릭한 노드를 선택 노드의 선행으로 연결/해제 | 우클릭: 추가/삭제 | Del: 선택 노드 삭제",
                EditorStyles.miniLabel);
        }
        #endregion

        #region Canvas
        private void _DrawCanvas(Rect viewRect)
        {
            IReadOnlyList<SkillNodeData> nodes = _db.SkillNodeDataList;
            int totalRows = _GetTotalRows(nodes);

            float laneWidth = COLS_PER_LANE * CELL_PITCH_X;
            float contentWidth = CANVAS_MARGIN * 2f + LaneCount * laneWidth + (LaneCount - 1) * LANE_GAP;
            float contentHeight = LANE_HEADER_HEIGHT + CANVAS_MARGIN * 2f + totalRows * CELL_PITCH_Y;
            var contentRect = new Rect(0f, 0f,
                Mathf.Max(contentWidth, viewRect.width),
                Mathf.Max(contentHeight, viewRect.height));

            _canvasScroll = GUI.BeginScrollView(viewRect, _canvasScroll, contentRect);

            _DrawGrid(totalRows, contentRect);

            Dictionary<string, int> idToIndex = _BuildIdMap(nodes);
            _DrawEdges(nodes, idToIndex, totalRows);
            _DrawNodes(nodes, totalRows);

            if (_draggingIndex >= 0 && _draggingIndex < nodes.Count)
                _DrawDragPreview(nodes, totalRows);

            _HandleCanvasInput(nodes, totalRows);

            GUI.EndScrollView();
        }

        private void _DrawGrid(int totalRows, Rect contentRect)
        {
            EditorGUI.DrawRect(contentRect, EditorGUIUtility.isProSkin
                ? new Color(0.13f, 0.13f, 0.15f)
                : new Color(0.72f, 0.72f, 0.75f));

            for (int lane = 0; lane < LaneCount; lane++)
            {
                float laneOriginX = _GetLaneOriginX(lane);
                float laneWidth = COLS_PER_LANE * CELL_PITCH_X;
                Color laneColor = LaneColors[Mathf.Min(lane, LaneColors.Length - 1)];
                var laneRect = new Rect(laneOriginX, LANE_HEADER_HEIGHT, laneWidth, contentRect.height - LANE_HEADER_HEIGHT - 8f);

                Color fill = laneColor;
                fill.a = 0.05f;
                EditorGUI.DrawRect(laneRect, fill);

                Color border = laneColor;
                border.a = 0.2f;
                EditorGUI.DrawRect(new Rect(laneRect.x, laneRect.y, 1f, laneRect.height), border);
                EditorGUI.DrawRect(new Rect(laneRect.xMax - 1f, laneRect.y, 1f, laneRect.height), border);

                string laneName = lane < LaneNames.Length ? LaneNames[lane] : $"Lane {lane}";
                var headerRect = new Rect(laneOriginX + 10f, 4f, laneWidth - 20f, LANE_HEADER_HEIGHT - 10f);
                _laneHeaderStyle.normal.textColor = Color.Lerp(laneColor, Color.white, 0.4f);
                GUI.Label(headerRect, laneName, _laneHeaderStyle);

                Color underline = laneColor;
                underline.a = 0.85f;
                EditorGUI.DrawRect(new Rect(laneOriginX, LANE_HEADER_HEIGHT - 3f, laneWidth, 2f), underline);

                for (int row = 0; row < totalRows; row++)
                {
                    float y = _GetRowCenterY(row, totalRows) + CELL_PITCH_Y * 0.5f;
                    EditorGUI.DrawRect(new Rect(laneRect.x + 8f, y, laneRect.width - 16f, 1f), new Color(1f, 1f, 1f, 0.03f));
                }
            }

            for (int row = 0; row < totalRows; row++)
            {
                float y = _GetRowCenterY(row, totalRows);
                var labelRect = new Rect(0f, y - 8f, CANVAS_MARGIN - 8f, 16f);
                GUI.Label(labelRect, row == 0 ? "출발" : row.ToString(), _rowLabelStyle);
            }
        }

        private void _DrawEdges(IReadOnlyList<SkillNodeData> nodes, Dictionary<string, int> idToIndex, int totalRows)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                string[] prerequisites = nodes[i].Prerequisites;

                if (prerequisites == null) continue;

                Rect nodeRect = _GetNodeRect(nodes[i], totalRows, i);

                foreach (string prereqId in prerequisites)
                {
                    if (string.IsNullOrEmpty(prereqId)) continue;
                    if (!idToIndex.TryGetValue(prereqId, out int prereqIndex)) continue;

                    Rect prereqRect = _GetNodeRect(nodes[prereqIndex], totalRows, prereqIndex);
                    var start = new Vector2(prereqRect.center.x, prereqRect.yMin);
                    var end = new Vector2(nodeRect.center.x, nodeRect.yMax);

                    bool involvesSelected = i == _selectedIndex || prereqIndex == _selectedIndex;
                    int laneIndex = Mathf.Clamp((int)nodes[i].Lane, 0, LaneColors.Length - 1);
                    Color edgeColor = involvesSelected
                        ? new Color(1f, 0.85f, 0.3f)
                        : Color.Lerp(LaneColors[laneIndex], Color.white, 0.2f) * new Color(1f, 1f, 1f, 0.7f);

                    Vector2 startTangent = start + Vector2.down * 45f;
                    Vector2 endTangent = end + Vector2.up * 45f;

                    // 어두운 밑선 + 색 윗선 이중 렌더로 선명한 레일 느낌
                    Handles.DrawBezier(start, end, startTangent, endTangent,
                        new Color(0f, 0f, 0f, 0.35f), null, involvesSelected ? 6f : 4.5f);
                    Handles.DrawBezier(start, end, startTangent, endTangent,
                        edgeColor, null, involvesSelected ? 3.5f : 2f);

                    Color previousHandlesColor = Handles.color;
                    Handles.color = edgeColor;
                    Handles.DrawSolidDisc(start, Vector3.forward, 3f);
                    Handles.DrawSolidDisc(end, Vector3.forward, 3f);
                    Handles.color = previousHandlesColor;
                }
            }
        }

        private void _DrawNodes(IReadOnlyList<SkillNodeData> nodes, int totalRows)
        {
            var issues = _CollectIssueIndices(nodes);

            for (int i = 0; i < nodes.Count; i++)
            {
                Rect rect = _GetNodeRect(nodes[i], totalRows, i);
                bool isDragging = i == _draggingIndex && _dragMoved;

                _DrawNodeBox(nodes[i], rect, i == _selectedIndex, issues.Contains(i), isDragging ? 0.35f : 1f);
            }
        }

        private void _DrawNodeBox(SkillNodeData node, Rect rect, bool selected, bool hasIssue, float alpha)
        {
            int category = Mathf.Clamp((int)node.Category, 0, CategoryColors.Length - 1);
            GUIStyle nodeStyle = selected ? _nodeStylesOn[category] : _nodeStyles[category];

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);

            if (nodeStyle != null)
            {
                GUI.Box(rect, GUIContent.none, nodeStyle);
            }
            else
            {
                // 내장 flow node 스타일이 없는 환경 폴백 — 각진 박스 + 카테고리 스트립
                EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.18f, alpha));
                Color strip = CategoryColors[category];
                strip.a = alpha;
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 4f), strip);

                if (selected)
                    EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), new Color(1f, 0.85f, 0.3f, alpha));
            }

            if (hasIssue)
                EditorGUI.DrawRect(new Rect(rect.xMax - 17f, rect.y + 9f, 8f, 8f), new Color(0.95f, 0.25f, 0.25f, alpha));

            string id = node.Id ?? "";
            string title = id.StartsWith("skill_") ? id.Substring("skill_".Length) : id;
            var titleRect = new Rect(rect.x + 10f, rect.y + 9f, rect.width - 20f, 26f);
            GUI.Label(titleRect, new GUIContent(title, id), _idStyle);

            string categoryName = CategoryShortNames[Mathf.Clamp(category, 0, CategoryShortNames.Length - 1)];
            string detail = node.Category switch
            {
                SkillNodeCategory.TurretStat => $"{categoryName} {(node.Stats?.Length ?? 0)}개",
                SkillNodeCategory.Passive => $"{categoryName} · {node.PassiveType} +{node.PassiveValuePerLevel}",
                SkillNodeCategory.TurretUnlock => $"{categoryName} · {node.UnlockTrainId}",
                _ => categoryName,
            };
            GUI.Label(new Rect(rect.x + 10f, rect.y + 37f, rect.width - 20f, 14f), detail, _infoStyle);

            string cost = node.MaxLevel > 1 ? $"비용 {node.NeedPoint} · 최대 Lv {node.MaxLevel}" : $"비용 {node.NeedPoint}";
            GUI.Label(new Rect(rect.x + 10f, rect.y + 53f, rect.width - 20f, 13f), cost, _costStyle);

            GUI.color = previousColor;
        }

        private void _DrawDragPreview(IReadOnlyList<SkillNodeData> nodes, int totalRows)
        {
            if (!_dragMoved) return;

            var dragRect = new Rect(_dragPosition.x, _dragPosition.y, CELL_WIDTH, CELL_HEIGHT);

            if (_TryGetCellAt(dragRect.center, totalRows, out int lane, out int row, out int col))
            {
                bool occupied = _IsCellOccupied(nodes, lane, row, col, _draggingIndex);
                Rect cellRect = _GetCellRect(lane, row, col, totalRows);
                Color highlight = occupied ? new Color(1f, 0.3f, 0.3f, 0.25f) : new Color(0.3f, 1f, 0.5f, 0.25f);
                EditorGUI.DrawRect(cellRect, highlight);
            }

            _DrawNodeBox(nodes[_draggingIndex], dragRect, true, false, 0.8f);
        }
        #endregion

        #region Input
        private void _HandleCanvasInput(IReadOnlyList<SkillNodeData> nodes, int totalRows)
        {
            Event current = Event.current;

            switch (current.type)
            {
                case EventType.MouseDown:
                    _OnMouseDown(current, nodes, totalRows);
                    break;

                case EventType.MouseDrag:
                    if (_draggingIndex >= 0 && current.button == 0)
                    {
                        _dragMoved = true;
                        _dragPosition = current.mousePosition - _dragOffset;
                        current.Use();
                        Repaint();
                    }
                    break;

                case EventType.MouseUp:
                    if (_draggingIndex >= 0 && current.button == 0)
                    {
                        if (_dragMoved) _DropNode(nodes, totalRows);

                        _draggingIndex = -1;
                        _dragMoved = false;
                        current.Use();
                        Repaint();
                    }
                    break;

                case EventType.KeyDown:
                    if (current.keyCode == KeyCode.Delete && _selectedIndex >= 0)
                    {
                        int target = _selectedIndex;
                        current.Use();
                        _DeleteNode(target);
                        GUIUtility.ExitGUI();
                    }
                    break;
            }
        }

        private void _OnMouseDown(Event current, IReadOnlyList<SkillNodeData> nodes, int totalRows)
        {
            int hit = _HitTestNode(nodes, totalRows, current.mousePosition);

            if (current.button == 0)
            {
                if (hit >= 0)
                {
                    bool connectModifier = current.control || current.command;

                    if (connectModifier && _selectedIndex >= 0 && hit != _selectedIndex)
                    {
                        _TogglePrerequisite(_selectedIndex, hit);
                    }
                    else
                    {
                        _selectedIndex = hit;
                        _draggingIndex = hit;
                        _dragMoved = false;
                        Rect rect = _GetNodeRect(nodes[hit], totalRows, hit);
                        _dragOffset = current.mousePosition - rect.position;
                        _dragPosition = rect.position;
                    }

                    GUI.FocusControl(null);
                    current.Use();
                    Repaint();
                }
                else
                {
                    _selectedIndex = -1;
                    _draggingIndex = -1;   // 창 밖에서 마우스를 놓아 남은 드래그 상태 정리
                    _dragMoved = false;
                    GUI.FocusControl(null);
                    current.Use();
                    Repaint();
                }
            }
            else if (current.button == 1)
            {
                if (hit >= 0)
                {
                    _ShowNodeContextMenu(hit, nodes, totalRows);
                    current.Use();
                }
                else if (_TryGetCellAt(current.mousePosition, totalRows, out int lane, out int row, out int col))
                {
                    _ShowEmptyCellContextMenu(nodes, lane, row, col);
                    current.Use();
                }
            }
        }

        private void _ShowNodeContextMenu(int index, IReadOnlyList<SkillNodeData> nodes, int totalRows)
        {
            var menu = new GenericMenu();
            SkillNodeData node = nodes[index];
            int lane = (int)node.Lane;
            int row = node.Row;
            int col = node.Col;
            string nodeId = node.Id;

            bool upperFree = !_IsCellOccupied(nodes, lane, row + 1, col, -1);

            if (upperFree)
                menu.AddItem(new GUIContent("위에 연결된 새 노드 추가"), false,
                    () => _WithSerialized(() => _AddNode(lane, row + 1, col, nodeId)));
            else
                menu.AddDisabledItem(new GUIContent("위에 연결된 새 노드 추가 (칸 사용 중)"));

            menu.AddItem(new GUIContent("선행 연결 모두 해제"), false,
                () => _WithSerialized(() => _ClearPrerequisites(index)));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("노드 삭제"), false,
                () => _WithSerialized(() => _DeleteNodeInternal(index)));
            menu.ShowAsContext();
        }

        private void _ShowEmptyCellContextMenu(IReadOnlyList<SkillNodeData> nodes, int lane, int row, int col)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent($"여기에 노드 추가 ({(SkillTreeLane)lane} · row {row} · col {col})"), false,
                () => _WithSerialized(() => _AddNode(lane, row, col, null)));
            menu.ShowAsContext();
        }
        #endregion

        #region Edit Operations
        /// <summary>GenericMenu 콜백은 OnGUI 사이클 밖에서 실행되므로 Update/Apply로 감싼다.</summary>
        private void _WithSerialized(System.Action action)
        {
            if (_serializedDb == null) return;

            _serializedDb.Update();
            action();
            _serializedDb.ApplyModifiedProperties();
            Repaint();
        }

        private void _DropNode(IReadOnlyList<SkillNodeData> nodes, int totalRows)
        {
            var center = new Vector2(_dragPosition.x + CELL_WIDTH * 0.5f, _dragPosition.y + CELL_HEIGHT * 0.5f);

            if (!_TryGetCellAt(center, totalRows, out int lane, out int row, out int col))
            {
                ShowNotification(new GUIContent("그리드 밖에는 놓을 수 없습니다"));

                return;
            }

            if (_IsCellOccupied(nodes, lane, row, col, _draggingIndex))
            {
                ShowNotification(new GUIContent("이미 노드가 있는 칸입니다"));

                return;
            }

            SerializedProperty element = _nodeList.GetArrayElementAtIndex(_draggingIndex);
            element.FindPropertyRelative("lane").enumValueIndex = lane;
            element.FindPropertyRelative("row").intValue = row;
            element.FindPropertyRelative("col").intValue = col;
        }

        private void _TogglePrerequisite(int ownerIndex, int targetIndex)
        {
            IReadOnlyList<SkillNodeData> nodes = _db.SkillNodeDataList;
            string targetId = nodes[targetIndex].Id;

            SerializedProperty prerequisites = _nodeList.GetArrayElementAtIndex(ownerIndex)
                .FindPropertyRelative("prerequisites");

            for (int i = 0; i < prerequisites.arraySize; i++)
            {
                if (prerequisites.GetArrayElementAtIndex(i).stringValue != targetId) continue;

                prerequisites.DeleteArrayElementAtIndex(i);

                return;
            }

            if (_WouldCreateCycle(nodes, nodes[ownerIndex].Id, targetId))
            {
                ShowNotification(new GUIContent("순환 연결은 만들 수 없습니다"));

                return;
            }

            prerequisites.arraySize++;
            prerequisites.GetArrayElementAtIndex(prerequisites.arraySize - 1).stringValue = targetId;
        }

        private void _ClearPrerequisites(int index)
        {
            _nodeList.GetArrayElementAtIndex(index).FindPropertyRelative("prerequisites").arraySize = 0;
        }

        private void _AddNode(int lane, int row, int col, string prerequisiteId)
        {
            string id = _GenerateUniqueId();
            int index = _nodeList.arraySize;
            _nodeList.arraySize++;

            SerializedProperty element = _nodeList.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("id").stringValue = id;
            element.FindPropertyRelative("iconId").stringValue = "";
            element.FindPropertyRelative("name").stringValue = $"SkillTree_{id}_Name";
            element.FindPropertyRelative("description").stringValue = $"SkillTree_{id}_Desc";
            element.FindPropertyRelative("lane").enumValueIndex = lane;
            element.FindPropertyRelative("row").intValue = row;
            element.FindPropertyRelative("col").intValue = col;
            element.FindPropertyRelative("needPoint").intValue = 1;
            element.FindPropertyRelative("maxLevel").intValue = 1;
            element.FindPropertyRelative("growthRate").floatValue = 1.5f;
            element.FindPropertyRelative("category").enumValueIndex = (int)SkillNodeCategory.TurretStat;
            element.FindPropertyRelative("stats").arraySize = 0;
            element.FindPropertyRelative("passiveType").enumValueIndex = (int)SkillTreePassiveType.MaxHp;
            element.FindPropertyRelative("passiveValuePerLevel").floatValue = 0f;
            element.FindPropertyRelative("unlockTrainId").stringValue = "";

            SerializedProperty prerequisites = element.FindPropertyRelative("prerequisites");
            prerequisites.arraySize = string.IsNullOrEmpty(prerequisiteId) ? 0 : 1;

            if (!string.IsNullOrEmpty(prerequisiteId))
                prerequisites.GetArrayElementAtIndex(0).stringValue = prerequisiteId;

            _selectedIndex = index;
        }

        /// <summary>OnGUI 안에서 부르는 삭제 — 즉시 Apply 후 ExitGUI로 stale 프레임을 끊는다.</summary>
        private void _DeleteNode(int index)
        {
            _DeleteNodeInternal(index);
            _serializedDb.ApplyModifiedProperties();
            _serializedDb.Update();
            Repaint();
        }

        private void _DeleteNodeInternal(int index)
        {
            if (index < 0 || index >= _nodeList.arraySize) return;

            string deletedId = _nodeList.GetArrayElementAtIndex(index)
                .FindPropertyRelative("id").stringValue;

            for (int i = 0; i < _nodeList.arraySize; i++)
            {
                if (i == index) continue;

                SerializedProperty prerequisites = _nodeList.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("prerequisites");

                for (int p = prerequisites.arraySize - 1; p >= 0; p--)
                {
                    if (prerequisites.GetArrayElementAtIndex(p).stringValue == deletedId)
                        prerequisites.DeleteArrayElementAtIndex(p);
                }
            }

            _nodeList.DeleteArrayElementAtIndex(index);
            _selectedIndex = -1;
            _draggingIndex = -1;
        }

        /// <summary>id 변경 시 다른 노드들의 prerequisites 참조를 함께 갱신한다.</summary>
        private void _UpdatePrerequisiteReferences(string oldId, string newId)
        {
            for (int i = 0; i < _nodeList.arraySize; i++)
            {
                SerializedProperty prerequisites = _nodeList.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("prerequisites");

                for (int p = 0; p < prerequisites.arraySize; p++)
                {
                    SerializedProperty entry = prerequisites.GetArrayElementAtIndex(p);

                    if (entry.stringValue == oldId) entry.stringValue = newId;
                }
            }
        }
        #endregion

        #region Inspector Panel
        private void _DrawInspector(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.12f));
            GUILayout.BeginArea(new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, rect.height - 12f));
            _inspectorScroll = GUILayout.BeginScrollView(_inspectorScroll);

            List<string> issues = _CollectIssueMessages(_db.SkillNodeDataList);

            if (issues.Count > 0)
                EditorGUILayout.HelpBox(string.Join("\n", issues), MessageType.Warning);

            if (_selectedIndex < 0 || _selectedIndex >= _nodeList.arraySize)
            {
                EditorGUILayout.HelpBox(
                    "노드를 클릭해 선택하세요.\n\n" +
                    "· 드래그: 그리드 스냅 이동\n" +
                    "· Ctrl+클릭: 클릭한 노드를 선택 노드의 선행으로 연결/해제\n" +
                    "· 우클릭(노드): 위에 연결된 새 노드 추가 / 삭제\n" +
                    "· 우클릭(빈 칸): 노드 추가\n" +
                    "· Del: 선택 노드 삭제",
                    MessageType.Info);
            }
            else
            {
                _DrawSelectedNodeFields();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void _DrawSelectedNodeFields()
        {
            SerializedProperty element = _nodeList.GetArrayElementAtIndex(_selectedIndex);

            EditorGUILayout.LabelField("선택 노드", EditorStyles.boldLabel);

            SerializedProperty idProperty = element.FindPropertyRelative("id");
            string idBefore = idProperty.stringValue;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedTextField(idProperty, new GUIContent("id"));

            if (EditorGUI.EndChangeCheck())
            {
                string idAfter = idProperty.stringValue;

                if (!string.IsNullOrEmpty(idBefore) && idBefore != idAfter)
                    _UpdatePrerequisiteReferences(idBefore, idAfter);
            }

            EditorGUILayout.PropertyField(element.FindPropertyRelative("iconId"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("name"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("description"));

            if (GUILayout.Button("이름/설명 키를 id로 재생성"))
            {
                string id = idProperty.stringValue;
                element.FindPropertyRelative("name").stringValue = $"SkillTree_{id}_Name";
                element.FindPropertyRelative("description").stringValue = $"SkillTree_{id}_Desc";
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("배치", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(element.FindPropertyRelative("lane"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("row"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("col"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("prerequisites"), true);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("비용", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(element.FindPropertyRelative("needPoint"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("maxLevel"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("growthRate"));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("효과", EditorStyles.boldLabel);
            SerializedProperty categoryProperty = element.FindPropertyRelative("category");
            EditorGUILayout.PropertyField(categoryProperty);

            switch ((SkillNodeCategory)categoryProperty.enumValueIndex)
            {
                case SkillNodeCategory.TurretStat:
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("stats"), true);
                    break;

                case SkillNodeCategory.Passive:
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("passiveType"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("passiveValuePerLevel"));
                    break;

                case SkillNodeCategory.TurretUnlock:
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("unlockTrainId"));
                    break;
            }

            EditorGUILayout.Space(10f);

            if (GUILayout.Button("이 노드 삭제"))
            {
                _DeleteNode(_selectedIndex);
                GUIUtility.ExitGUI();
            }
        }
        #endregion

        #region Helpers
        private static float _GetLaneOriginX(int lane)
        {
            return CANVAS_MARGIN + lane * (COLS_PER_LANE * CELL_PITCH_X + LANE_GAP);
        }

        private static float _GetRowCenterY(int row, int totalRows)
        {
            return LANE_HEADER_HEIGHT + CANVAS_MARGIN + (totalRows - 1 - row) * CELL_PITCH_Y + CELL_PITCH_Y * 0.5f;
        }

        private static Rect _GetCellRect(int lane, int row, int col, int totalRows)
        {
            float x = _GetLaneOriginX(lane) + (col + 1) * CELL_PITCH_X + (CELL_PITCH_X - CELL_WIDTH) * 0.5f;
            float y = _GetRowCenterY(row, totalRows) - CELL_HEIGHT * 0.5f;

            return new Rect(x, y, CELL_WIDTH, CELL_HEIGHT);
        }

        private static Rect _GetNodeRect(SkillNodeData node, int totalRows, int index)
        {
            return _GetCellRect((int)node.Lane, node.Row, node.Col, totalRows);
        }

        private static bool _TryGetCellAt(Vector2 canvasPosition, int totalRows, out int lane, out int row, out int col)
        {
            lane = -1;
            row = -1;
            col = 0;
            float laneWidth = COLS_PER_LANE * CELL_PITCH_X;

            for (int l = 0; l < LaneCount; l++)
            {
                float originX = _GetLaneOriginX(l);

                if (canvasPosition.x < originX || canvasPosition.x >= originX + laneWidth) continue;

                lane = l;
                col = Mathf.Clamp(Mathf.FloorToInt((canvasPosition.x - originX) / CELL_PITCH_X), 0, COLS_PER_LANE - 1) - 1;
                break;
            }

            if (lane < 0) return false;

            float gridTop = LANE_HEADER_HEIGHT + CANVAS_MARGIN;
            row = totalRows - 1 - Mathf.FloorToInt((canvasPosition.y - gridTop) / CELL_PITCH_Y);

            return row >= 0 && row < totalRows;
        }

        private int _HitTestNode(IReadOnlyList<SkillNodeData> nodes, int totalRows, Vector2 canvasPosition)
        {
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                if (_GetNodeRect(nodes[i], totalRows, i).Contains(canvasPosition)) return i;
            }

            return -1;
        }

        private static int _GetTotalRows(IReadOnlyList<SkillNodeData> nodes)
        {
            int maxRow = 0;

            foreach (SkillNodeData node in nodes)
                maxRow = Mathf.Max(maxRow, node.Row);

            return Mathf.Max(MIN_ROWS, maxRow + 1 + EXTRA_ROWS);
        }

        private static bool _IsCellOccupied(IReadOnlyList<SkillNodeData> nodes, int lane, int row, int col, int ignoreIndex)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (i == ignoreIndex) continue;
                if ((int)nodes[i].Lane == lane && nodes[i].Row == row && nodes[i].Col == col) return true;
            }

            return false;
        }

        private static Dictionary<string, int> _BuildIdMap(IReadOnlyList<SkillNodeData> nodes)
        {
            var map = new Dictionary<string, int>();

            for (int i = 0; i < nodes.Count; i++)
            {
                if (string.IsNullOrEmpty(nodes[i].Id)) continue;

                map.TryAdd(nodes[i].Id, i);
            }

            return map;
        }

        /// <summary>targetId를 ownerId의 선행으로 추가하면 순환이 생기는지 — targetId의 선행 체인에 ownerId가 있는지 검사.</summary>
        private static bool _WouldCreateCycle(IReadOnlyList<SkillNodeData> nodes, string ownerId, string targetId)
        {
            Dictionary<string, int> map = _BuildIdMap(nodes);
            var visited = new HashSet<string>();
            var stack = new Stack<string>();
            stack.Push(targetId);

            while (stack.Count > 0)
            {
                string currentId = stack.Pop();

                if (currentId == ownerId) return true;
                if (!visited.Add(currentId)) continue;
                if (!map.TryGetValue(currentId, out int index)) continue;

                string[] prerequisites = nodes[index].Prerequisites;

                if (prerequisites == null) continue;

                foreach (string prereqId in prerequisites)
                {
                    if (!string.IsNullOrEmpty(prereqId)) stack.Push(prereqId);
                }
            }

            return false;
        }

        private string _GenerateUniqueId()
        {
            var ids = new HashSet<string>();

            for (int i = 0; i < _nodeList.arraySize; i++)
                ids.Add(_nodeList.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue);

            int number = 1;

            while (ids.Contains($"skill_new_{number}")) number++;

            return $"skill_new_{number}";
        }

        private static HashSet<int> _CollectIssueIndices(IReadOnlyList<SkillNodeData> nodes)
        {
            var result = new HashSet<int>();
            var seen = new Dictionary<string, int>();
            Dictionary<string, int> map = _BuildIdMap(nodes);

            for (int i = 0; i < nodes.Count; i++)
            {
                string id = nodes[i].Id;

                if (string.IsNullOrEmpty(id))
                {
                    result.Add(i);
                }
                else if (seen.TryGetValue(id, out int firstIndex))
                {
                    result.Add(i);
                    result.Add(firstIndex);
                }
                else
                {
                    seen[id] = i;
                }

                string[] prerequisites = nodes[i].Prerequisites;

                if (prerequisites == null) continue;

                foreach (string prereqId in prerequisites)
                {
                    if (string.IsNullOrEmpty(prereqId) || !map.ContainsKey(prereqId) || prereqId == id)
                        result.Add(i);
                }
            }

            return result;
        }

        private static List<string> _CollectIssueMessages(IReadOnlyList<SkillNodeData> nodes)
        {
            var messages = new List<string>();
            var seen = new HashSet<string>();
            Dictionary<string, int> map = _BuildIdMap(nodes);

            foreach (SkillNodeData node in nodes)
            {
                if (string.IsNullOrEmpty(node.Id))
                {
                    messages.Add("id가 비어 있는 노드가 있습니다.");
                }
                else if (!seen.Add(node.Id))
                {
                    messages.Add($"중복 id: {node.Id}");
                }

                if (node.Prerequisites == null) continue;

                foreach (string prereqId in node.Prerequisites)
                {
                    if (string.IsNullOrEmpty(prereqId))
                        messages.Add($"{node.Id}: 빈 선행 항목이 있습니다.");
                    else if (prereqId == node.Id)
                        messages.Add($"{node.Id}: 자기 자신을 선행으로 참조합니다.");
                    else if (!map.ContainsKey(prereqId))
                        messages.Add($"{node.Id}: 선행 '{prereqId}'가 존재하지 않습니다.");
                }
            }

            return messages;
        }
        #endregion
    }
}
#endif
