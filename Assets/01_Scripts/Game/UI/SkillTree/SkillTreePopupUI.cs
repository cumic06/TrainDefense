using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrainDefense.Game.Datas;
using TrainDefense.Localize;

namespace TrainDefense.Game.UI.SkillTree
{
    /// <summary>
    /// 스킬트리 팝업. 보유 스킬 포인트를 표시하고 (lane, row, col) 데이터 주도로 노드·레일을 런타임 생성한다.
    /// 노드 클릭 → 하단 상세 패널에서 습득, 리스펙 버튼으로 전액 환급 초기화 (v1 무료).
    /// SkillTreeButton이 Resources의 Popup_SkillTree 프리팹을 Instantiate해서 띄운다.
    /// 흐름은 아래→위 (출발역이 하단) — treeContent는 pivot (0.5, 0) 하단 기준을 권장한다.
    /// </summary>
    public class SkillTreePopupUI : MonoBehaviour
    {
        // DESIGN.md 그리드: 노드 144px + 간격 48 = 피치 192, 레인 3개(화력/방어/유틸) 세로 선로
        private const float NodePitch = 192f;
        private const float LaneSpacing = 360f;
        private const float RailThickness = 24f;
        private const float ContentPadding = 96f;

        #region Fields
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI skillPointText;
        [Header("정적 라벨 (로컬라이즈)")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI acquireText;
        [SerializeField] private TextMeshProUGUI respecText;
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

        // 프리팹에 박힌 정적 라벨(타이틀·습득·초기화 버튼)을 현재 언어로 갱신한다.
        private void _ApplyStaticTexts()
        {
            if (titleText != null)
                titleText.text = LocalizeHelper.GetByKey("UI_SkillTree_Title", "스킬 트리");
            if (acquireText != null)
                acquireText.text = LocalizeHelper.GetByKey("UI_SkillTree_Acquire", "습득");
            if (respecText != null)
                respecText.text = LocalizeHelper.GetByKey("UI_SkillTree_Respec", "초기화");
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
                    if (data == null || data.Prerequisites == null) continue;

                    foreach (string fromId in data.Prerequisites)
                    {
                        var fromData = manager.GetNode(fromId);
                        if (fromData == null) continue;

                        SkillTreeRailUI rail = Instantiate(railPrefab, treeContent);
                        rail.transform.SetAsFirstSibling();
                        rail.Init(fromId, data.Id);
                        rail.Place(_GetNodePosition(fromData), _GetNodePosition(data), RailThickness);
                        _rails.Add(rail);
                    }
                }
            }

            // 세로 스크롤 높이 = 최상단 행 + 상하 여백
            treeContent.sizeDelta = new Vector2(treeContent.sizeDelta.x, ContentPadding * 2f + maxRow * NodePitch);
        }

        // 흐름 아래→위: 출발역(row 0)이 하단. 레인이 세로 선로 한 줄, col은 분기 오프셋.
        private Vector2 _GetNodePosition(SkillNodeData data)
        {
            float x = ((int)data.Lane - 1) * LaneSpacing + data.Col * NodePitch;
            float y = ContentPadding + data.Row * NodePitch;

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
            }
        }

        // 선택된 노드의 이름·설명·레벨·비용·습득 가능 여부를 하단 패널에 표시한다.
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
                detailNameText.text = hasSelection ? _selectedData.Name : string.Empty;
            if (detailDescriptionText != null)
                detailDescriptionText.text = hasSelection ? _selectedData.Description : string.Empty;

            if (!hasSelection || manager == null)
            {
                if (detailLevelText != null) detailLevelText.text = string.Empty;
                if (detailCostText != null) detailCostText.text = string.Empty;
                if (acquireButton != null) acquireButton.interactable = false;

                return;
            }

            int level = manager.GetLevel(_selectedData.Id);
            bool isMax = manager.IsMaxLevel(_selectedData.Id);
            int cost = _selectedData.GetCostAtLevel(level);
            bool isAffordable = cost <= manager.SkillPoint;

            if (detailLevelText != null)
                detailLevelText.text = _selectedData.MaxLevel > 0 ? $"Lv {level}/{_selectedData.MaxLevel}" : $"Lv {level}";

            if (detailCostText != null)
            {
                detailCostText.text = isMax ? "MAX" : cost.ToString();
                // 비용 부족은 색 + 버튼 비활성으로 이중부호화 (danger 텍스트는 #E06666 — #B34040 금지)
                detailCostText.color = isMax || isAffordable ? SkillTreePalette.OnSurface : SkillTreePalette.DangerText;
            }

            if (acquireButton != null)
                acquireButton.interactable = manager.CanAcquire(_selectedData.Id);
        }

        private void _RefreshSkillPoint()
        {
            var manager = SkillTreeManager.Instance;
            if (skillPointText != null && manager != null)
                skillPointText.text = manager.SkillPoint.ToString();
        }
        #endregion
    }
}
