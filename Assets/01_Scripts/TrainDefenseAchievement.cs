using System.Collections.Generic;
using System.Linq;
using Cumic.Achievement;
using Cumic.Events;
using TrainDefense;
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

        // 로비 데모(LobbyGameSimulation)는 게임씬과 동일하게 몬스터 스폰/처치 이벤트를 발생시키므로,
        // 로비에서 발생한 이벤트는 업적에 반영하지 않는다. Monster/TrainManager/ScoreManager와 동일한 판별 패턴.
        private bool _IsLobby()
        {
            return UserDataManager.Instance != null && UserDataManager.Instance.IsLobby;
        }

        private void AddProgressByKey(string conditionKey, int amount = 1)
        {
            if (_IsLobby()) return;

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
            if (_IsLobby()) return;

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
            GameEventSystem.Unsubscribe<LevelUpEvent>(OnLevelUp);
            GameEventSystem.Unsubscribe<AddTrainEvent>(OnTrainAdded);
            GameEventSystem.Unsubscribe<TrainDeadEvent>(OnTrainDead);
            GameEventSystem.Unsubscribe<BuyShopItemEvent>(OnShopItemBought);
            GameEventSystem.Unsubscribe<UpgradeAppliedEvent>(OnUpgradeApplied);
            GameEventSystem.Unsubscribe<NewMonsterDiscoveredEvent>(OnMonsterDiscovered);
            GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
        }

        private void OnMonsterDead(MonsterDeadEvent e) => AddProgressByKey("monster_kill");
        // level_reached는 "도달한 레벨"이 기준이다. LevelUpEvent.LevelUpCount는 이번 경험치 획득에서
        // 오른 레벨 수(보통 1)라 누적 레벨이 아니므로, 현재 누적 레벨(UserDataManager.CurrentLevel)로 갱신한다.
        private void OnLevelUp(LevelUpEvent e)
        {
            int level = UserDataManager.Instance != null ? UserDataManager.Instance.CurrentLevel : e.LevelUpCount;
            SetProgressByKey("level_reached", level);
        }
        private void OnTrainAdded(AddTrainEvent e) => AddProgressByKey("train_added");
        private void OnTrainDead(TrainDeadEvent e) => AddProgressByKey("train_lost");
        private void OnShopItemBought(BuyShopItemEvent e) => AddProgressByKey("shop_item_bought");
        private void OnUpgradeApplied(UpgradeAppliedEvent e) => AddProgressByKey("upgrade_purchased");
        private void OnMonsterDiscovered(NewMonsterDiscoveredEvent e) => AddProgressByKey("monster_discovered");
        // 로비 GameEnterEvent(isLobby:true)는 카운트하지 않는다. UserDataManager 갱신 순서와 무관하게
        // 이벤트 자체의 IsLobby로 직접 판별한다.
        private void OnGameEnter(GameEnterEvent e)
        {
            if (e.IsLobby) return;
            AddProgressByKey("game_played");
        }

        #endregion
    }
}
