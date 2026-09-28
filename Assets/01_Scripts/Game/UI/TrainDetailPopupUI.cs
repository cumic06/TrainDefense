using System.Text;
using TMPro;
using TrainDefense.Game.Datas;
using TrainDefense.Localize;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    public class TrainDetailPopupUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI trainNameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI skillsText;
        [SerializeField]
        [Tooltip("상점용 작은 표시 — 설명·스킬을 숨기고(카드에 이미 적혀 있음) 스탯을 두 열로 놓는다")]
        private bool compactLayout;

        // 강화 카드에서 바뀌는 스탯의 새 값 색(#E2DCD1 배경 위 대비 5.8:1).
        private const string UPGRADED_VALUE_COLOR = "#1B5E20";

        private Train _train;
        // 상점 강화 카드 미리보기 — 언어 변경 시 다시 그리기 위해 보관.
        private System.Func<(string label, string value)[]> _getUpgradedDetails;
        // 상점 카드 미리보기(아직 없는 포탑) — 언어 변경 시 다시 그리기 위해 보관.
        private TrainData _previewTrainData;
        private Train _previewPromotionSource;

        // 화면 밖으로 나간 팝업을 안쪽으로 당길 때 쓰는 모서리 버퍼(매번 할당하지 않도록 재사용).
        private static readonly Vector3[] _cornerBuffer = new Vector3[4];
        // 화면 끝과 팝업 사이 최소 여백(캔버스 단위). 끝에 딱 붙으면 잘려 보인다.
        private const float SCREEN_MARGIN = 16f;
        // PlaceBeside에서 기준 요소(카드)와 팝업 사이 간격(캔버스 단위).
        private const float ANCHOR_GAP = 12f;

        // 이 요소의 아래쪽 가장자리를 팝업 세로 가운데로 삼는다(상점: 카드 판과 기차 그림을 나누는 선). 없으면 기준 요소와 윗선을 맞춘다.
        private RectTransform _dividerLine;
        // 스탯에서 뺄 줄의 번역 키·폴백(상점 팝업은 크리티컬을 뺀다).
        private (string key, string fallback)[] _hiddenStats = System.Array.Empty<(string key, string fallback)>();

        private void Awake()
        {
            if (!compactLayout)
                return;

            if (descriptionText != null)
                descriptionText.gameObject.SetActive(false);
            if (skillsText != null)
                skillsText.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            Localization.OnInitialized += _RefreshLocalizedTexts;
            Localization.OnLanguageChanged += _RefreshLocalizedTexts;

            PopupTween.PlayShow(gameObject);
        }

        private void OnDisable()
        {
            Localization.OnInitialized -= _RefreshLocalizedTexts;
            Localization.OnLanguageChanged -= _RefreshLocalizedTexts;
        }

        // 떠 있는 동안 초기화 완료·언어 변경 시, 마지막으로 표시한 train으로 다시 그린다.
        private void _RefreshLocalizedTexts()
        {
            if (_train != null && _getUpgradedDetails != null)
                ShowUpgradePreview(_train, _getUpgradedDetails);
            else if (_train != null)
                Show(_train);
            else if (_previewTrainData != null)
                ShowPreview(_previewTrainData, _previewPromotionSource);
        }

        public void Show(Train train)
        {
            if (train == null) return;
            _train = train;
            _getUpgradedDetails = null;
            _previewTrainData = null;
            _previewPromotionSource = null;

            var data = train.TrainData;
            if (data != null)
            {
                if (trainNameText != null)
                    trainNameText.text = data.Name;

                if (descriptionText != null)
                    descriptionText.text = data.Description;
            }

            if (statsText != null)
                statsText.text = _BuildStatsText(train.GetStatDetails(), null);

            if (skillsText != null)
            {
                var sb = new StringBuilder();

                // TrainData 전체 스킬이 아니라, 이 인스턴스에 실제 적용된 스킬만 표시(마스크 반영).
                string passiveLabel = LocalizeHelper.GetByKey("skill_type_passive", "패시브");

                foreach (var (name, description) in train.GetAppliedSkillDisplays())
                {
                    sb.AppendLine($"[{passiveLabel}] {name}");
                    if (!string.IsNullOrEmpty(description))
                        sb.AppendLine(description);
                }

                skillsText.text = sb.ToString().TrimEnd();
            }

            _Open();
        }

        /// <summary>
        /// 아직 편성에 없는 포탑의 스탯을 띄운다(상점 새 포탑·승격 카드).
        /// promotionSource가 있으면 그 포탑의 누적 강화를 승계한 승격 후 값, 없으면 생성 직후 값.
        /// 스킬은 카드에 이미 적혀 있어 비운다.
        /// </summary>
        public void ShowPreview(TrainData trainData, Train promotionSource)
        {
            if (trainData == null) return;
            _train = null;
            _getUpgradedDetails = null;
            _previewTrainData = trainData;
            _previewPromotionSource = promotionSource;

            if (trainNameText != null)
                trainNameText.text = trainData.Name;

            if (descriptionText != null)
                descriptionText.text = trainData.Description;

            if (statsText != null)
                statsText.text = _BuildStatsText(Train.GetPreviewStatDetails(trainData, promotionSource), null);

            if (skillsText != null)
                skillsText.text = string.Empty;

            _Open();
        }

        /// <summary>
        /// 보유 포탑에 강화 카드를 샀을 때의 변화를 띄운다 — 바뀌는 스탯은 맨 위에 "현재 → 구매 후"로.
        /// getUpgradedDetails는 구매 후 스탯(TrainStatUpgradeChoice.GetUpgradedStatDetails).
        /// </summary>
        public void ShowUpgradePreview(Train train, System.Func<(string label, string value)[]> getUpgradedDetails)
        {
            if (train == null || getUpgradedDetails == null) return;
            Show(train);
            _getUpgradedDetails = getUpgradedDetails;

            if (statsText != null)
                statsText.text = _BuildStatsText(train.GetStatDetails(), getUpgradedDetails());
        }

        // 상점 카드는 이름이 카드에 적혀 있어 숨기고, 기차 그림의 칸은 보여 준다.
        public void SetNameVisible(bool visible)
        {
            if (trainNameText != null)
                trainNameText.gameObject.SetActive(visible);
        }

        public void SetDividerLine(RectTransform dividerLine)
        {
            _dividerLine = dividerLine;
        }

        public void SetHiddenStats(params (string key, string fallback)[] hiddenStats)
        {
            _hiddenStats = hiddenStats ?? System.Array.Empty<(string key, string fallback)>();
        }

        /// <summary>
        /// anchor 옆에 띄운다 — preferLeft면 왼쪽 우선, 아니면 오른쪽 우선, 화면 밖으로 나가면 반대쪽.
        /// 세로는 구분선(SetDividerLine)이 있으면 그 선에 걸치게, 없으면 anchor와 윗선을 맞춘다.
        /// Show/ShowPreview 뒤에 부른다 — 높이가 내용(ContentSizeFitter)으로 정해지므로 레이아웃을 먼저 확정한다.
        /// </summary>
        public void PlaceBeside(RectTransform anchor, bool preferLeft = false)
        {
            if (anchor == null)
                return;

            anchor.GetWorldCorners(_cornerBuffer);
            PlaceBeside(_cornerBuffer[0], _cornerBuffer[2], preferLeft);
        }

        /// <summary>
        /// 캔버스 월드 좌표로 준 영역(anchorMin 왼쪽 아래, anchorMax 오른쪽 위) 옆에 띄운다. RectTransform이 없는 대상(상점 기차 그림의 칸)용.
        /// </summary>
        public void PlaceBeside(Vector3 anchorMin, Vector3 anchorMax, bool preferLeft = false)
        {
            var rectTransform = (RectTransform)transform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            float unit = canvas.rootCanvas.transform.lossyScale.x;
            _GetScreenBounds(canvas, unit, out Vector3 screenMin, out Vector3 screenMax);

            rectTransform.GetWorldCorners(_cornerBuffer);
            Vector2 size = _cornerBuffer[2] - _cornerBuffer[0];

            float bottom;
            if (_dividerLine != null)
            {
                _dividerLine.GetWorldCorners(_cornerBuffer);
                bottom = _cornerBuffer[0].y - size.y * 0.5f;
            }
            else
            {
                bottom = anchorMax.y - size.y;
            }

            float rightSideLeft = anchorMax.x + ANCHOR_GAP * unit;
            float leftSideLeft = anchorMin.x - ANCHOR_GAP * unit - size.x;
            float left = preferLeft ? leftSideLeft : rightSideLeft;

            // 화면 밖으로 나가면 반대쪽으로 넘긴다 — 단 팝업이 anchor와 세로로 안 겹치면(기차 칸 위에 뜰 때) 넘기지 않고 화면 안으로 당기기만 한다.
            bool overlapsVertically = bottom < anchorMax.y && bottom + size.y > anchorMin.y;
            if (overlapsVertically)
            {
                if (preferLeft && leftSideLeft < screenMin.x)
                    left = rightSideLeft;
                else if (!preferLeft && rightSideLeft + size.x > screenMax.x)
                    left = leftSideLeft;
            }

            Vector2 pivot = rectTransform.pivot;
            rectTransform.position = new Vector3(left + size.x * pivot.x, bottom + size.y * pivot.y, rectTransform.position.z);
            _ClampInsideScreen(rectTransform);
        }

        // 화면(루트 캔버스)에서 SCREEN_MARGIN만큼 안쪽 영역 밖으로 나간 만큼 당긴다.
        private void _ClampInsideScreen(RectTransform rectTransform)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            _GetScreenBounds(canvas, canvas.rootCanvas.transform.lossyScale.x, out Vector3 screenMin, out Vector3 screenMax);

            rectTransform.GetWorldCorners(_cornerBuffer);
            Vector3 popupMin = _cornerBuffer[0];
            Vector3 popupMax = _cornerBuffer[2];

            Vector3 offset = Vector3.zero;
            if (popupMin.x < screenMin.x) offset.x = screenMin.x - popupMin.x;
            else if (popupMax.x > screenMax.x) offset.x = screenMax.x - popupMax.x;
            if (popupMax.y > screenMax.y) offset.y = screenMax.y - popupMax.y;
            else if (popupMin.y < screenMin.y) offset.y = screenMin.y - popupMin.y;

            rectTransform.position += offset;
        }

        private static void _GetScreenBounds(Canvas canvas, float unit, out Vector3 screenMin, out Vector3 screenMax)
        {
            ((RectTransform)canvas.rootCanvas.transform).GetWorldCorners(_cornerBuffer);
            Vector3 margin = new Vector3(SCREEN_MARGIN * unit, SCREEN_MARGIN * unit, 0f);
            screenMin = _cornerBuffer[0] + margin;
            screenMax = _cornerBuffer[2] - margin;
        }

        public void Hide()
        {
            PopupTween.PlayHide(gameObject, () => gameObject.SetActive(false));
        }

        // 퇴장 트윈 중에 다시 띄우면 이미 활성이라 OnEnable(등장 연출)이 안 돌고, 남은 퇴장 트윈이 끝나며 꺼져 버린다.
        // 그 경우엔 등장 연출을 직접 다시 건다(PlayShow가 퇴장 트윈을 끊는다).
        private void _Open()
        {
            if (gameObject.activeSelf)
                PopupTween.PlayShow(gameObject);
            else
                gameObject.SetActive(true);
        }

        // upgradedStats가 있으면(강화 카드) 값이 바뀌는 줄을 맨 위에 "현재 → 구매 후"로 한 줄씩 놓고, 나머지를 그 아래에 둔다.
        // compactLayout이면 나머지 줄을 두 열로 놓는다.
        private string _BuildStatsText((string label, string value)[] stats, (string label, string value)[] upgradedStats)
        {
            // 스탯 쪽(GetStatDetails)과 같은 키·폴백으로 라벨을 만들어 비교한다 — 번역 전이면 폴백 문자열이 라벨이다.
            var hiddenLabels = new string[_hiddenStats.Length];
            for (int i = 0; i < _hiddenStats.Length; i++)
                hiddenLabels[i] = LocalizeHelper.GetByKey(_hiddenStats[i].key, _hiddenStats[i].fallback);

            var changedLines = new System.Collections.Generic.List<string>();
            var otherLines = new System.Collections.Generic.List<string>();
            foreach (var (label, value) in stats)
            {
                if (System.Array.IndexOf(hiddenLabels, label) >= 0)
                    continue;

                string upgradedValue = null;
                if (upgradedStats != null)
                    foreach (var upgraded in upgradedStats)
                        if (upgraded.label == label) { upgradedValue = upgraded.value; break; }

                if (upgradedValue != null && upgradedValue != value)
                    changedLines.Add($"{label}: {value} → <b><color={UPGRADED_VALUE_COLOR}>{upgradedValue}</color></b>");
                else
                    otherLines.Add($"{label}: {value}");
            }

            // 바뀌는 줄이 앞칸부터 채운다(두 열이면 왼쪽 위).
            var lines = new System.Collections.Generic.List<string>(changedLines);
            lines.AddRange(otherLines);

            if (!compactLayout || statsText == null)
                return string.Join("\n", lines);

            // 두 열 — 오른쪽 열은 왼쪽 열 가장 긴 칸 + 간격에서 시작하고, 팝업 폭을 두 열에 맞춘다(반 칸 고정이면 "현재 → 구매 후"가 넘친다).
            float leftWidth = 0f, rightWidth = 0f;
            for (int i = 0; i < lines.Count; i++)
            {
                float width = statsText.GetPreferredValues(lines[i]).x;
                if (i % 2 == 0) leftWidth = Mathf.Max(leftWidth, width);
                else rightWidth = Mathf.Max(rightWidth, width);
            }
            int rightColumnStart = Mathf.CeilToInt(leftWidth + COLUMN_GAP);

            var sb = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                sb.Append(lines[i]);
                if (i % 2 == 0 && i + 1 < lines.Count)
                    sb.Append("<pos=").Append(rightColumnStart).Append('>').Append(lines[++i]);
                sb.Append('\n');
            }

            var layoutGroup = GetComponentInChildren<VerticalLayoutGroup>(true);
            float horizontalPadding = layoutGroup != null ? layoutGroup.padding.horizontal : 0f;
            var rectTransform = (RectTransform)transform;
            float contentWidth = rightWidth > 0f ? rightColumnStart + rightWidth : leftWidth;
            rectTransform.sizeDelta = new Vector2(Mathf.Ceil(contentWidth) + horizontalPadding, rectTransform.sizeDelta.y);

            return sb.ToString().TrimEnd();
        }

        // 두 열 사이 간격(캔버스 단위).
        private const float COLUMN_GAP = 28f;
    }
}
