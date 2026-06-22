using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrainDefense.Localize;

namespace TrainDefense.Game.UI.Collection
{
    /// <summary>
    /// 도감 오른쪽 상세 패널. 선택된 유닛의 아이콘/이름/설명/스탯을 보여준다.
    /// 미발견 유닛은 검은 실루엣 + "???" + 안내 문구로 표시한다.
    /// </summary>
    public class CollectionDetailUI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private GameObject emptyHint;
        [SerializeField] private Color lockedColor = Color.black;
        [SerializeField] private string undiscoveredDescription = "아직 발견하지 못한 유닛입니다.";
        [Tooltip("발견한 몬스터의 기본 애니메이션 재생 속도(프레임/초).")]
        [SerializeField] private float framesPerSecond = 10f;
        #endregion

        private readonly StringBuilder _statBuilder = new();

        private System.Collections.Generic.IReadOnlyList<Sprite> _playingFrames;
        private int _frameIndex;
        private float _frameTimer;

        public void Show(CollectionEntry entry)
        {
            if (entry == null)
            {
                _ShowEmpty();

                return;
            }

            if (emptyHint != null)
                emptyHint.SetActive(false);

            bool discovered = entry.IsDiscovered;

            if (iconImage != null)
            {
                iconImage.sprite = entry.Icon;
                iconImage.enabled = entry.Icon != null;
                iconImage.color = discovered ? Color.white : lockedColor;
            }

            // 발견한 몬스터이고 베이크된 프레임이 있으면 순환 재생, 아니면 첫 프레임으로 정지한다.
            if (discovered && entry.HasAnimation)
                _StartAnimation(entry.Frames);
            else
                _StopAnimation();

            if (nameText != null)
                nameText.text = discovered ? entry.Name : "???";

            if (descriptionText != null)
            {
                string description = discovered ? entry.Description : LocalizeHelper.GetByKey("Collection_Undiscovered", undiscoveredDescription);
                descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(description));
                descriptionText.text = description;
            }

            if (statsText != null)
                statsText.text = discovered ? _BuildStatsText(entry) : string.Empty;
        }

        private void Update()
        {
            if (_playingFrames == null || _playingFrames.Count <= 1 || iconImage == null)
                return;

            _frameTimer += Time.unscaledDeltaTime;
            float interval = 1f / Mathf.Max(1f, framesPerSecond);

            while (_frameTimer >= interval)
            {
                _frameTimer -= interval;
                _frameIndex = (_frameIndex + 1) % _playingFrames.Count;
                iconImage.sprite = _playingFrames[_frameIndex];
            }
        }

        private void _StartAnimation(System.Collections.Generic.IReadOnlyList<Sprite> frames)
        {
            _playingFrames = frames;
            _frameIndex = 0;
            _frameTimer = 0f;

            if (iconImage != null && frames.Count > 0)
            {
                iconImage.sprite = frames[0];
                iconImage.enabled = true;
                iconImage.color = Color.white;
            }
        }

        private void _StopAnimation()
        {
            _playingFrames = null;
        }

        private void _ShowEmpty()
        {
            _StopAnimation();

            if (iconImage != null)
                iconImage.enabled = false;

            if (nameText != null)
                nameText.text = string.Empty;

            if (descriptionText != null)
                descriptionText.text = string.Empty;

            if (statsText != null)
                statsText.text = string.Empty;

            if (emptyHint != null)
            {
                emptyHint.SetActive(true);

                TMP_Text hintText = emptyHint.GetComponentInChildren<TMP_Text>(true);
                if (hintText != null)
                    hintText.text = LocalizeHelper.GetByKey("Collection_SelectPrompt", "유닛을 선택하세요");
            }
        }

        private string _BuildStatsText(CollectionEntry entry)
        {
            _statBuilder.Clear();

            foreach (CollectionStatLine line in entry.StatLines)
            {
                _statBuilder.AppendLine($"{line.Label} : {line.Value}");
            }

            return _statBuilder.ToString();
        }
    }
}
