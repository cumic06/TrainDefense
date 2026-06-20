using System.Collections.Generic;
using System.Linq;
using Cumic.Achievement;
using Cumic.Events;
using TrainDefense.Game.Events;
using UnityEngine;

namespace Cumic
{
    public class TrainDefenseAchievement : MonoBehaviour, IAchievementTracker
    {
        private class TrainDefenseAchievementData : IAchievementData
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public int TargetValue { get; set; }
            public bool IsHidden { get; set; }
            public string ConditionKey { get; set; }
        }

        private List<TrainDefenseAchievementData> _achievements = new();
        private AchievementSaveData _saveData;
        private Dictionary<string, List<TrainDefenseAchievementData>> _conditionMap = new();

        private void Start()
        {
            Initialize();
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        #region IAchievementTracker

        public void Initialize()
        {
            RegisterAchievements();
            Load();
            BuildConditionMap();
        }

        public void AddProgress(string achievementId, int amount = 1)
        {
            var state = _saveData.GetState(achievementId);
            if (state.IsUnlocked) return;

            var data = _achievements.FirstOrDefault(a => a.Id == achievementId);
            if (data == null) return;

            state.CurrentValue += amount;

            GameEventSystem.Publish(new AchievementProgressEvent(data, state));

            if (state.CurrentValue >= data.TargetValue)
            {
                state.IsUnlocked = true;
                GameEventSystem.Publish(new AchievementUnlockedEvent(data, state));
                Debug.Log($"[Achievement] 달성: {data.Title}");
            }

            Save();
        }

        public void SetProgress(string achievementId, int value)
        {
            var state = _saveData.GetState(achievementId);
            if (state.IsUnlocked) return;

            var data = _achievements.FirstOrDefault(a => a.Id == achievementId);
            if (data == null) return;

            state.CurrentValue = value;

            GameEventSystem.Publish(new AchievementProgressEvent(data, state));

            if (state.CurrentValue >= data.TargetValue)
            {
                state.IsUnlocked = true;
                GameEventSystem.Publish(new AchievementUnlockedEvent(data, state));
                Debug.Log($"[Achievement] 달성: {data.Title}");
            }

            Save();
        }

        public AchievementState GetState(string achievementId)
        {
            return _saveData.GetState(achievementId);
        }

        public IReadOnlyList<IAchievementData> GetAllAchievements()
        {
            return _achievements.Cast<IAchievementData>().ToList();
        }

        public void Save()
        {
            _saveData.Save();
        }

        public void Load()
        {
            _saveData = AchievementSaveData.Load();
        }

        #endregion

        #region Achievement Registration

        private void RegisterAchievements()
        {
            _achievements = new List<TrainDefenseAchievementData>
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

        private void BuildConditionMap()
        {
            _conditionMap.Clear();
            foreach (var achievement in _achievements)
            {
                if (!_conditionMap.ContainsKey(achievement.ConditionKey))
                    _conditionMap[achievement.ConditionKey] = new List<TrainDefenseAchievementData>();

                _conditionMap[achievement.ConditionKey].Add(achievement);
            }
        }

        #endregion

        #region Event Bridge

        private void AddProgressByKey(string conditionKey, int amount = 1)
        {
            if (!_conditionMap.TryGetValue(conditionKey, out var achievements)) return;

            foreach (var data in achievements)
            {
                var state = _saveData.GetState(data.Id);
                if (state.IsUnlocked) continue;

                state.CurrentValue += amount;

                GameEventSystem.Publish(new AchievementProgressEvent(data, state));

                if (state.CurrentValue >= data.TargetValue)
                {
                    state.IsUnlocked = true;
                    GameEventSystem.Publish(new AchievementUnlockedEvent(data, state));
                    Debug.Log($"[Achievement] 달성: {data.Title}");
                }
            }

            Save();
        }

        private void SetProgressByKey(string conditionKey, int value)
        {
            if (!_conditionMap.TryGetValue(conditionKey, out var achievements)) return;

            foreach (var data in achievements)
            {
                var state = _saveData.GetState(data.Id);
                if (state.IsUnlocked) continue;

                state.CurrentValue = value;

                GameEventSystem.Publish(new AchievementProgressEvent(data, state));

                if (state.CurrentValue >= data.TargetValue)
                {
                    state.IsUnlocked = true;
                    GameEventSystem.Publish(new AchievementUnlockedEvent(data, state));
                    Debug.Log($"[Achievement] 달성: {data.Title}");
                }
            }

            Save();
        }

        private void SubscribeEvents()
        {
            GameEventSystem.Subscribe<MonsterDeadEvent>(OnMonsterDead);
            GameEventSystem.Subscribe<StageEndEvent>(OnStageEnd);
            GameEventSystem.Subscribe<LevelUpEvent>(OnLevelUp);
            GameEventSystem.Subscribe<AddTrainEvent>(OnTrainAdded);
            GameEventSystem.Subscribe<TrainDeadEvent>(OnTrainDead);
            GameEventSystem.Subscribe<BuyShopItemEvent>(OnShopItemBought);
            GameEventSystem.Subscribe<UpgradeAppliedEvent>(OnUpgradeApplied);
            GameEventSystem.Subscribe<NewMonsterDiscoveredEvent>(OnMonsterDiscovered);
            GameEventSystem.Subscribe<GameEnterEvent>(OnGameEnter);
        }

        private void UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<MonsterDeadEvent>(OnMonsterDead);
            GameEventSystem.Unsubscribe<StageEndEvent>(OnStageEnd);
            GameEventSystem.Unsubscribe<LevelUpEvent>(OnLevelUp);
            GameEventSystem.Unsubscribe<AddTrainEvent>(OnTrainAdded);
            GameEventSystem.Unsubscribe<TrainDeadEvent>(OnTrainDead);
            GameEventSystem.Unsubscribe<BuyShopItemEvent>(OnShopItemBought);
            GameEventSystem.Unsubscribe<UpgradeAppliedEvent>(OnUpgradeApplied);
            GameEventSystem.Unsubscribe<NewMonsterDiscoveredEvent>(OnMonsterDiscovered);
            GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
        }

        private void OnMonsterDead(MonsterDeadEvent e) => AddProgressByKey("monster_kill");
        private void OnStageEnd(StageEndEvent e) { if (e.IsClear) AddProgressByKey("stage_clear"); }
        private void OnLevelUp(LevelUpEvent e) => SetProgressByKey("level_reached", e.LevelUpCount);
        private void OnTrainAdded(AddTrainEvent e) => AddProgressByKey("train_added");
        private void OnTrainDead(TrainDeadEvent e) => AddProgressByKey("train_lost");
        private void OnShopItemBought(BuyShopItemEvent e) => AddProgressByKey("shop_item_bought");
        private void OnUpgradeApplied(UpgradeAppliedEvent e) => AddProgressByKey("upgrade_purchased");
        private void OnMonsterDiscovered(NewMonsterDiscoveredEvent e) => AddProgressByKey("monster_discovered");
        private void OnGameEnter(GameEnterEvent e) => AddProgressByKey("game_played");

        #endregion
    }
}
