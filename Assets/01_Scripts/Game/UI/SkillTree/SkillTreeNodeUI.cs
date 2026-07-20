using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using TrainDefense.Game.Datas;

namespace TrainDefense.Game.UI.SkillTree
{
    /// <summary>
    /// 스킬트리 노드 한 칸. 상태(잠김/획득가능/습득/만렙) 비주얼과 선택 알림, 습득 펀치 연출을 담당한다.
    /// 설명·비용·습득 버튼은 팝업(SkillTreePopupUI) 하단 상세 패널 몫이다 (PermanentUpgradeSlotUI 선례).
    /// 상태는 색으로만 구분하지 않는다 — 레벨 텍스트(Lv n/m·MAX)가 항상 병행한다 (이중부호화).
    /// </summary>
    public class SkillTreeNodeUI : MonoBehaviour
    {
        // DESIGN.md 습득 펀치: 1.0 → 1.25 → 1.0, 240ms. vibrato 불사용(저프레임 언더샘플링 함정) — 시퀀스로 구성.
        private const float AcquirePunchScale = 1.25f;
        private const float AcquirePunchDuration = 0.24f;

        #region Fields
        [SerializeField] private Image backgroundImage;   // 노드 배경 (TurretSelectSlotBg 9-slice 권장)
        [SerializeField] private Image borderImage;       // 획득 가능 강조 테두리 (TurretSelectHighlight 권장)
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Button selectButton;
        #endregion

        private SkillNodeData _data;
        private Action<SkillNodeData> _onSelect;
        private Tween _punchTween;

        public SkillNodeData Data => _data;
        public RectTransform RectTransform => (RectTransform)transform;

        private void Awake()
        {
            if (selectButton != null)
                selectButton.onClick.AddListener(_OnClickSelect);
        }

        private void OnDestroy()
        {
            if (selectButton != null)
                selectButton.onClick.RemoveListener(_OnClickSelect);

            _punchTween?.Kill();
        }

        public void Init(SkillNodeData data, Action<SkillNodeData> onSelect)
        {
            _data = data;
            _onSelect = onSelect;

            if (iconImage != null)
            {
                iconImage.sprite = data.Icon;
                iconImage.enabled = data.Icon != null;
            }

            Refresh();
        }

        /// <summary>현재 레벨·습득 가능 여부에 맞춰 상태 비주얼을 갱신한다.</summary>
        public void Refresh()
        {
            var manager = SkillTreeManager.Instance;
            if (manager == null || _data == null) return;

            int level = manager.GetLevel(_data.Id);
            bool isMax = manager.IsMaxLevel(_data.Id);
            bool arePrerequisitesMet = manager.ArePrerequisitesMet(_data.Id);
            bool canAcquire = manager.CanAcquire(_data.Id);

            if (backgroundImage != null)
            {
                if (isMax) backgroundImage.color = SkillTreePalette.Mastered;
                else if (level > 0) backgroundImage.color = SkillTreePalette.Accent;
                else if (arePrerequisitesMet) backgroundImage.color = SkillTreePalette.SurfaceRaised;
                else backgroundImage.color = SkillTreePalette.SurfaceLine;
            }

            // accent 테두리 = "지금 습득 가능" 신호 (비용까지 충족)
            if (borderImage != null)
            {
                borderImage.enabled = level == 0 && canAcquire;
                borderImage.color = SkillTreePalette.Accent;
            }

            if (iconImage != null)
                iconImage.color = level > 0 || arePrerequisitesMet ? Color.white : SkillTreePalette.OnSurfaceMuted;

            if (levelText != null)
                levelText.text = isMax ? "MAX" : _data.MaxLevel > 0 ? $"Lv {level}/{_data.MaxLevel}" : $"Lv {level}";
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
            _onSelect?.Invoke(_data);
        }
    }
}
