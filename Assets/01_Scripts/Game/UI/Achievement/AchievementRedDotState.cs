using System;
using Cumic.Achievement;
using UnityEngine;

namespace TrainDefense.Game.UI.Achievement
{
    /// <summary>
    /// 업적 레드닷 상태. "아직 확인하지 않은 달성 업적"이 있는지를 판정한다.
    /// 확인 기준은 마지막으로 업적 팝업을 열었을 때의 달성 수(PlayerPrefs)이며,
    /// 팝업을 열면 <see cref="MarkAllSeen"/>으로 현재 달성 수를 기록해 레드닷이 꺼진다.
    /// </summary>
    public static class AchievementRedDotState
    {
        private const string SeenUnlockedCountKey = "AchievementSeenUnlockedCount";

        /// <summary>레드닷 표시 상태가 바뀔 수 있는 시점(팝업 확인 등)에 발생.</summary>
        public static event Action OnChanged;

        /// <summary>저장본 기준 현재 달성(언락)된 업적 수.</summary>
        public static int GetUnlockedCount()
        {
            AchievementSaveData saveData = AchievementSaveData.Load();
            int count = 0;

            foreach (AchievementData data in AchievementCatalog.All)
            {
                if (data == null) continue;
                if (saveData.GetState(data.Id).IsUnlocked) count++;
            }

            return count;
        }

        /// <summary>아직 팝업에서 확인하지 않은 달성 업적이 있는지.</summary>
        public static bool HasUnseenUnlocked()
        {
            int unlocked = GetUnlockedCount();
            int seen = PlayerPrefs.GetInt(SeenUnlockedCountKey, 0);

            // 업적 초기화(달성 수 감소) 시 기록이 더 크면 현재 값으로 내려 잠긴 레드닷을 방지한다.
            if (seen > unlocked)
            {
                PlayerPrefs.SetInt(SeenUnlockedCountKey, unlocked);
                seen = unlocked;
            }

            return unlocked > seen;
        }

        /// <summary>업적 팝업을 연 시점에 호출 — 현재 달성 수까지 확인한 것으로 기록한다.</summary>
        public static void MarkAllSeen()
        {
            PlayerPrefs.SetInt(SeenUnlockedCountKey, GetUnlockedCount());
            OnChanged?.Invoke();
        }
    }
}
