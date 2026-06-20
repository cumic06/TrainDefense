using System.Collections.Generic;

namespace Cumic.Achievement
{
    /// <summary>
    /// 모든 업적의 정적 정의 목록. 인게임 트래커(TrainDefenseAchievement)와
    /// 로비 업적 UI가 동일한 정의를 공유하도록 한곳에 모았다.
    /// 진행 상태(달성 여부/현재값)는 AchievementSaveData에 별도 저장된다.
    /// </summary>
    public static class AchievementCatalog
    {
        private static List<AchievementData> _all;

        public static IReadOnlyList<AchievementData> All
        {
            get
            {
                if (_all == null)
                    _Build();

                return _all;
            }
        }

        public static AchievementData Find(string id)
        {
            foreach (var data in All)
            {
                if (data.Id == id)
                    return data;
            }

            return null;
        }

        private static void _Build()
        {
            _all = new List<AchievementData>
            {
                // 몬스터 처치
                new() { Id = "monster_kill_50", Title = "몬스터 사냥꾼", Description = "몬스터 50마리 처치", ConditionKey = "monster_kill", TargetValue = 50 },
                new() { Id = "monster_kill_500", Title = "몬스터 학살자", Description = "몬스터 500마리 처치", ConditionKey = "monster_kill", TargetValue = 500 },
                new() { Id = "monster_kill_5000", Title = "몬스터 멸망자", Description = "몬스터 5000마리 처치", ConditionKey = "monster_kill", TargetValue = 5000 },

                // 스테이지 클리어
                new() { Id = "stage_clear_1", Title = "첫 승리", Description = "스테이지 1회 클리어", ConditionKey = "stage_clear", TargetValue = 1 },
                new() { Id = "stage_clear_10", Title = "스테이지 정복자", Description = "스테이지 10회 클리어", ConditionKey = "stage_clear", TargetValue = 10 },

                // 레벨 도달
                new() { Id = "level_5", Title = "성장 중", Description = "레벨 5 도달", ConditionKey = "level_reached", TargetValue = 5 },
                new() { Id = "level_10", Title = "베테랑", Description = "레벨 10 도달", ConditionKey = "level_reached", TargetValue = 10 },

                // 기차 관련
                new() { Id = "train_added_10", Title = "열차 수집가", Description = "기차 10대 추가", ConditionKey = "train_added", TargetValue = 10 },
                new() { Id = "train_lost_5", Title = "고난의 길", Description = "기차 5대 잃기", ConditionKey = "train_lost", TargetValue = 5 },

                // 업그레이드
                new() { Id = "upgrade_10", Title = "강화 마니아", Description = "업그레이드 10회 구매", ConditionKey = "upgrade_purchased", TargetValue = 10 },

                // 상점
                new() { Id = "shop_buy_10", Title = "단골 손님", Description = "상점 아이템 10회 구매", ConditionKey = "shop_item_bought", TargetValue = 10 },

                // 몬스터 발견
                new() { Id = "monster_discover_5", Title = "탐험가", Description = "몬스터 5종 발견", ConditionKey = "monster_discovered", TargetValue = 5 },

                // 게임 플레이
                new() { Id = "game_play_10", Title = "열혈 방어대원", Description = "게임 10회 플레이", ConditionKey = "game_played", TargetValue = 10 },
            };
        }
    }
}
