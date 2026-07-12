using Cumic;
using TMPro;
using TrainDefense.Game.Manager;
using TrainDefense.Localize;
using UnityEngine;
using UnityEngine.UI;

namespace TrainDefense.Game.UI
{
    /// <summary>
    /// 게임오버 결산 슬라이드쇼의 맵 한 칸. 맵 이미지 + 순번 라벨 + 점수 텍스트 + 처치 내역을 표시한다.
    /// 점수는 <see cref="StageResultSlideUI"/>의 카운트업 트윈이 <see cref="SetScore"/>로 갱신한다.
    /// </summary>
    public class StageResultSlotUI : MonoBehaviour
    {
        [SerializeField]
        private Image mapImage;
        [SerializeField]
        private TMP_Text mapLabel;
        [SerializeField]
        private TMP_Text scoreText;
        [Tooltip("어떤 적을 몇 마리 잡아 몇 점인지 보여주는 내역 텍스트(선택).")]
        [SerializeField]
        private TMP_Text killBreakdownText;

        // 카운트업 setter가 매 프레임 호출되므로 접미사("점")는 Bind 시 1회만 해석해 캐시한다.
        private string _scoreSuffix = "점";

        /// <summary>맵 구간 기록으로 칸을 채운다. displayIndex는 1부터 시작하는 방문 순번.</summary>
        public void Bind(StageRunRecord record, int displayIndex)
        {
            if (mapImage != null)
            {
                mapImage.sprite = record?.StageImage;
                // 스프라이트가 없으면 흰 박스가 보이지 않도록 이미지 자체를 숨긴다.
                mapImage.enabled = mapImage.sprite != null;
            }

            if (mapLabel != null)
            {
                string format = LocalizeHelper.GetByKey("result_map_index", "{0}번째 맵");
                mapLabel.text = string.Format(format, displayIndex);
            }

            _scoreSuffix = LocalizeHelper.GetByKey("result_score_suffix", "점");
            _SetKillBreakdown(record);
            SetScore(0);
        }

        /// <summary>맵별 점수 합계를 보여주는 마지막 "총점" 칸으로 구성한다.</summary>
        public void BindTotal()
        {
            if (mapImage != null)
            {
                mapImage.enabled = false;
            }

            if (mapLabel != null)
            {
                mapLabel.text = LocalizeHelper.GetByKey("result_total_label", "총점");
            }

            _scoreSuffix = LocalizeHelper.GetByKey("result_score_suffix", "점");

            if (killBreakdownText != null)
                killBreakdownText.text = string.Empty;

            SetScore(0);
        }

        /// <summary>카운트업 트윈이 호출. 천 단위 콤마 + 접미사로 표기.</summary>
        public void SetScore(int value)
        {
            if (scoreText == null)
                return;

            scoreText.text = $"{value.ToCommaString()} {_scoreSuffix}";
        }

        // "일반 몬스터 ×12 +120점 · 엘리트 몬스터 ×3 +150점" 형태로 어떤 적에게서 몇 점을 얻었는지 표기.
        private void _SetKillBreakdown(StageRunRecord record)
        {
            if (killBreakdownText == null)
                return;

            var scoreManager = ScoreManager.Instance;
            int normalUnit = scoreManager != null ? scoreManager.NormalKillUnitScore : 0;
            int eliteUnit = scoreManager != null ? scoreManager.EliteKillUnitScore : 0;

            int normalKill = record?.NormalKill ?? 0;
            int eliteKill = record?.EliteKill ?? 0;

            string normalLabel = LocalizeHelper.GetByKey("result_kill_normal", "일반 몬스터");
            string eliteLabel = LocalizeHelper.GetByKey("result_kill_elite", "엘리트 몬스터");

            killBreakdownText.text =
                $"{normalLabel} ×{normalKill}  +{(normalKill * normalUnit).ToCommaString()}{_scoreSuffix}" +
                $"  ·  {eliteLabel} ×{eliteKill}  +{(eliteKill * eliteUnit).ToCommaString()}{_scoreSuffix}";
        }
    }
}
