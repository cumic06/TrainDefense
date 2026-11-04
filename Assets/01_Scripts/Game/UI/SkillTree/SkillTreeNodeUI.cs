using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game.UI.SkillTree
{
    /// <summary>
    /// 스킬트리 노드 한 칸 = 한 노드의 티어 하나 ("공격력 I → II → III" 낱개 칸, 칸마다 최대 레벨이 따로 있고 만렙하면 다음 칸이 열린다).
    /// 상태(잠김/획득가능/습득/만렙) 비주얼과 선택 알림, 습득 펀치 연출을 담당한다.
    /// 설명·비용·습득 버튼은 팝업(SkillTreePopupUI) 하단 상세 패널 몫이다.
    /// 상태는 색으로만 구분하지 않는다 — 로마자 티어 글자가 항상 병행한다 (이중부호화).
    /// </summary>
    public class SkillTreeNodeUI : MonoBehaviour
    {
        // DESIGN.md 습득 펀치: 1.0 → 1.25 → 1.0, 240ms. vibrato 불사용(저프레임 언더샘플링 함정) — 시퀀스로 구성.
        private const float AcquirePunchScale = 1.25f;
        private const float AcquirePunchDuration = 0.24f;

        // 살 수 있는 칸·코인이 모자란 칸의 빛. 꺼지지 않고 약하게 숨 쉬듯 움직인다 (상점 등급 빛과 같은 코사인 펄스)
        private const float GlowPulsePeriod = 2.4f;
        private const float GlowMinStrength = 0.55f;
        private const float BuyableGlowMaxAlpha = 0.3f;
        private const float UnaffordableGlowMaxAlpha = 0.45f;
        private const float SelectedGlowStrength = 0.5f;   // 선택 테두리와 색이 부딪히지 않게 줄인다

        private static readonly string[] RomanNumerals = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };

        #region Fields
        [SerializeField] private Image backgroundImage;   // 노드 배경 (TurretSelectSlotBg 9-slice 권장)
        [SerializeField] private Image borderImage;       // 테두리 링 (SkillTreeNodeRingThin — 칸 모양을 그대로 따라가는 9-slice 링) — 선택 흰색 / 칸 만렙 골드 / 획득 가능 accent / 코인 부족 빨강
        [SerializeField] private Image glowImage;         // 테두리 바깥 빛 (SkillTreeNodeGlow) — 살 수 있음 / 코인 부족에만 켠다
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI levelText;   // 로마자 티어 (I~)
        [SerializeField] private Button selectButton;
        #endregion

        // 잠김 아이콘 = 어두운 실루엣 틴트 (도감 미발견 컨벤션). 회색 곱 틴트는 09-11에 반려됨 — 검은 실루엣 유지.
        // ⚠️ SpriteGrayscale 머티리얼 금지 — Stencil/_ClipRect가 없어 UI Mask/RectMask2D 클리핑을 뚫고 그려진다.
        private static readonly Color LockedIconSilhouette = new Color(0.08f, 0.09f, 0.12f, 1f);
        private static readonly Color UnaffordableIconTint = new Color(0.6f, 0.6f, 0.6f, 1f);   // 코인이 모자란 칸은 비활성처럼 어둡게 (그림은 알아볼 수 있게)

        private SkillNodeData _data;
        private int _tier;
        private Action<SkillNodeData, int> _onSelect;
        private Tween _punchTween;
        private bool _isSelected;
        private float _glowMaxAlpha;

        public SkillNodeData Data => _data;
        /// <summary>이 칸이 나타내는 티어 (1부터).</summary>
        public int Tier => _tier;
        public RectTransform RectTransform => (RectTransform)transform;

        /// <summary>티어 번호의 로마자 표기 (1 → I). 범위를 넘으면 아라비아 숫자.</summary>
        public static string ToRomanNumeral(int tier)
            => tier >= 1 && tier <= RomanNumerals.Length ? RomanNumerals[tier - 1] : tier.ToString();

        private void Awake()
        {
            if (selectButton != null)
                selectButton.onClick.AddListener(_OnClickSelect);
        }

        private void Update()
        {
            if (_glowMaxAlpha <= 0f) return;

            _UpdateGlowPulse();
        }

        private void _UpdateGlowPulse()
        {
            float wave = (1f - Mathf.Cos(Time.unscaledTime * Mathf.PI * 2f / GlowPulsePeriod)) * 0.5f;
            Color color = glowImage.color;
            color.a = _glowMaxAlpha * Mathf.Lerp(GlowMinStrength, 1f, wave) * (_isSelected ? SelectedGlowStrength : 1f);
            glowImage.color = color;
        }

        private void OnDestroy()
        {
            if (selectButton != null)
                selectButton.onClick.RemoveListener(_OnClickSelect);

            _punchTween?.Kill();
        }

        public void Init(SkillNodeData data, int tier, Action<SkillNodeData, int> onSelect)
        {
            _data = data;
            _tier = tier;
            _onSelect = onSelect;

            if (iconImage != null)
            {
                iconImage.sprite = data.Icon;
                iconImage.enabled = data.Icon != null;
                iconImage.rectTransform.sizeDelta *= data.IconScale;
            }

            if (levelText != null)
                levelText.text = ToRomanNumeral(tier);

            Refresh();
        }

        /// <summary>현재 레벨·습득 가능 여부에 맞춰 상태 비주얼을 갱신한다.</summary>
        public void Refresh()
        {
            var manager = SkillTreeManager.Instance;
            if (manager == null || _data == null) return;

            int level = manager.GetLevel(_data.Id);
            bool isAcquired = _data.IsTierMastered(level, _tier);
            bool isNext = _data.IsTierInProgress(level, _tier);     // 레벨을 올릴 차례인 칸
            bool arePrerequisitesMet = manager.ArePrerequisitesMet(_data.Id);
            bool isOpen = isAcquired || (isNext && arePrerequisitesMet);   // 열린 칸 — 탭하면 상세가 뜬다
            bool canAcquire = isNext && manager.CanAcquire(_data.Id);
            bool isUnaffordable = isNext && arePrerequisitesMet && !canAcquire;  // 열렸는데 코인이 모자란 칸

            if (backgroundImage != null)
                backgroundImage.color = isOpen && !isUnaffordable ? SkillTreePalette.SurfaceRaised : SkillTreePalette.SurfaceLine;

            // 테두리 링은 하나 — 선택이면 흰 글로우(상세 패널이 가리키는 칸), 칸 만렙이면 골드, 살 수 있으면 accent, 코인이 모자라면 빨강.
            // 두 상태가 겹치면 선택이 이긴다 (습득 가능 여부는 상세 패널의 버튼이 말해준다)
            if (borderImage != null)
            {
                borderImage.enabled = _isSelected || isAcquired || canAcquire || isUnaffordable;
                borderImage.color = _isSelected ? SkillTreePalette.Selected
                    : isAcquired ? SkillTreePalette.Mastered
                    : canAcquire ? SkillTreePalette.Accent
                    : SkillTreePalette.Unaffordable;
            }

            if (glowImage != null)
            {
                _glowMaxAlpha = canAcquire ? BuyableGlowMaxAlpha : isUnaffordable ? UnaffordableGlowMaxAlpha : 0f;
                glowImage.enabled = _glowMaxAlpha > 0f;
                if (_glowMaxAlpha > 0f)
                {
                    glowImage.color = canAcquire ? SkillTreePalette.Accent : SkillTreePalette.Unaffordable;
                    _UpdateGlowPulse();
                }
            }

            // 잠김 = 어두운 실루엣 (색으로만 구분 금지 — 로마자 글자가 병행)
            if (iconImage != null)
                iconImage.color = !isOpen ? LockedIconSilhouette : isUnaffordable ? UnaffordableIconTint : Color.white;

            if (levelText != null)
                levelText.color = isAcquired ? SkillTreePalette.AcquiredText : SkillTreePalette.OnSurface;
        }

        /// <summary>선택 표시 — 상세 패널이 어느 칸을 말하는지 테두리 링을 흰 글로우로 바꿔 알린다 (Refresh가 색·표시를 결정).</summary>
        public void SetSelected(bool isSelected)
        {
            if (_isSelected == isSelected) return;

            _isSelected = isSelected;
            Refresh();
        }

        /// <summary>습득 순간 펀치 연출. timeScale=0에서도 재생된다 (SetUpdate(true)).</summary>
        public void PlayAcquirePunch()
        {
            _punchTween?.Kill();
            transform.localScale = Vector3.one;
            _punchTween = DOTween.Sequence()
                .Append(transform.DOScale(AcquirePunchScale, AcquirePunchDuration * 0.5f).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(1f, AcquirePunchDuration * 0.5f).SetEase(Ease.OutBack))
                .SetUpdate(true);
        }

        private void _OnClickSelect()
        {
            // 잠긴 칸도 정보를 볼 수 있게 선택한다. 구매는 상세 패널 버튼이 막는다.
            if (_data == null) return;

            _onSelect?.Invoke(_data, _tier);
        }
    }
}
