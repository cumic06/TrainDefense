using UnityEngine;

namespace TrainDefense.Game.Manager
{
    /// <summary>
    /// 한 판 동안 거쳐 간 맵 한 구간의 결산 기록.
    /// 맵에 진입할 때 점수/처치 수를 스냅샷하고, 다음 맵으로 넘어가거나 게임이 끝날 때
    /// 그 차이를 "이 맵에서 번 점수/처치 수"로 확정해 누적한다. (<see cref="StageManager.RunRecords"/>)
    /// 게임오버 결산 슬라이드쇼(StageResultSlideUI)가 이 목록을 읽어 맵별로 보여준다.
    /// </summary>
    public class StageRunRecord
    {
        public string StageId;
        /// <summary>맵 선택 UI용 스테이지 이미지(StageData.StageImage). 없을 수 있다.</summary>
        public Sprite StageImage;
        /// <summary>이 맵 구간에서 획득한 점수(이탈 점수 - 진입 점수).</summary>
        public int ScoreEarned;
        /// <summary>이 맵 구간에서 잡은 일반 몬스터 수.</summary>
        public int NormalKill;
        /// <summary>이 맵 구간에서 잡은 엘리트 몬스터 수.</summary>
        public int EliteKill;
    }
}
