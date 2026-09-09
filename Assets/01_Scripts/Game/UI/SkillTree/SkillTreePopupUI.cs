using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cumic;
using TrainDefense.Game.Datas;
using TrainDefense.Localize;

namespace TrainDefense.Game.UI.SkillTree
{
    /// <summary>
    /// 스킬트리 팝업. 보유 스킬 포인트를 표시하고 (lane, row, col) 데이터 주도로 노드·레일을 런타임 생성한다.
    /// 레인 타이틀·출발역 마커는 프리팹에 정적 배치돼 있고 (런타임 생성 금지), 여기서는 문구·점등만 갱신한다.
    /// 노드 클릭 → 상세 패널에서 습득, 리스펙 버튼으로 전액 환급 초기화 (v1 무료).
    /// SkillTreeButton이 Resources의 Popup_SkillTree 프리팹을 Instantiate해서 띄운다.
    /// 흐름은 아래→위 (출발역이 하단) — treeContent는 pivot (0.5, 0) 하단 기준을 권장한다.
    /// </summary>
    public class SkillTreePopupUI : MonoBehaviour
    {
        // DESIGN.md 그리드: 노드 144px + 간격 48 = 피치 192, 레인 3개(화력/방어/유틸) 세로 선로
        private const float NodePitch = 192f;
        private const float LaneSpacing = 360f;
        private const float RailThickness = 56f;   // 기찻길 스프라이트가 침목까지 보이는 목업 폭
        private const float ContentPadding = 96f;
        // 상단 레인 타이틀 공간 — 노드 그리드가 그만큼 위로 밀린다
        // 하단 출발역은 없앴다(첫 업그레이드가 그 자리로 내려옴). 되살리려면 160f로 되돌리고
        // 프리팹의 Station_* / StationLabel_* 를 다시 켠 뒤 루트 노드 선로 생성을 복원한다.
        private const float StationExtraBottom = 0f;
        private const float LaneTitleExtraTop = 170f;

        #region Fields
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI skillPointText;
        [Header("정적 라벨 (로컬라이즈)")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI acquireText;
        [SerializeField] private TextMeshProUGUI respecText;
        [Header("레인 타이틀·출발역 (프리팹 정적 배치 — index = (int)SkillTreeLane)")]
        [SerializeField] private TextMeshProUGUI[] laneTitleTexts;
        [SerializeField] private Image[] stationImages;
        [SerializeField] private TextMeshProUGUI[] stationLabelTexts;
        [Header("트리 (런타임 생성)")]
        [SerializeField] private RectTransform treeContent;        // 스크롤 컨텐츠 — pivot (0.5, 0) 권장
        [SerializeField] private SkillTreeNodeUI nodePrefab;
        [SerializeField] private SkillTreeRailUI railPrefab;
        [Header("상세 패널 (노드 클릭 시 표시)")]
        [SerializeField] private GameObject detailPanel;           // 선택 전에는 숨김
        [SerializeField] private Image detailIcon;
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailDescriptionText;
        [SerializeField] private TextMeshProUGUI detailLevelText;
        [SerializeField] private TextMeshProUGUI detailCostText;
        [SerializeField] private Button acquireButton;
        [SerializeField] private Button respecButton;
        #endregion

        private readonly Dictionary<string, SkillTreeNodeUI> _nodeUIs = new();
        private readonly List<SkillTreeRailUI> _rails = new();
        private SkillNodeData _selectedData;

        #region LifeCycle
        private void OnEnable()
        {
            Localization.OnLanguageChanged += _ApplyStaticTexts;
            Localization.OnInitialized += _ApplyStaticTexts;
            _ApplyStaticTexts();

            PopupTween.PlayShow(gameObject);
        }

        private void OnDisable()
        {
            Localization.OnLanguageChanged -= _ApplyStaticTexts;
            Localization.OnInitialized -= _ApplyStaticTexts;
        }

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            if (acquireButton != null)
                acquireButton.onClick.AddListener(_OnClickAcquire);
            if (respecButton != null)
                respecButton.onClick.AddListener(_OnClickRespec);
        }

        private void Start()
        {
            _BuildTree();
            _RefreshSkillPoint();
            _RefreshRails(sweepToNodeId: null);
            _RefreshDetail();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
            if (acquireButton != null)
                acquireButton.onClick.RemoveListener(_OnClickAcquire);
            if (respecButton != null)
                respecButton.onClick.RemoveListener(_OnClickRespec);
        }
        #endregion

        public void Close()
        {
            PopupTween.PlayHide(gameObject, () => Destroy(gameObject));
        }

        // 프리팹에 박힌 정적 라벨(타이틀·레인 타이틀·출발역·초기화 버튼)을 현재 언어로 갱신한다.
        private void _ApplyStaticTexts()
        {
            if (titleText != null)
                titleText.text = LocalizeHelper.GetByKey("UI_SkillTree_Title", "스킬 트리");
            if (respecText != null)
                respecText.text = LocalizeHelper.GetByKey("UI_SkillTree_Respec", "초기화");

            if (laneTitleTexts != null)
            {
                for (int i = 0; i < laneTitleTexts.Length; i++)
                {
                    if (laneTitleTexts[i] != null)
                        laneTitleTexts[i].text = _GetLaneName((SkillTreeLane)i);
                }
            }

            if (stationLabelTexts != null)
            {
                string stationName = LocalizeHelper.GetByKey("UI_SkillTree_Station", "출발역");
                foreach (var stationLabel in stationLabelTexts)
                {
                    if (stationLabel != null)
                        stationLabel.text = stationName;
                }
            }

            // 습득 버튼 문구(획득/레벨업/MAX/선행 노드 필요)·비용 라벨도 언어를 따른다
            _RefreshDetail();
        }

        // 레인 표시 이름 — DB(스킬트리 SO)의 레인 데이터에서 조회한다
        private static string _GetLaneName(SkillTreeLane lane)
        {
            var manager = SkillTreeManager.Instance;

            return manager != null ? manager.GetLaneName(lane) : string.Empty;
        }

        #region Build
        private void _BuildTree()
        {
            var manager = SkillTreeManager.Instance;
            if (manager == null || nodePrefab == null || treeContent == null) return;

            int maxRow = 0;
            foreach (var data in manager.GetNodes())
            {
                if (data == null) continue;

                SkillTreeNodeUI node = Instantiate(nodePrefab, treeContent);
                node.RectTransform.anchoredPosition = _GetNodePosition(data);
                node.Init(data, _OnSelectNode);
                _nodeUIs[data.Id] = node;

                if (data.Row > maxRow) maxRow = data.Row;
            }

            // 선행 → 노드 간선마다 레일 하나. 노드 아래에 깔리도록 맨 앞 형제로 보낸다.
            if (railPrefab != null)
            {
                foreach (var data in manager.GetNodes())
                {
                    if (data == null) continue;

                    // 출발역을 없애고 첫 노드를 그 자리로 내렸으므로, 루트 노드로 올라오는 선로는 없다.
                    if (data.Prerequisites == null || data.Prerequisites.Length == 0)
                        continue;

                    foreach (string fromId in data.Prerequisites)
                    {
                        var fromData = manager.GetNode(fromId);
                        if (fromData == null) continue;

                        _CreateRail(fromId, data.Id, _GetNodePosition(fromData), _GetNodePosition(data));
                    }
                }
            }

            // 세로 스크롤 높이 = 출발역 + 최상단 행 + 레인 타이틀 + 상하 여백
            treeContent.sizeDelta = new Vector2(
                treeContent.sizeDelta.x,
                ContentPadding * 2f + StationExtraBottom + LaneTitleExtraTop + maxRow * NodePitch);
        }

        private void _CreateRail(string fromId, string toId, Vector2 fromPosition, Vector2 toPosition)
        {
            SkillTreeRailUI rail = Instantiate(railPrefab, treeContent);
            rail.transform.SetAsFirstSibling();
            rail.Init(fromId, toId);
            rail.Place(fromPosition, toPosition, RailThickness);
            _rails.Add(rail);
        }

        // 흐름 아래→위: 출발역(하단) 위로 row 0부터 쌓인다. 레인이 세로 선로 한 줄, col은 분기 오프셋.
        private Vector2 _GetNodePosition(SkillNodeData data)
        {
            float x = ((int)data.Lane - 1) * LaneSpacing + data.Col * NodePitch;
            float y = ContentPadding + StationExtraBottom + data.Row * NodePitch;

            return new Vector2(x, y);
        }

        #endregion

        #region Refresh
        private void _OnSelectNode(SkillNodeData data)
        {
            _selectedData = data;
            _RefreshDetail();
        }

        private void _OnClickAcquire()
        {
            var manager = SkillTreeManager.Instance;
            if (manager == null || _selectedData == null) return;
            if (!manager.TryAcquire(_selectedData.Id)) return;

            // 습득 성공 → 포인트·전 노드·레일·상세 갱신 (포인트가 줄어 다른 노드의 습득 가능 여부도 바뀜)
            _RefreshSkillPoint();
            foreach (var node in _nodeUIs.Values)
                node.Refresh();

            // 선로 점등 스윕은 첫 습득(레벨 1) 순간에만 — 이미 점등된 선로는 그대로 둔다
            string sweepToNodeId = manager.GetLevel(_selectedData.Id) == 1 ? _selectedData.Id : null;
            _RefreshRails(sweepToNodeId);

            if (_nodeUIs.TryGetValue(_selectedData.Id, out var acquiredNode))
                acquiredNode.PlayAcquirePunch();

            _RefreshDetail();
        }

        private void _OnClickRespec()
        {
            var manager = SkillTreeManager.Instance;
            if (manager == null) return;
            if (!manager.ResetAll()) return;

            // 초기화로 잠긴 노드가 선택된 채 남지 않게 — 잠김 노드는 상세 표시 대상이 아니다
            if (_selectedData != null && !manager.ArePrerequisitesMet(_selectedData.Id))
                _selectedData = null;

            _RefreshSkillPoint();
            foreach (var node in _nodeUIs.Values)
                node.Refresh();
            _RefreshRails(sweepToNodeId: null);
            _RefreshDetail();
        }

        private void _RefreshRails(string sweepToNodeId)
        {
            var manager = SkillTreeManager.Instance;
            if (manager == null) return;

            foreach (var rail in _rails)
            {
                bool isLit = manager.GetLevel(rail.ToNodeId) > 0;
                rail.SetLit(isLit, animated: isLit && rail.ToNodeId == sweepToNodeId);
                // 획득 가능 노드의 진입 선로는 accent로 맥동한다 (DESIGN.md 상태 표)
                rail.SetApproachPulse(!isLit && manager.CanAcquire(rail.ToNodeId));
            }

            _RefreshStations(manager);
        }

        // 레인에 습득한 노드가 하나라도 있으면 해당 출발역 점등
        private void _RefreshStations(SkillTreeManager manager)
        {
            if (stationImages == null) return;

            var litLanes = new HashSet<SkillTreeLane>();
            foreach (var data in manager.GetNodes())
            {
                if (data != null && manager.GetLevel(data.Id) > 0)
                    litLanes.Add(data.Lane);
            }

            for (int i = 0; i < stationImages.Length; i++)
            {
                if (stationImages[i] == null) continue;

                stationImages[i].color = litLanes.Contains((SkillTreeLane)i)
                    ? SkillTreePalette.Accent
                    : SkillTreePalette.SurfaceLine;
            }
        }

        // 선택된 노드의 계열·이름·설명·레벨·비용·습득 가능 여부를 상세 패널에 표시한다.
        private void _RefreshDetail()
        {
            var manager = SkillTreeManager.Instance;
            bool hasSelection = _selectedData != null;

            if (detailPanel != null)
                detailPanel.SetActive(hasSelection);

            if (detailIcon != null)
            {
                detailIcon.sprite = hasSelection ? _selectedData.Icon : null;
                detailIcon.enabled = hasSelection && _selectedData.Icon != null;
            }

            if (detailNameText != null)
            {
                // 목업 레인 칩 — 이름 위에 계열명을 accent로 작게 표기 (rich text, 프리팹 추가 요소 없이)
                detailNameText.text = hasSelection
                    ? $"<size=55%><color=#E47A3C>{_GetLaneName(_selectedData.Lane)}</color></size>\n{_selectedData.Name}"
                    : string.Empty;
            }
            if (detailDescriptionText != null)
                detailDescriptionText.text = hasSelection ? _selectedData.Description : string.Empty;

            if (!hasSelection || manager == null)
            {
                if (detailLevelText != null) detailLevelText.text = string.Empty;
                if (detailCostText != null) detailCostText.text = string.Empty;
                if (acquireText != null) acquireText.text = LocalizeHelper.GetByKey("UI_SkillTree_Acquire", "습득");
                if (acquireButton != null) acquireButton.interactable = false;

                return;
            }

            int level = manager.GetLevel(_selectedData.Id);
            bool isMax = manager.IsMaxLevel(_selectedData.Id);
            bool arePrerequisitesMet = manager.ArePrerequisitesMet(_selectedData.Id);
            int cost = _selectedData.GetCostAtLevel(level);
            bool isAffordable = cost <= manager.SkillPoint;

            if (detailLevelText != null)
            {
                detailLevelText.text = isMax ? "MAX"
                    : _selectedData.MaxLevel > 0 ? $"Lv {level}/{_selectedData.MaxLevel}" : $"Lv {level}";
                detailLevelText.color = isMax ? SkillTreePalette.Mastered : SkillTreePalette.OnSurface;
            }

            if (detailCostText != null)
            {
                detailCostText.text = isMax ? "MAX"
                    : $"{LocalizeHelper.GetByKey("UI_SkillTree_NeedPoint", "필요 포인트")} {cost.ToCommaString()}";
                // 비용 부족은 색 + 버튼 비활성으로 이중부호화 (danger 텍스트는 #E06666 — #B34040 금지)
                detailCostText.color = isMax || isAffordable ? SkillTreePalette.OnSurface : SkillTreePalette.DangerText;
            }

            // 버튼 문구가 곧 상태다: 획득 / 레벨업 / MAX / 선행 노드 필요 (목업)
            if (acquireText != null)
            {
                acquireText.text = isMax ? "MAX"
                    : !arePrerequisitesMet ? LocalizeHelper.GetByKey("UI_SkillTree_NeedPrerequisite", "선행 노드 필요")
                    : level > 0 ? LocalizeHelper.GetByKey("UI_SkillTree_LevelUp", "레벨업")
                    : LocalizeHelper.GetByKey("UI_SkillTree_Acquire", "습득");
            }

            if (acquireButton != null)
                acquireButton.interactable = manager.CanAcquire(_selectedData.Id);
        }

        private void _RefreshSkillPoint()
        {
            var manager = SkillTreeManager.Instance;
            if (skillPointText != null && manager != null)
                skillPointText.text = manager.SkillPoint.ToCommaString();
        }
        #endregion
    }
}
