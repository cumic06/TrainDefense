#if UNITY_EDITOR
using System.Collections.Generic;
using TrainDefense.Game.Datas;
using UnityEditor;
using UnityEngine;

namespace TrainDefense.Editor
{
    /// <summary>
    /// DB.asset 통합 브라우저 (Tools > DB Browser).
    /// 탭으로 데이터 종류를 이동하고, 각 탭은 좌측 목록(검색 필터) + 우측 상세(전체 필드 편집) 마스터-디테일로 보여준다.
    /// 목록형 탭은 추가(복제)/삭제를 지원하고, 스킬트리 탭에서는 그래프 에디터(SkillTreeGraphWindow)를 바로 연다.
    /// 모든 편집은 SerializedProperty를 통해 이뤄져 Undo·에셋 저장이 Unity 표준으로 동작한다.
    /// (엑셀 → DB 일괄 임포트는 기존 Tools > Database Generator 사용)
    /// </summary>
    public class DatabaseBrowserWindow : EditorWindow
    {
        #region Consts
        private const string DB_ASSET_PATH = "Assets/Resources/Data/DB.asset";
        private const float LIST_WIDTH = 280f;
        private const float TOOLBAR_HEIGHT = 22f;
        private const float ROW_HEIGHT = 32f;
        private const float THUMB_SIZE = 28f;
        private const float PREVIEW_LARGE = 110f;
        private const float PREVIEW_SMALL = 52f;
        private const int PREVIEW_MAX = 13;         // 큰 미리보기 1 + 스트립 12 (애니메이션 프레임 등)
        private const int PREVIEW_PER_ROW = 5;

        /// <summary>목록형 탭 정의 — (탭 이름, 서브 리스트(라벨, DB 프로퍼티 경로)[]). 경로는 SerializedObject.FindProperty 점 표기.</summary>
        private static readonly (string tabName, (string label, string path)[] lists)[] ListTabs =
        {
            ("몬스터", new[] { ("몬스터", "monsterDataList") }),
            ("기차", new[]
            {
                ("기본", "trainDataList"),
                ("터렛", "turretTrainDataList"),
                ("원거리", "rangeTrainDataList"),
            }),
            ("기차 스킬", new[]
            {
                ("패시브", "trainSkillDataDB.trainPassiveSkillDataList"),
            }),
            ("업그레이드", new[]
            {
                ("일반", "upgradeDataList"),
                ("영구", "permanentUpgradeDataList"),
                ("강화 등급", "statUpgradeTierDataList"),
                ("포탑 강화 규칙", "trainStatUpgradeRuleDataList"),
            }),
            ("스테이지", new[] { ("스테이지", "stageDataList") }),
            ("스킬트리", new[] { ("노드", "skillNodeDataList") }),
        };

        /// <summary>단일 객체형 탭 정의 — 목록이 아니라 프로퍼티 전체를 그대로 그린다.</summary>
        private static readonly (string tabName, (string label, string path)[] properties)[] DirectTabs =
        {
            ("선택지", new[] { ("3지선다 DB", "triChoiceDB") }),
            ("기타", new[]
            {
                ("엘리트", "eliteData"),
                ("스코어", "scoreData"),
                ("사운드 DB", "soundDB"),
            }),
        };

        private static readonly string[] TabNames = _BuildTabNames();
        #endregion

        #region Variables
        private DB _db;
        private SerializedObject _serializedDb;

        private int _selectedTab;
        private int _selectedSubList;
        private string _searchText = "";
        private Vector2 _listScroll;
        private Vector2 _detailScroll;

        // 탭/서브리스트를 오가도 선택이 유지되도록 프로퍼티 경로별로 저장
        private readonly Dictionary<string, int> _selectionByPath = new();
        #endregion

        #region LifeCycle
        [MenuItem("Tools/DB Browser")]
        public static void Open()
        {
            var window = GetWindow<DatabaseBrowserWindow>("DB 브라우저");
            window.minSize = new Vector2(860f, 480f);
            window.Show();
        }

        private void OnEnable()
        {
            _LoadDb();
            wantsMouseMove = true;   // 목록 행 호버 하이라이트 갱신용
            Undo.undoRedoPerformed += Repaint;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
        }

        private void OnGUI()
        {
            if (_db == null || _serializedDb == null) _LoadDb();

            if (_db == null)
            {
                EditorGUILayout.HelpBox($"DB 에셋을 찾을 수 없습니다: {DB_ASSET_PATH}", MessageType.Error);
                _db = (DB)EditorGUILayout.ObjectField("DB 에셋", _db, typeof(DB), false);

                if (_db != null) _LoadDb();

                return;
            }

            _serializedDb.Update();

            if (Event.current.type == EventType.MouseMove) Repaint();
            if (AssetPreview.IsLoadingAssetPreviews()) Repaint();   // 프리팹 프리뷰 비동기 로딩 반영

            _DrawToolbar();

            int previousTab = _selectedTab;
            _selectedTab = GUILayout.Toolbar(_selectedTab, TabNames);

            if (_selectedTab != previousTab)
            {
                _selectedSubList = 0;
                _searchText = "";
                GUI.FocusControl(null);
            }

            EditorGUILayout.Space(2f);

            if (_selectedTab < ListTabs.Length)
                _DrawListTab(ListTabs[_selectedTab]);
            else
                _DrawDirectTab(DirectTabs[_selectedTab - ListTabs.Length]);

            _serializedDb.ApplyModifiedProperties();
        }
        #endregion

        #region Toolbar
        private void _DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(TOOLBAR_HEIGHT));
            GUILayout.Label("DB.asset", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("DB 선택", EditorStyles.toolbarButton))
                EditorGUIUtility.PingObject(_db);

            if (GUILayout.Button("에셋 저장", EditorStyles.toolbarButton))
                AssetDatabase.SaveAssetIfDirty(_db);

            GUILayout.EndHorizontal();
        }
        #endregion

        #region List Tab (마스터-디테일)
        private void _DrawListTab((string tabName, (string label, string path)[] lists) tab)
        {
            if (tab.lists.Length > 1)
            {
                var subNames = new string[tab.lists.Length];

                for (int i = 0; i < tab.lists.Length; i++)
                    subNames[i] = tab.lists[i].label;

                int previousSub = _selectedSubList;
                _selectedSubList = GUILayout.Toolbar(Mathf.Clamp(_selectedSubList, 0, tab.lists.Length - 1), subNames);

                if (_selectedSubList != previousSub)
                {
                    _searchText = "";
                    GUI.FocusControl(null);
                }
            }
            else
            {
                _selectedSubList = 0;
            }

            string path = tab.lists[_selectedSubList].path;
            SerializedProperty list = _serializedDb.FindProperty(path);

            if (list == null || !list.isArray)
            {
                EditorGUILayout.HelpBox($"프로퍼티를 찾을 수 없습니다: {path}", MessageType.Error);

                return;
            }

            if (tab.tabName == "스킬트리")
            {
                if (GUILayout.Button("스킬트리 그래프 에디터 열기 (노드 이동·연결)"))
                    SkillTreeGraphWindow.Open();
            }

            GUILayout.BeginHorizontal();
            _DrawMasterList(path, list);
            _DrawDetail(path, list);
            GUILayout.EndHorizontal();
        }

        private void _DrawMasterList(string path, SerializedProperty list)
        {
            GUILayout.BeginVertical(GUILayout.Width(LIST_WIDTH));

            GUILayout.BeginHorizontal();
            _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);

            if (GUILayout.Button("+", GUILayout.Width(24f)))
                _AddElement(path, list);

            using (new EditorGUI.DisabledScope(_GetSelection(path, list) < 0))
            {
                if (GUILayout.Button("−", GUILayout.Width(24f)))
                    _DeleteElement(path, list);
            }
            GUILayout.EndHorizontal();

            GUILayout.Label($"{list.arraySize}개", EditorStyles.miniLabel);

            _listScroll = GUILayout.BeginScrollView(_listScroll, "box");
            int selection = _GetSelection(path, list);
            string filter = _searchText?.Trim().ToLowerInvariant() ?? "";

            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                string label = _GetElementLabel(element, i);

                if (filter.Length > 0 && !label.ToLowerInvariant().Contains(filter)) continue;

                Rect row = GUILayoutUtility.GetRect(0f, ROW_HEIGHT, GUILayout.ExpandWidth(true));
                bool isSelected = i == selection;

                if (isSelected)
                    EditorGUI.DrawRect(row, new Color(1f, 0.85f, 0.3f, 0.15f));
                else if (row.Contains(Event.current.mousePosition))
                    EditorGUI.DrawRect(row, new Color(1f, 1f, 1f, 0.04f));

                var thumbRect = new Rect(row.x + 2f, row.y + (ROW_HEIGHT - THUMB_SIZE) * 0.5f, THUMB_SIZE, THUMB_SIZE);
                EditorGUI.DrawRect(thumbRect, new Color(0f, 0f, 0f, 0.25f));
                _DrawFirstPreview(element, thumbRect);

                var labelRect = new Rect(thumbRect.xMax + 4f, row.y, row.width - thumbRect.width - 8f, ROW_HEIGHT);
                var rowStyle = isSelected ? EditorStyles.boldLabel : EditorStyles.label;
                GUI.Label(labelRect, label, rowStyle);

                if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition))
                {
                    _selectionByPath[path] = i;
                    GUI.FocusControl(null);
                    Event.current.Use();
                    Repaint();
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void _DrawDetail(string path, SerializedProperty list)
        {
            GUILayout.BeginVertical();
            _detailScroll = GUILayout.BeginScrollView(_detailScroll);

            int selection = _GetSelection(path, list);

            if (selection < 0)
            {
                EditorGUILayout.HelpBox("좌측 목록에서 항목을 선택하세요.\n'+' = 추가(선택 항목 복제) / '−' = 선택 항목 삭제", MessageType.Info);
            }
            else
            {
                SerializedProperty element = list.GetArrayElementAtIndex(selection);
                EditorGUILayout.LabelField(_GetElementLabel(element, selection), EditorStyles.boldLabel);
                EditorGUILayout.Space(4f);
                _DrawPreviewSection(element);
                _DrawElementChildren(element);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        /// <summary>요소의 직속 자식 프로퍼티들을 펼쳐서 그린다 ("Element N" 폴드아웃 없이 바로 필드 표시).</summary>
        private static void _DrawElementChildren(SerializedProperty element)
        {
            SerializedProperty iterator = element.Copy();
            SerializedProperty end = element.GetEndProperty();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                EditorGUILayout.PropertyField(iterator, true);
                enterChildren = false;
            }
        }

        private void _AddElement(string path, SerializedProperty list)
        {
            int selection = _GetSelection(path, list);

            if (selection >= 0)
            {
                // 선택 항목 바로 아래에 복제 — 비슷한 데이터를 이어서 만들 때 편하다
                list.InsertArrayElementAtIndex(selection);
                _selectionByPath[path] = selection + 1;
            }
            else
            {
                list.arraySize++;
                _selectionByPath[path] = list.arraySize - 1;
            }
        }

        private void _DeleteElement(string path, SerializedProperty list)
        {
            int selection = _GetSelection(path, list);

            if (selection < 0) return;

            list.DeleteArrayElementAtIndex(selection);
            _selectionByPath[path] = Mathf.Min(selection, list.arraySize - 1);
        }

        private int _GetSelection(string path, SerializedProperty list)
        {
            if (!_selectionByPath.TryGetValue(path, out int selection)) return -1;

            if (selection < 0 || selection >= list.arraySize) return -1;

            return selection;
        }

        /// <summary>목록 행 라벨 — id/name 프로퍼티가 있으면 함께 표시한다.</summary>
        private static string _GetElementLabel(SerializedProperty element, int index)
        {
            string id = _GetStringLike(element.FindPropertyRelative("id"));
            string name = _GetStringLike(element.FindPropertyRelative("name"));

            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name)) return $"[{index}] {id} · {name}";
            if (!string.IsNullOrEmpty(id)) return $"[{index}] {id}";
            if (!string.IsNullOrEmpty(name)) return $"[{index}] {name}";

            return $"[{index}] (id 없음)";
        }

        private static string _GetStringLike(SerializedProperty property)
        {
            if (property == null) return null;

            return property.propertyType switch
            {
                SerializedPropertyType.String => property.stringValue,
                SerializedPropertyType.Integer => property.intValue.ToString(),
                _ => null,
            };
        }
        #endregion

        #region Direct Tab (단일 객체)
        private void _DrawDirectTab((string tabName, (string label, string path)[] properties) tab)
        {
            _detailScroll = GUILayout.BeginScrollView(_detailScroll);

            foreach ((string label, string path) in tab.properties)
            {
                SerializedProperty property = _serializedDb.FindProperty(path);

                if (property == null)
                {
                    EditorGUILayout.HelpBox($"프로퍼티를 찾을 수 없습니다: {path}", MessageType.Error);

                    continue;
                }

                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(property, true);
                EditorGUILayout.Space(8f);
            }

            GUILayout.EndScrollView();
        }
        #endregion

        #region Sprite Preview (뷰어)
        private readonly struct PreviewEntry
        {
            public readonly string Label;
            public readonly Sprite Sprite;
            public readonly Texture Texture;

            public PreviewEntry(string label, Sprite sprite, Texture texture)
            {
                Label = label;
                Sprite = sprite;
                Texture = texture;
            }
        }

        /// <summary>
        /// 요소가 참조하는 스프라이트/텍스처/프리팹 프리뷰를 수집한다.
        /// 대응: ① 직접 Sprite/Texture 필드 ② Sprite[] 배열(애니메이션 프레임) ③ iconId → Resources/Sprite ④ GameObject 프리팹(에셋 프리뷰)
        /// </summary>
        private static List<PreviewEntry> _CollectPreviews(SerializedProperty element, int maxCount)
        {
            var results = new List<PreviewEntry>();
            SerializedProperty iterator = element.Copy();
            SerializedProperty end = element.GetEndProperty();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;

                if (results.Count >= maxCount) break;

                if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                {
                    Object value = iterator.objectReferenceValue;

                    if (value == null) continue;

                    if (value is Sprite sprite)
                    {
                        results.Add(new PreviewEntry(iterator.displayName, sprite, null));
                    }
                    else if (value is Texture texture)
                    {
                        results.Add(new PreviewEntry(iterator.displayName, null, texture));
                    }
                    else if (value is GameObject)
                    {
                        Texture preview = AssetPreview.GetAssetPreview(value);

                        if (preview == null) preview = AssetPreview.GetMiniThumbnail(value);
                        if (preview != null) results.Add(new PreviewEntry(iterator.displayName, null, preview));
                    }
                }
                else if (iterator.isArray && iterator.arrayElementType == "PPtr<$Sprite>")
                {
                    for (int i = 0; i < iterator.arraySize && results.Count < maxCount; i++)
                    {
                        var frame = iterator.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;

                        if (frame != null)
                            results.Add(new PreviewEntry($"{iterator.displayName}[{i}]", frame, null));
                    }
                }
                else if (iterator.propertyType == SerializedPropertyType.String && iterator.name == "iconId")
                {
                    string iconId = iterator.stringValue;

                    if (string.IsNullOrEmpty(iconId)) continue;

                    var sprite = Resources.Load<Sprite>($"Sprite/{iconId}");

                    if (sprite != null) results.Add(new PreviewEntry($"iconId: {iconId}", sprite, null));
                }
            }

            return results;
        }

        /// <summary>목록 행 썸네일 — 첫 번째 프리뷰만 그린다.</summary>
        private static void _DrawFirstPreview(SerializedProperty element, Rect rect)
        {
            List<PreviewEntry> previews = _CollectPreviews(element, 1);

            if (previews.Count > 0) _DrawPreview(rect, previews[0]);
        }

        /// <summary>상세 패널 상단 뷰어 — 첫 프리뷰는 크게, 나머지(애니메이션 프레임 등)는 스트립으로 나열.</summary>
        private void _DrawPreviewSection(SerializedProperty element)
        {
            List<PreviewEntry> previews = _CollectPreviews(element, PREVIEW_MAX);

            if (previews.Count == 0) return;

            EditorGUILayout.LabelField("미리보기", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            Rect large = GUILayoutUtility.GetRect(PREVIEW_LARGE, PREVIEW_LARGE,
                GUILayout.Width(PREVIEW_LARGE), GUILayout.Height(PREVIEW_LARGE));
            EditorGUI.DrawRect(large, new Color(0f, 0f, 0f, 0.25f));
            _DrawPreview(large, previews[0]);

            GUILayout.BeginVertical();
            GUILayout.Label(previews[0].Label, EditorStyles.miniLabel);

            if (previews[0].Sprite != null)
                GUILayout.Label(previews[0].Sprite.name, EditorStyles.miniLabel);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            for (int start = 1; start < previews.Count; start += PREVIEW_PER_ROW)
            {
                GUILayout.BeginHorizontal();

                for (int i = start; i < Mathf.Min(start + PREVIEW_PER_ROW, previews.Count); i++)
                {
                    Rect small = GUILayoutUtility.GetRect(PREVIEW_SMALL, PREVIEW_SMALL,
                        GUILayout.Width(PREVIEW_SMALL), GUILayout.Height(PREVIEW_SMALL));
                    EditorGUI.DrawRect(small, new Color(0f, 0f, 0f, 0.25f));
                    _DrawPreview(small, previews[i]);
                    GUI.Label(small, new GUIContent("", previews[i].Label));   // 툴팁
                }

                GUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(6f);
        }

        private static void _DrawPreview(Rect rect, PreviewEntry entry)
        {
            if (entry.Sprite != null)
                _DrawSprite(rect, entry.Sprite);
            else if (entry.Texture != null)
                GUI.DrawTexture(rect, entry.Texture, ScaleMode.ScaleToFit);
        }

        /// <summary>스프라이트를 아틀라스 부분 UV로 잘라 비율 유지하며 그린다 (Tight 패킹은 전체 텍스처로 폴백).</summary>
        private static void _DrawSprite(Rect rect, Sprite sprite)
        {
            Texture2D texture = sprite.texture;

            if (texture == null) return;

            if (sprite.packed && sprite.packingMode == SpritePackingMode.Tight)
            {
                GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);

                return;
            }

            Rect pixelRect = sprite.textureRect;
            var uv = new Rect(pixelRect.x / texture.width, pixelRect.y / texture.height,
                pixelRect.width / texture.width, pixelRect.height / texture.height);

            float aspect = pixelRect.height > 0f ? pixelRect.width / pixelRect.height : 1f;
            Rect fitted = rect;

            if (aspect >= 1f)
            {
                fitted.height = rect.width / aspect;
                fitted.y += (rect.height - fitted.height) * 0.5f;
            }
            else
            {
                fitted.width = rect.height * aspect;
                fitted.x += (rect.width - fitted.width) * 0.5f;
            }

            GUI.DrawTextureWithTexCoords(fitted, texture, uv);
        }
        #endregion

        #region Helpers
        private void _LoadDb()
        {
            if (_db == null) _db = AssetDatabase.LoadAssetAtPath<DB>(DB_ASSET_PATH);

            if (_db == null) return;

            _serializedDb = new SerializedObject(_db);
        }

        private static string[] _BuildTabNames()
        {
            var names = new List<string>();

            foreach ((string tabName, _) in ListTabs)
                names.Add(tabName);

            foreach ((string tabName, _) in DirectTabs)
                names.Add(tabName);

            return names.ToArray();
        }
        #endregion
    }
}
#endif
