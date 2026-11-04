using System.Collections.Generic;
using UnityEngine;
using TrainDefense.Game.Manager;
using UnityEngine.UI;
using TMPro;
using Cumic;
using TrainDefense.Game.Datas;
using TrainDefense.Localize;

namespace TrainDefense.Game.UI.SkillTree
{
    /// <summary>
    /// 스킬트리 팝업. 보유 재화를 표시하고 노드·선로를 런타임 생성한다.
    /// 11-02 방사형 전환: 가운데 빈 허브(프리팹 정적 배치)에서 레인 4개가 왼쪽(화력)·오른쪽(방어)·위(유틸)·아래(포격)로 뻗는다.
    /// 칸 하나 = 티어 하나 — 칸마다 최대 레벨이 있고, 만렙하면 다음 칸이 열린다 (세이브는 노드 전체 레벨).
    /// 레인 안 배치는 데이터 col이 정한다: 0 = 직진 사슬, ±1 = 첫 티어에서 대각으로 갈라지는 가지.
    /// 칸 탭 → 트리 아래 상세 띠(이름 티어 / LV. n / m / 칸 효과 + 레벨업 버튼[문구 / ◆비용])에서 그 칸의 레벨을 올린다.
    /// (리스펙 버튼은 09-11에 제거 — 강화 초기화 기능 자체를 없애기로 함)
    /// SkillTreeButton이 Resources의 Popup_SkillTree 프리팹을 Instantiate해서 띄운다.
    /// </summary>
    public class SkillTreePopupUI : MonoBehaviour
    {
        // 트리 판 1170×588, 허브 108, 방향별 첫 칸 76, 일반 칸 60. 칸 사이 틈은 어디서나 30이 되게 중심 거리를 잡는다.
        private const float HubToFirstTier = 122f;   // 54 + 30 + 38
        private const float FirstTierToNextTier = 98f;   // 38 + 30 + 30
        private const float NodeHalfSize = 30f;      // 노드 프리팹 60의 절반 — 범위 계산용
        // 11-02 사용자 "너무 따닥따닥" → 칸 간격 75→90(칸 사이 30). 판(588)보다 세로가 길어지는 건 줌·드래그(SkillTreeZoomPanUI + ScrollRect)로 본다
        private const float TierPitchHorizontal = 90f;
        private const float TierPitchVertical = 90f;
        private const float ContentPadding = 40f;    // 트리 범위 바깥 여백 — 끝 칸이 판 가장자리에 붙지 않게
        private const float RailThickness = 36f;    // 기찻길 스프라이트가 침목까지 보이는 폭 (노드 60 기준, 프리팹 타일 배율 7.3)

        // 레인 방향과 가지가 벌어지는 쪽 (col +1이 향하는 방향). 가로 레인은 위, 세로 레인은 왼쪽.
        private static readonly Dictionary<SkillTreeLane, (Vector2 direction, Vector2 branchSide)> LaneLayouts = new()
        {
            { SkillTreeLane.Firepower, (Vector2.left, Vector2.up) },
            { SkillTreeLane.Defense, (Vector2.right, Vector2.up) },
            { SkillTreeLane.Utility, (Vector2.up, Vector2.left) },
            { SkillTreeLane.Bombard, (Vector2.down, Vector2.left) },
        };

        #region Fields
        [SerializeField] private Button closeButton;
        [SerializeField] private TextMeshProUGUI coinText;
        [Header("정적 라벨 (로컬라이즈)")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI acquireText;
        [Header("트리 (런타임 생성)")]
        [SerializeField] private RectTransform treeContent;        // 가운데 기준 — 허브(프리팹 정적 배치, 누를 수 없음)가 (0, 0)에 온다
        [SerializeField] private SkillTreeNodeUI nodePrefab;
        [SerializeField] private SkillTreeNodeUI rootNodePrefab;   // 레인별 시작 칸 — 일반 칸보다 크다
        [SerializeField] private SkillTreeRailUI railPrefab;
        [Header("상세 띠 (트리 아래 — 세로 레이아웃 그룹: 이름 티어 / 티어 n / m / 효과. 라벨·화살표 없음)")]
        [SerializeField] private GameObject detailPanel;           // 선택이 없을 때만 숨김 (열자마자 첫 노드의 다음 티어를 기본 선택)
        [SerializeField] private Image detailIcon;
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailDescriptionText;      // 효과 한 줄 — 이 칸이 주는 값 ("모든 포탑의 공격력이 0.5% 증가합니다")
        [SerializeField] private TextMeshProUGUI detailLevelText;
        [Tooltip("습득 버튼 안 비용 줄(재화 아이콘 + 수량) — 이미 산 티어면 줄을 꺼서 버튼 문구만 가운데 온다")]
        [SerializeField] private GameObject detailCostRow;
        [Tooltip("비용 옆 재화 아이콘 — 재화 이름을 글자로 쓰지 않고 아이콘 + 수량으로만 보여준다.")]
        [SerializeField] private Image detailCostIcon;
        [SerializeField] private TextMeshProUGUI detailCostText;
        [SerializeField] private Button acquireButton;
        [Tooltip("만렙 칸을 고르면 켜지는 금색 테두리 — 칸의 만렙 링과 같은 표시")]
        [SerializeField] private GameObject acquireMasteredRing;
        #endregion

        private readonly Dictionary<(string nodeId, int tier), SkillTreeNodeUI> _nodeUIs = new();
        private readonly List<SkillTreeRailUI> _rails = new();
        private SkillNodeData _selectedData;
        private int _selectedTier;

        #region LifeCycle
        private void OnEnable()
        {
            Localization.OnLanguageChanged += _ApplyStaticTexts;
            Localization.OnInitialized += _ApplyStaticTexts;
            _ApplyStaticTexts();

            PopupTween.PlayShow(gameObject);
            AnalyticsManager.LogScreenView("skill_tree");
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
            _RefreshRails(sweepToNodeId: null, sweepToTier: 0);
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

        // 프리팹에 박힌 정적 라벨(타이틀)을 현재 언어로 갱신한다.
        private void _ApplyStaticTexts()
        {
            if (titleText != null)
                titleText.text = LocalizeHelper.GetByKey("UI_SkillTree_Title", "스킬 트리");

            // 레벨업 버튼 문구(레벨업/MAX/선행 노드 필요)·티어 줄·효과 문구도 언어를 따른다
            _RefreshDetail();
        }

        #region Build
        private void _BuildTree()
        {
            var manager = SkillTreeManager.Instance;
            if (manager == null || nodePrefab == null || rootNodePrefab == null || treeContent == null) return;

            // 1) 허브 기준 좌표를 먼저 모은다. 허브는 판 정중앙에 둔다 — 레인 길이가 달라도 대칭이 읽히는 쪽이 낫다.
            var positions = new Dictionary<(string nodeId, int tier), Vector2>();
            var occupiedCells = new Dictionary<Vector2, (string nodeId, int tier)>();
            foreach (var data in manager.GetNodes())
            {
                if (data == null) continue;

                for (int tier = 1; tier <= data.TierCount; tier++)
                {
                    Vector2 position = _GetTierPosition(data, tier);
                    positions[(data.Id, tier)] = position;

                    // 같은 레인에서 col이 겹치는 노드(예: 가지 둘 다 col 0)는 같은 자리에 포개진다 — 데이터 실수를 조용히 넘기지 않는다
                    if (occupiedCells.TryGetValue(position, out var occupant))
                        Debug.LogWarning($"[SkillTreePopupUI] 노드 칸 겹침: {data.Id} {tier}와 {occupant.nodeId} {occupant.tier}가 같은 자리 {position} — DB의 lane/row/col 확인", this);
                    else
                        occupiedCells[position] = (data.Id, tier);
                }
            }

            // Content 크기 = 트리 범위 + 여백 (허브가 가운데에 오도록 상하좌우 모두 큰 쪽 기준 대칭). 뷰포트보다 작으면 뷰포트 크기 — ScrollRect가 이동 범위를 여기서 얻는다
            float halfWidth = 0f;
            float halfHeight = 0f;
            foreach (var position in positions.Values)
            {
                halfWidth = Mathf.Max(halfWidth, Mathf.Abs(position.x));
                halfHeight = Mathf.Max(halfHeight, Mathf.Abs(position.y));
            }
            Vector2 viewportSize = treeContent.parent is RectTransform viewportRect ? viewportRect.rect.size : Vector2.zero;
            treeContent.sizeDelta = new Vector2(
                Mathf.Max(viewportSize.x, (halfWidth + NodeHalfSize + ContentPadding) * 2f),
                Mathf.Max(viewportSize.y, (halfHeight + NodeHalfSize + ContentPadding) * 2f));

            // 2) 칸 생성
            foreach (var pair in positions)
            {
                var data = manager.GetNode(pair.Key.nodeId);
                bool isLaneStart = data.Row == 0 && pair.Key.tier == 1;
                SkillTreeNodeUI node = Instantiate(isLaneStart ? rootNodePrefab : nodePrefab, treeContent);
                node.RectTransform.anchoredPosition = pair.Value;
                node.Init(data, pair.Key.tier, _OnSelectNode);
                _nodeUIs[pair.Key] = node;
            }

            // 3) 선로 — 허브→첫 티어(선행 없는 노드), 선행 노드 I→가지 I, 같은 노드의 티어 n-1→n. 칸 아래에 깔리도록 맨 앞 형제로 보낸다.
            if (railPrefab == null) return;

            foreach (var data in manager.GetNodes())
            {
                if (data == null || data.TierCount == 0) continue;

                Vector2 firstTierPosition = positions[(data.Id, 1)];
                if (data.Prerequisites == null || data.Prerequisites.Length == 0)
                {
                    _CreateRail(null, data.Id, 1, Vector2.zero, firstTierPosition);
                }
                else
                {
                    foreach (string fromId in data.Prerequisites)
                    {
                        if (!positions.TryGetValue((fromId, 1), out Vector2 fromPosition)) continue;

                        _CreateRail(fromId, data.Id, 1, fromPosition, firstTierPosition);
                    }
                }

                for (int tier = 2; tier <= data.TierCount; tier++)
                {
                    _CreateRail(data.Id, data.Id, tier,
                        positions[(data.Id, tier - 1)],
                        positions[(data.Id, tier)]);
                }
            }
        }

        private void _CreateRail(string fromId, string toId, int toTier, Vector2 fromPosition, Vector2 toPosition)
        {
            SkillTreeRailUI rail = Instantiate(railPrefab, treeContent);
            rail.transform.SetAsFirstSibling();
            rail.Init(fromId, toId, toTier);
            rail.Place(fromPosition, toPosition, RailThickness);
            _rails.Add(rail);
        }

        // 허브 기준 칸 위치. row 0 노드의 I는 레인 방향으로 허브 바로 옆. 그 뒤 칸(k = 1, 2, …)은 col이 정한 슬롯을 따라간다:
        //   col 0  → 레인 방향으로 직진
        //   col ±1 → 첫 티어에서 대각으로 한 칸 벌어진 뒤 레인 방향으로 이어진다
        private static Vector2 _GetTierPosition(SkillNodeData data, int tier)
        {
            var (direction, branchSide) = LaneLayouts[data.Lane];

            bool isLaneRoot = data.Row == 0;
            if (isLaneRoot && tier == 1)
                return direction * HubToFirstTier;

            bool isHorizontalLane = direction.x != 0f;
            float alongPitch = isHorizontalLane ? TierPitchHorizontal : TierPitchVertical;   // 레인 방향으로 한 칸
            float sidePitch = isHorizontalLane ? TierPitchVertical : TierPitchHorizontal;    // 레인과 직각으로 한 칸

            int step = isLaneRoot ? tier - 1 : tier;
            float distance = HubToFirstTier + FirstTierToNextTier + (step - 1) * alongPitch;
            return direction * distance + branchSide * (data.Col * sidePitch);
        }
        #endregion

        #region Refresh
        private void _OnSelectNode(SkillNodeData data, int tier)
        {
            _selectedData = data;
            _selectedTier = tier;
            _RefreshDetail();
        }

        private void _OnClickAcquire()
        {
            var manager = SkillTreeManager.Instance;
            if (manager == null || _selectedData == null) return;
            // 버튼은 "레벨을 올릴 차례인 칸"이 선택됐을 때만 살아 있지만, 재화·레벨이 바뀐 직후를 대비해 한 번 더 확인
            if (!_selectedData.IsTierInProgress(manager.GetLevel(_selectedData.Id), _selectedTier)) return;
            if (!manager.TryAcquire(_selectedData.Id)) return;

            // 습득 성공 → 포인트·전 칸·선로·상세 갱신 (포인트가 줄어 다른 칸의 습득 가능 여부도 바뀜)
            _RefreshCoin();
            foreach (var node in _nodeUIs.Values)
                node.Refresh();

            // 칸의 첫 레벨을 샀을 때만 들어오는 선로를 점등 스윕 — 이미 점등된 선로는 그대로 둔다
            int level = manager.GetLevel(_selectedData.Id);
            bool isFirstLevelInTier = _selectedData.GetLevelInTier(level, _selectedTier) == 1;
            _RefreshRails(isFirstLevelInTier ? _selectedData.Id : null, _selectedTier);

            if (_nodeUIs.TryGetValue((_selectedData.Id, _selectedTier), out var acquiredNode))
                acquiredNode.PlayAcquirePunch();

            // 칸을 만렙했고 사슬이 남았으면 다음 칸으로 선택을 옮겨 연속 구매가 되게 한다
            if (_selectedData.IsTierMastered(level, _selectedTier) && _selectedTier < _selectedData.TierCount)
                _selectedTier++;

            _RefreshDetail();
        }

        private void _RefreshRails(string sweepToNodeId, int sweepToTier)
        {
            var manager = SkillTreeManager.Instance;
            if (manager == null) return;

            foreach (var rail in _rails)
            {
                int level = manager.GetLevel(rail.ToNodeId);
                // 도착 칸에 1레벨이라도 들어가면 점등
                int tierStartLevel = manager.GetNode(rail.ToNodeId).GetLevelsBeforeTier(rail.ToTier);
                bool isLit = level > tierStartLevel;
                bool isSweep = isLit && rail.ToNodeId == sweepToNodeId && rail.ToTier == sweepToTier;
                rail.SetLit(isLit, animated: isSweep);
            }
        }

        // 선택이 없으면 화력 레인 뿌리 노드의 "다음에 살 티어"(다 샀으면 마지막)를 기본으로 잡는다 — 띠가 빈 채로 열리지 않게.
        private void _EnsureSelection()
        {
            if (_selectedData != null) return;

            var manager = SkillTreeManager.Instance;
            if (manager == null) return;

            SkillNodeData first = null;
            foreach (var data in manager.GetNodes())
            {
                if (data == null || data.TierCount == 0) continue;

                // 레인 → 행 → 직진(col 0) 우선. DB 나열 순서와 무관하게 같은 칸이 뽑혀야 한다
                if (first == null || _CompareNodeOrder(data, first) < 0)
                    first = data;
            }

            if (first == null) return;

            _selectedData = first;
            _selectedTier = first.GetCurrentTier(manager.GetLevel(first.Id));
        }

        private static int _CompareNodeOrder(SkillNodeData a, SkillNodeData b)
        {
            if (a.Lane != b.Lane) return (int)a.Lane - (int)b.Lane;
            if (a.Row != b.Row) return a.Row - b.Row;

            return Mathf.Abs(a.Col) - Mathf.Abs(b.Col);
        }

        private void _RefreshDetail()
        {
            _EnsureSelection();

            var manager = SkillTreeManager.Instance;
            bool hasSelection = _selectedData != null;

            if (detailPanel != null)
                detailPanel.SetActive(hasSelection);

            // 선택 링은 상세 패널이 가리키는 칸 하나만 켠다
            foreach (var node in _nodeUIs.Values)
                node.SetSelected(hasSelection && node.Data.Id == _selectedData.Id && node.Tier == _selectedTier);

            if (detailIcon != null)
            {
                detailIcon.sprite = hasSelection ? _selectedData.Icon : null;
                detailIcon.enabled = hasSelection && _selectedData.Icon != null;
            }

            if (detailNameText != null)
            {
                // "공격력 강화 II" — 이름 + 로마자 티어. 계열 칩은 09-13 사용자 결정으로 뺐다 (방향이 계열을 말한다)
                detailNameText.text = hasSelection
                    ? $"{_selectedData.Name} {SkillTreeNodeUI.ToRomanNumeral(_selectedTier)}"
                    : string.Empty;
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
                if (acquireMasteredRing != null) acquireMasteredRing.SetActive(false);

                return;
            }

            int level = manager.GetLevel(_selectedData.Id);
            bool isAcquired = _selectedData.IsTierMastered(level, _selectedTier);
            bool isNext = _selectedData.IsTierInProgress(level, _selectedTier);
            bool arePrerequisitesMet = manager.ArePrerequisitesMet(_selectedData.Id);
            bool isChainMastered = isAcquired && _selectedTier == _selectedData.TierCount;
            SkillNodeTier tierData = _selectedData.GetTier(_selectedTier);
            int cost = tierData.GetCost(_selectedData.GetLevelInTier(level, _selectedTier));
            bool isAffordable = cost <= manager.AvailableCoin;

            if (detailLevelText != null)
            {
                // "LV. 2 / 5" — 이 칸의 레벨. 대문자: DNF 폰트에서 소문자 v가 u처럼 보인다. 이름 아래 부제라 평소엔 흐린 INK2. 사슬 끝 칸은 진한 INK (금색은 크림 바탕에서 안 읽혀 09-13 반려)
                detailLevelText.text = $"LV. {_selectedData.GetLevelInTier(level, _selectedTier)} / {tierData.MaxLevel}";
                detailLevelText.color = isChainMastered ? SkillTreePalette.OnSurface : SkillTreePalette.OnSurfaceMuted;
            }

            // 효과 한 줄 — 이 칸이 주는 값. 0레벨이면 "0.5%", 올리는 중이면 "0.5% → 1%", 만렙 칸이면 칸 값만 (11-03 사용자 결정)
            if (detailDescriptionText != null)
                detailDescriptionText.text = _selectedData.GetTierDescription(level, _selectedTier, includesNextLevel: !isAcquired);

            // 비용은 습득 버튼 안(문구 아래)에 아이콘 + 수량으로만 — 행동과 값이 한 자리. 이미 산 칸이면 줄을 꺼서 "MAX"만 가운데 온다
            if (detailCostRow != null)
                detailCostRow.SetActive(!isAcquired);
            if (detailCostText != null)
            {
                detailCostText.text = isAcquired ? string.Empty : cost.ToCommaString();
                // 비용 부족은 색 + 버튼 비활성으로 이중부호화 (danger 텍스트는 #E06666 — #B34040 금지)
                detailCostText.color = isAffordable ? SkillTreePalette.OnSurface : SkillTreePalette.DangerText;
            }

            if (detailCostIcon != null)
                detailCostIcon.enabled = !isAcquired;

            // 버튼 문구가 곧 상태다: 레벨업 / MAX(이미 산 칸) / 잠김(앞 티어나 선행 노드 미습득). 0→1도 "레벨업"으로 통일 (09-13 사용자 결정)
            if (acquireText != null)
            {
                acquireText.text = isAcquired ? "MAX"
                    : !(isNext && arePrerequisitesMet) ? LocalizeHelper.GetByKey("UI_SkillTree_NeedPrerequisite", "잠김")
                    : LocalizeHelper.GetByKey("UI_SkillTree_LevelUp", "레벨업");
            }

            if (acquireButton != null)
                acquireButton.interactable = isNext && manager.CanAcquire(_selectedData.Id);

            if (acquireMasteredRing != null)
                acquireMasteredRing.SetActive(isAcquired);
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
