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
    /// 스킬트리 팝업. 보유 재화를 표시하고 (lane, row, col) 데이터 주도로 노드·레일을 런타임 생성한다.
    /// 레인 타이틀·출발역 마커는 프리팹에 정적 배치돼 있고 (런타임 생성 금지), 여기서는 문구·점등만 갱신한다.
    /// 노드 탭 → 트리 아래 상세 띠(이름 … LV / 현재 효과 / 다음 효과 + 레벨업 버튼[문구 / ◆비용])에서 레벨업한다.
    /// (리스펙 버튼은 09-11에 제거 — 강화 초기화 기능 자체를 없애기로 함)
    /// SkillTreeButton이 Resources의 Popup_SkillTree 프리팹을 Instantiate해서 띄운다.
    /// 흐름은 왼쪽→오른쪽 (첫 노드가 왼쪽 열) — treeContent는 pivot (0, 0.5) 왼쪽 기준, 노드·선로 프리팹도 앵커 (0, 0.5).
    /// </summary>
    public class SkillTreePopupUI : MonoBehaviour
    {
        // DESIGN.md 그리드: 노드 136px. 09-13 가로 선로 전환 — 레인 3개(화력/방어/유틸)가 가로 선로 세 줄이 되고 행(row)이 왼쪽→오른쪽 역이다.
        // 남는 가로는 선로 길이에 돌린다 (세로 배치 때 "남는 세로는 선로 노출에"와 같은 원칙): 트리 판 폭 1170 = 여백 61·2 + 4·136 + 선로 168·3
        private const float NodeSize = 136f;
        private const float NodePitch = 304f;       // 노드 136 + 선로 168
        private const float LanePitch = 176f;       // 노드 136 + 레인 간격 40 (트리 판 높이 588 = 여백 50·2 + 3·136 + 2·40 — 아래 상세 띠 176 자리를 남긴다)
        private const float RailThickness = 56f;    // 기찻길 스프라이트가 침목까지 보이는 목업 폭
        // 왼쪽 여백 61 = 트리 판 9-slice 외곽선(rect 안쪽 10) + 선택 링(+14) 바깥 여유. 세로 여백은 LanePitch가 결정한다
        private const float ContentPaddingLeft = 61f;

        #region Fields
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI coinText;
        [Header("정적 라벨 (로컬라이즈)")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI acquireText;
        [Header("레인 타이틀·출발역 (프리팹 정적 배치 — index = (int)SkillTreeLane)")]
        [SerializeField] private TextMeshProUGUI[] laneTitleTexts;
        [SerializeField] private Image[] stationImages;
        [SerializeField] private TextMeshProUGUI[] stationLabelTexts;
        [Header("트리 (런타임 생성)")]
        [SerializeField] private RectTransform treeContent;        // 스크롤 컨텐츠 — pivot (0, 0.5) 왼쪽 기준, 세로는 뷰포트에 늘림
        [SerializeField] private SkillTreeNodeUI nodePrefab;
        [SerializeField] private SkillTreeRailUI railPrefab;
        [Header("상세 띠 (트리 아래 — 세로 레이아웃 그룹: 이름 … LV / 현재 효과 / ◆비용 다음 효과. 라벨·화살표 없음)")]
        [SerializeField] private GameObject detailPanel;           // 선택이 없을 때만 숨김 (열자마자 첫 노드를 기본 선택)
        [SerializeField] private Image detailIcon;
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailDescriptionText;      // 효과 한 줄 — 현재 레벨 값, 미습득이면 1레벨 값 ("모든 포탑의 공격력이 N 증가합니다")
        [SerializeField] private TextMeshProUGUI detailLevelText;
        [Tooltip("습득 버튼 안 비용 줄(재화 아이콘 + 수량) — 만렙이면 줄을 꺼서 버튼 문구만 가운데 온다")]
        [SerializeField] private GameObject detailCostRow;
        [Tooltip("비용 옆 재화 아이콘 — 재화 이름을 글자로 쓰지 않고 아이콘 + 수량으로만 보여준다.")]
        [SerializeField] private Image detailCostIcon;
        [SerializeField] private TextMeshProUGUI detailCostText;
        [SerializeField] private Button acquireButton;
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
        }

        private void Start()
        {
            _BuildTree();
            _RefreshCoin();
            _RefreshRails(sweepToNodeId: null);
            _RefreshDetail();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
            if (acquireButton != null)
                acquireButton.onClick.RemoveListener(_OnClickAcquire);
        }
        #endregion

        public void Close()
        {
            PopupTween.PlayHide(gameObject, () => Destroy(gameObject));
        }

        // 프리팹에 박힌 정적 라벨(타이틀·레인 타이틀·출발역)을 현재 언어로 갱신한다.
        private void _ApplyStaticTexts()
        {
            if (titleText != null)
                titleText.text = LocalizeHelper.GetByKey("UI_SkillTree_Title", "스킬 트리");

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

            // 레벨업 버튼 문구(레벨업/MAX/선행 노드 필요)·효과 문구도 언어를 따른다
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

            // 가로 스크롤 폭 = 좌우 여백 + 마지막 열까지 (4열 = 1170 → 트리 판과 같아 스크롤 없음, 열이 늘면 가로 스크롤)
            treeContent.sizeDelta = new Vector2(
                ContentPaddingLeft * 2f + NodeSize + maxRow * NodePitch,
                treeContent.sizeDelta.y);
        }

        private void _CreateRail(string fromId, string toId, Vector2 fromPosition, Vector2 toPosition)
        {
            SkillTreeRailUI rail = Instantiate(railPrefab, treeContent);
            rail.transform.SetAsFirstSibling();
            rail.Init(fromId, toId);
            rail.Place(fromPosition, toPosition, RailThickness);
            _rails.Add(rail);
        }

        // 흐름 왼쪽→오른쪽: row 0(첫 노드)이 왼쪽 열. 레인이 가로 선로 한 줄 — 화력이 맨 위, 유틸이 맨 아래 (세로 중앙 기준).
        // col(분기 오프셋)은 현재 데이터가 전부 0이라 자리를 정하지 않는다 — 분기가 생기면 레인 간격을 넓혀 세로 오프셋으로 넣을 것.
        private Vector2 _GetNodePosition(SkillNodeData data)
        {
            float x = ContentPaddingLeft + NodeSize * 0.5f + data.Row * NodePitch;
            float y = (1 - (int)data.Lane) * LanePitch;

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
            _RefreshCoin();
            foreach (var node in _nodeUIs.Values)
                node.Refresh();

            // 선로 점등 스윕은 첫 습득(레벨 1) 순간에만 — 이미 점등된 선로는 그대로 둔다
            string sweepToNodeId = manager.GetLevel(_selectedData.Id) == 1 ? _selectedData.Id : null;
            _RefreshRails(sweepToNodeId);

            if (_nodeUIs.TryGetValue(_selectedData.Id, out var acquiredNode))
                acquiredNode.PlayAcquirePunch();

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

        // 선택된 노드의 계열·이름·설명·레벨·비용·습득 가능 여부를 상세 띠에 표시한다.
        // 선택이 없으면 왼쪽 첫 열의 맨 위 레인 노드를 기본으로 잡는다 — 띠가 빈 채로 열리지 않게.
        private void _EnsureSelection()
        {
            if (_selectedData != null) return;

            var manager = SkillTreeManager.Instance;
            if (manager == null) return;

            SkillNodeData first = null;

            foreach (var data in manager.GetNodes())
            {
                if (data == null) continue;

                if (first == null || data.Row < first.Row || (data.Row == first.Row && (int)data.Lane < (int)first.Lane))
                    first = data;
            }

            _selectedData = first;
        }

        private void _RefreshDetail()
        {
            _EnsureSelection();

            var manager = SkillTreeManager.Instance;
            bool hasSelection = _selectedData != null;

            if (detailPanel != null)
                detailPanel.SetActive(hasSelection);

            // 선택 링은 상세 패널이 가리키는 노드 하나만 켠다
            foreach (var node in _nodeUIs.Values)
                node.SetSelected(hasSelection && node.Data.Id == _selectedData.Id);

            if (detailIcon != null)
            {
                detailIcon.sprite = hasSelection ? _selectedData.Icon : null;
                detailIcon.enabled = hasSelection && _selectedData.Icon != null;
            }

            if (detailNameText != null)
            {
                // 이름만 — 계열 칩("화력")은 09-13 사용자 결정으로 뺐다 (레인이 가로 줄이라 위치가 계열을 말한다)
                detailNameText.text = hasSelection ? _selectedData.Name : string.Empty;
            }
            if (!hasSelection || manager == null)
            {
                if (detailDescriptionText != null) detailDescriptionText.text = string.Empty;
                if (detailLevelText != null) detailLevelText.text = string.Empty;
                if (detailCostRow != null) detailCostRow.SetActive(false);
                if (detailCostText != null) detailCostText.text = string.Empty;
                if (detailCostIcon != null) detailCostIcon.enabled = false;
                if (acquireText != null) acquireText.text = LocalizeHelper.GetByKey("UI_SkillTree_LevelUp", "레벨업");
                if (acquireButton != null) acquireButton.interactable = false;

                return;
            }

            int level = manager.GetLevel(_selectedData.Id);
            bool isMax = manager.IsMaxLevel(_selectedData.Id);
            bool arePrerequisitesMet = manager.ArePrerequisitesMet(_selectedData.Id);
            int cost = _selectedData.GetCostAtLevel(level);
            bool isAffordable = cost <= manager.AvailableCoin;

            if (detailLevelText != null)
            {
                // "LV" 대문자 — DNF 폰트에서 소문자 v가 u처럼 보인다
                detailLevelText.text = isMax ? "MAX"
                    : _selectedData.MaxLevel > 0 ? $"LV {level}/{_selectedData.MaxLevel}" : $"LV {level}";
                // 이름 아래 부제라 평소엔 흐린 INK2. 만렙은 금색이 크림 바탕에서 안 읽혀(09-13 반려) 진한 INK로 — 노드 쪽 금색 면과 달리 글자만이라 대비가 부족하다
                detailLevelText.color = isMax ? SkillTreePalette.OnSurface : SkillTreePalette.OnSurfaceMuted;
            }

            // 효과 한 줄 — 현재 레벨의 값, 미습득(레벨 0)이면 1레벨 값 (사용자 결정 09-13: "모든 포탑의 공격력이 N 증가합니다" 한 줄만).
            // 라벨·화살표·색 강조·다음 레벨 줄은 쓰지 않는다 — 레벨업하면 같은 줄의 숫자가 올라간다
            if (detailDescriptionText != null)
                detailDescriptionText.text = _selectedData.GetDescriptionAtLevel(Mathf.Max(level, 1));

            // 비용은 습득 버튼 안(문구 아래)에 아이콘 + 수량으로만 — 행동과 값이 한 자리. 만렙이면 줄을 꺼서 "MAX"만 가운데 온다
            if (detailCostRow != null)
                detailCostRow.SetActive(!isMax);
            if (detailCostText != null)
            {
                detailCostText.text = isMax ? string.Empty : cost.ToCommaString();
                // 비용 부족은 색 + 버튼 비활성으로 이중부호화 (danger 텍스트는 #E06666 — #B34040 금지)
                detailCostText.color = isAffordable ? SkillTreePalette.OnSurface : SkillTreePalette.DangerText;
            }

            if (detailCostIcon != null)
                detailCostIcon.enabled = !isMax;

            // 버튼 문구가 곧 상태다: 레벨업 / MAX / 선행 노드 필요. 0→1도 "레벨업"으로 통일 (09-13 사용자 결정 — "습득"은 쓰지 않는다)
            if (acquireText != null)
            {
                acquireText.text = isMax ? "MAX"
                    : !arePrerequisitesMet ? LocalizeHelper.GetByKey("UI_SkillTree_NeedPrerequisite", "선행 노드 필요")
                    : LocalizeHelper.GetByKey("UI_SkillTree_LevelUp", "레벨업");
            }

            if (acquireButton != null)
                acquireButton.interactable = manager.CanAcquire(_selectedData.Id);
        }

        private void _RefreshCoin()
        {
            var manager = SkillTreeManager.Instance;
            if (coinText != null && manager != null)
                coinText.text = manager.AvailableCoin.ToCommaString();
        }
        #endregion
    }
}
