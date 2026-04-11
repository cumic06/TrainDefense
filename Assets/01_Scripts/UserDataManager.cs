using System.Collections.Generic;
using TrainDefense.Game.Events;
using Cumic.Events;
using Cumic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TrainDefense
{
    public class UserDataManager : Singleton<UserDataManager>
    {
        private const string DISCOVERED_MONSTERS_KEY = "DiscoveredMonsters";

        private Dictionary<string, int> _triChoiceData = new();

        [ShowInInspector]
        private Dictionary<string, int> _upgradeLevels = new();

        [ShowInInspector]
        private HashSet<string> _discoveredMonsterIds = new();

        private int _coin;
        private int _currentExp;
        private int _currentLevel = 1;
        private UserOptionData _userOptionData = new();
        public int Coin => _coin;
        public float ExpPercent => _currentExp / GetNextLevelUpExp();
        public int CurrentLevel => _currentLevel;
        public bool IsHapticEnabled => _userOptionData == null || _userOptionData.IsHapticEnabled;
        public UserOptionData UserOptionData => _userOptionData;

        private void Start()
        {
            DontDestroyOnLoad(gameObject);

            LoadDiscoveredMonsters();
            LoadUserOptionData();
            SubscribeEvents();
            _coin = 0;
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        #region Event   
        private void SubscribeEvents()
        {
            GameEventSystem.Subscribe<AddExpEvent>(AddExp);
            GameEventSystem.Subscribe<TriChoiceSelectEvent>(AddTriChoiceData);
            GameEventSystem.Subscribe<ChangeCoinUIEvent>(ChangeCoin);
            GameEventSystem.Subscribe<BuyShopItemEvent>(BuyShopItem);
            GameEventSystem.Subscribe<MonsterSpawnedEvent>(OnMonsterSpawned);
            GameEventSystem.Subscribe<GameEnterEvent>(OnGameEnter);
        }

        private void UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<AddExpEvent>(AddExp);
            GameEventSystem.Unsubscribe<TriChoiceSelectEvent>(AddTriChoiceData);
            GameEventSystem.Unsubscribe<ChangeCoinUIEvent>(ChangeCoin);
            GameEventSystem.Unsubscribe<BuyShopItemEvent>(BuyShopItem);
            GameEventSystem.Unsubscribe<MonsterSpawnedEvent>(OnMonsterSpawned);
            GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
        }

        private void OnGameEnter(GameEnterEvent gameEnterEvent)
        {
            _coin = 0;
            _currentExp = 0;
            _currentLevel = 1;
            _triChoiceData.Clear();
            _upgradeLevels.Clear();
        }
        #endregion

        private void OnMonsterSpawned(MonsterSpawnedEvent monsterSpawnedEvent)
        {
            string monsterId = monsterSpawnedEvent.MonsterId;

            if (string.IsNullOrEmpty(monsterId)) return;

            if (!_discoveredMonsterIds.Contains(monsterId))
            {
                _discoveredMonsterIds.Add(monsterId);
                SaveDiscoveredMonsters();

                // UI에 새 몬스터 발견 알림
                GameEventSystem.Publish(new NewMonsterDiscoveredEvent(monsterId));
            }
        }

        private void LoadDiscoveredMonsters()
        {
            string savedData = PlayerPrefs.GetString(DISCOVERED_MONSTERS_KEY, "");
            _discoveredMonsterIds.Clear();

            if (!string.IsNullOrEmpty(savedData))
            {
                string[] ids = savedData.Split(',');
                foreach (string id in ids)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        _discoveredMonsterIds.Add(id);
                    }
                }
            }
        }

        private void SaveDiscoveredMonsters()
        {
            string dataToSave = string.Join(",", _discoveredMonsterIds);
            PlayerPrefs.SetString(DISCOVERED_MONSTERS_KEY, dataToSave);
            PlayerPrefs.Save();
        }

        private void LoadUserOptionData()
        {
            _userOptionData = UserOptionDataParser.Load();
        }

        public void SetHapticEnabled(bool enabled)
        {
            _userOptionData ??= new UserOptionData();
            _userOptionData.IsHapticEnabled = enabled;
            UserOptionDataParser.Save(_userOptionData);
        }

        /// <summary>
        /// 발견된 몬스터 데이터를 초기화합니다.
        /// </summary>
        [Button("발견된 몬스터 초기화")]
        private void ResetDiscoveredMonsters()
        {
            _discoveredMonsterIds.Clear();
            PlayerPrefs.DeleteKey(DISCOVERED_MONSTERS_KEY);
            PlayerPrefs.Save();
            Debug.Log("[UserDataManager] 발견된 몬스터 데이터가 초기화되었습니다.");
        }

        /// <summary>
        /// 해당 몬스터가 이미 발견되었는지 확인합니다.
        /// </summary>
        public bool IsMonsterDiscovered(string monsterId)
        {
            return _discoveredMonsterIds.Contains(monsterId);
        }

        /// <summary>
        /// 발견한 모든 몬스터 ID 목록을 반환합니다.
        /// </summary>
        public HashSet<string> GetDiscoveredMonsterIds()
        {
            return new HashSet<string>(_discoveredMonsterIds);
        }


        #region Exp
        private void AddExp(AddExpEvent addExpEvent)
        {
            _currentExp += addExpEvent.Exp;

            var requiredExpInt = Mathf.CeilToInt(GetNextLevelUpExp());
            int levelUpCount = 0;
            while (requiredExpInt > 0 && _currentExp >= requiredExpInt)
            {
                _currentExp -= requiredExpInt;
                levelUpCount++;
                LevelUp();
                requiredExpInt = Mathf.CeilToInt(GetNextLevelUpExp());
                GameEventSystem.Publish(new LevelUpEvent(levelUpCount));
            }
        }

        private void LevelUp()
        {
            _currentLevel++;
        }
        #endregion

        #region Coin
        private void ChangeCoin(ChangeCoinUIEvent changeCoinEvent)
        {
            _coin = changeCoinEvent.AfterCoin;
        }
        #endregion

        public void AddTriChoiceData(TriChoiceSelectEvent triChoiceSelectEvent)
        {
            var choiceOption = triChoiceSelectEvent.ChoiceOption;

            if (choiceOption == null) return;

            if (_triChoiceData.ContainsKey(choiceOption.Id))
            {
                _triChoiceData[choiceOption.Id]++;
            }
            else
            {
                _triChoiceData.Add(choiceOption.Id, 1);
            }
        }

        /// <summary>
        /// 선택된 ChoiceOption ID 목록을 반환합니다.
        /// </summary>
        public HashSet<string> GetSelectedChoiceIds()
        {
            return new HashSet<string>(_triChoiceData.Keys);
        }

        public bool IsFirstTimeSelected(string choiceId)
        {
            return !_triChoiceData.ContainsKey(choiceId);
        }

        /// <summary>
        /// 특정 Choice의 선택 횟수를 반환합니다.
        /// </summary>
        public int GetSelectionCount(string choiceId)
        {
            return _triChoiceData.ContainsKey(choiceId) ? _triChoiceData[choiceId] : 0;
        }

        #region Upgrade
        public int GetUpgradeLevel(string upgradeId)
        {
            return _upgradeLevels.ContainsKey(upgradeId) ? _upgradeLevels[upgradeId] : 0;
        }

        public void UpgradeLevel(string upgradeId)
        {
            if (_upgradeLevels.ContainsKey(upgradeId))
            {
                _upgradeLevels[upgradeId]++;
            }
            else
            {
                _upgradeLevels.Add(upgradeId, 1);
            }
        }

        /// <summary>
        /// 상점에서 구매한 모든 업그레이드 ID 목록을 반환합니다.
        /// </summary>
        public IEnumerable<string> GetAllUpgradeIds()
        {
            return _upgradeLevels.Keys;
        }

        private const float baseExp = 250f;
        private const float powFactor = 1.15f;
        private const float expMultiplier = 1.05f;

        public float GetNextLevelUpExp()
        {
            if (_currentLevel == 1) return baseExp;

            return baseExp * Mathf.Pow(_currentLevel - 1, powFactor) * Mathf.Pow(expMultiplier, _currentLevel - 1);
        }
        #endregion

        private void BuyShopItem(BuyShopItemEvent buyShopItemEvent)
        {
            if (_coin >= buyShopItemEvent.NeedMoney)
            {
                int beforeCoin = _coin;
                int afterCoin = _coin - buyShopItemEvent.NeedMoney;
                GameEventSystem.Publish(new ChangeCoinUIEvent(beforeCoin, afterCoin));
            }
        }
    }
}
