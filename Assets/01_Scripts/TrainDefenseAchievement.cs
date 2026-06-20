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
        private List<AchievementData> _achievements = new();
        private AchievementSaveData _saveData;
        private Dictionary<string, List<AchievementData>> _conditionMap = new();

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
            // 업적 정의는 AchievementCatalog가 단일 소스로 보유한다(로비 UI와 공유).
            _achievements = new List<AchievementData>(AchievementCatalog.All);
        }

        private void BuildConditionMap()
        {
            _conditionMap.Clear();
            foreach (var achievement in _achievements)
            {
                if (!_conditionMap.ContainsKey(achievement.ConditionKey))
                    _conditionMap[achievement.ConditionKey] = new List<AchievementData>();

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
