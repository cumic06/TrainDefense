using UnityEngine;

namespace TrainDefense.Game.Difficulty
{
    /// <summary>
    /// 로비에서 고른 난이도 단계와 해금 진행도.
    /// 선택값은 로비 → 게임 씬으로 넘기는 정적 홀더이며, 판이 시작되면 StageManager가 자기 상태로 가져간다
    /// (이어하기는 세이브에 저장된 단계를 쓰므로 이 값을 보지 않는다).
    /// 해금은 "이전 단계를 클리어하면 다음 단계"(09-23 결정)라 최고 클리어 단계 하나만 PlayerPrefs에 둔다.
    /// </summary>
    public static class DifficultyStageSelection
    {
        public const int MIN_STAGE = 1;
        public const int MAX_STAGE = 3;

        private const string HIGHEST_CLEARED_STAGE_KEY = "DifficultyHighestClearedStage";

        private static int _selectedStage = MIN_STAGE;

        /// <summary>다음 판에 적용할 단계(1부터). 잠긴 단계는 받지 않는다.</summary>
        public static int SelectedStage
        {
            get => _selectedStage;
            set
            {
                if (!IsUnlocked(value))
                {
                    // 로비 UI의 잠금 표시와 어긋난 것이라 조용히 1단계로 떨어뜨리지 않고 남긴다.
                    Debug.LogWarning($"[Difficulty] 잠긴 단계 {value}를 골라 {MIN_STAGE}단계로 둡니다 (최고 클리어 {HighestClearedStage}).");
                    _selectedStage = MIN_STAGE;

                    return;
                }

                _selectedStage = value;
            }
        }

        /// <summary>클리어한 가장 높은 단계. 0이면 아직 클리어가 없다.</summary>
        public static int HighestClearedStage => PlayerPrefs.GetInt(HIGHEST_CLEARED_STAGE_KEY, 0);

        /// <summary>1단계는 항상 열려 있고, 그 위는 바로 아래 단계를 클리어했을 때 열린다.</summary>
        public static bool IsUnlocked(int stage)
            => stage >= MIN_STAGE && stage <= MAX_STAGE && stage <= HighestClearedStage + 1;

        /// <summary>종착역 도착 때 StageManager가 부른다. 더 높은 단계를 깼을 때만 기록한다.</summary>
        public static void MarkCleared(int stage)
        {
            if (stage <= HighestClearedStage)
                return;

            PlayerPrefs.SetInt(HIGHEST_CLEARED_STAGE_KEY, stage);
            PlayerPrefs.Save();
        }

        // 도메인 리로드 없이 플레이를 반복해도 이전 플레이의 선택이 남지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void _ResetStatics() => _selectedStage = MIN_STAGE;
    }
}
