using System.Collections.Generic;
using TrainDefense.Game;
using TrainDefense.Game.Events;
using TrainDefense.Game.Tutorial;
using Cumic.Events;
using Cumic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace TrainDefense
{
    public class UserDataManager : Singleton<UserDataManager>
    {
        private const string DISCOVERED_MONSTERS_KEY = "DiscoveredMonsters";
        private const string DISCOVERED_TRAINS_KEY = "DiscoveredTrains";
        private const string TUTORIAL_SAVE_KEY = "TutorialSaveData";
        // 포탑별 최장 생존 시간(초) 저장 키 접두사. 실제 키는 BEST_SURVIVAL_PREFIX + turretId.
        private const string BEST_SURVIVAL_PREFIX = "BestSurvival_";

        private Dictionary<string, int> _triChoiceData = new();
        private TutorialSaveData _tutorialSaveData;

        [ShowInInspector]
        private Dictionary<string, int> _upgradeLevels = new();

        [ShowInInspector]
        private HashSet<string> _discoveredMonsterIds = new();

        [ShowInInspector]
        private HashSet<string> _discoveredTrainIds = new();

        private int _coin;
        private int _currentExp;
        private int _currentLevel = 1;
        private UserOptionData _userOptionData = new();
        public int Coin => _coin;
        public float ExpPercent => _currentExp / GetNextLevelUpExp();
        public int CurrentExp => _currentExp;
        public int CurrentLevel => _currentLevel;
        public bool IsLobby { get; private set; }
        public bool IsHapticEnabled => _userOptionData == null || _userOptionData.IsHapticEnabled;
        public UserOptionData UserOptionData => _userOptionData;
        public TutorialSaveData TutorialSaveData => _tutorialSaveData;

        // 이번 판 포탑 선택창에서 고른 주무기 포탑 ID. 게임 시작 게이트 → MainTrain 무기 장착 → 생존시간 기록에 사용. (세션값, 영구저장 안 함)
        public string SelectedTurretId { get; set; }

        protected override void Awake()
        {
            base.Awake();
            if (Instance == this)
                DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            LoadDiscoveredMonsters();
            LoadDiscoveredTrains();
            LoadUserOptionData();
            LoadTutorialData();
            SubscribeEvents();
            _coin = 0;
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            if (_tutorialSaveData != null)
                _tutorialSaveData.OnDataChanged -= SaveTutorialData;
        }

        #region Event   
        private void SubscribeEvents()
        {
            GameEventSystem.Subscribe<AddExpEvent>(AddExp);
            GameEventSystem.Subscribe<TriChoiceSelectEvent>(AddTriChoiceData);
            GameEventSystem.Subscribe<ChangeCoinUIEvent>(ChangeCoin);
            GameEventSystem.Subscribe<BuyShopItemEvent>(BuyShopItem);
            GameEventSystem.Subscribe<MonsterSpawnedEvent>(OnMonsterSpawned);
            GameEventSystem.Subscribe<TrainSpawnedEvent>(OnTrainSpawned);
            GameEventSystem.Subscribe<GameEnterEvent>(OnGameEnter);
        }

        private void UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<AddExpEvent>(AddExp);
            GameEventSystem.Unsubscribe<TriChoiceSelectEvent>(AddTriChoiceData);
            GameEventSystem.Unsubscribe<ChangeCoinUIEvent>(ChangeCoin);
            GameEventSystem.Unsubscribe<BuyShopItemEvent>(BuyShopItem);
            GameEventSystem.Unsubscribe<MonsterSpawnedEvent>(OnMonsterSpawned);
            GameEventSystem.Unsubscribe<TrainSpawnedEvent>(OnTrainSpawned);
            GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
        }

        private void OnGameEnter(GameEnterEvent gameEnterEvent)
        {
            _coin = 0;
            _currentExp = 0;
            _currentLevel = 1;
            _triChoiceData.Clear();
            _upgradeLevels.Clear();
            IsLobby = gameEnterEvent.IsLobby;
        }
        #endregion

        private void OnMonsterSpawned(MonsterSpawnedEvent monsterSpawnedEvent)
        {
            // 로비 시뮬레이션에서 스폰된 몬스터는 도감에 등록하지 않는다.
            if (IsLobby) return;

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

        private void OnTrainSpawned(TrainSpawnedEvent trainSpawnedEvent)
        {
            // 로비 시뮬레이션에서 스폰된 트레인은 도감에 등록하지 않는다.
            if (IsLobby) return;

            string trainId = trainSpawnedEvent.TrainId;

            if (string.IsNullOrEmpty(trainId)) return;

            if (!_discoveredTrainIds.Contains(trainId))
            {
                _discoveredTrainIds.Add(trainId);
                SaveDiscoveredTrains();
            }
        }

        private void LoadDiscoveredTrains()
        {
            string savedData = PlayerPrefs.GetString(DISCOVERED_TRAINS_KEY, "");
            _discoveredTrainIds.Clear();

            if (!string.IsNullOrEmpty(savedData))
            {
                string[] ids = savedData.Split(',');
                foreach (string id in ids)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        _discoveredTrainIds.Add(id);
                    }
                }
            }
        }

        private void SaveDiscoveredTrains()
        {
            string dataToSave = string.Join(",", _discoveredTrainIds);
            PlayerPrefs.SetString(DISCOVERED_TRAINS_KEY, dataToSave);
            PlayerPrefs.Save();
        }

        private void LoadUserOptionData()
        {
            _userOptionData = UserOptionDataParser.Load();
        }

        #region Tutorial Data

        private void LoadTutorialData()
        {
            string json = PlayerPrefs.GetString(TUTORIAL_SAVE_KEY, "");
            _tutorialSaveData = TutorialSaveData.FromJson(json);
            _tutorialSaveData.OnDataChanged += SaveTutorialData;
        }

        private void SaveTutorialData()
        {
            string json = _tutorialSaveData.ToJson();
            PlayerPrefs.SetString(TUTORIAL_SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        [Button("튜토리얼 진행 초기화")]
        public void ResetTutorialData()
        {
            _tutorialSaveData?.ResetAll();
            Debug.Log("[UserDataManager] 튜토리얼 데이터가 초기화되었습니다.");
        }

        #endregion

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
        public void ResetDiscoveredMonsters()
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

        /// <summary>
        /// 발견된 트레인 데이터를 초기화합니다.
        /// </summary>
        [Button("발견된 트레인 초기화")]
        public void ResetDiscoveredTrains()
        {
            _discoveredTrainIds.Clear();
            PlayerPrefs.DeleteKey(DISCOVERED_TRAINS_KEY);
            PlayerPrefs.Save();
            Debug.Log("[UserDataManager] 발견된 트레인 데이터가 초기화되었습니다.");
        }

        /// <summary>
        /// 해당 트레인이 이미 발견되었는지 확인합니다.
        /// </summary>
        public bool IsTrainDiscovered(string trainId)
        {
            return _discoveredTrainIds.Contains(trainId);
        }

        /// <summary>
        /// 발견한 모든 트레인 ID 목록을 반환합니다.
        /// </summary>
        public HashSet<string> GetDiscoveredTrainIds()
        {
            return new HashSet<string>(_discoveredTrainIds);
        }

        /// <summary>
        /// 발견한 트레인/몬스터(도감) 런타임 데이터를 모두 비웁니다.
        /// PlayerPrefs.DeleteAll() 등으로 디스크를 지운 뒤 인메모리 상태를 동기화할 때 호출합니다.
        /// (DeleteAll이 이미 키를 지우므로 여기서는 인메모리만 비운다.)
        /// </summary>
        public void ClearDiscoveredCollections()
        {
            _discoveredMonsterIds.Clear();
            _discoveredTrainIds.Clear();
        }

        #region Survival Time
        /// <summary>
        /// 해당 포탑으로 기록한 최장 생존 시간(초)을 반환합니다. 기록이 없으면 0.
        /// </summary>
        public float GetBestSurvivalTime(string turretId)
        {
            if (string.IsNullOrEmpty(turretId))
                return 0f;

            return PlayerPrefs.GetFloat(BEST_SURVIVAL_PREFIX + turretId, 0f);
        }

        /// <summary>
        /// 이번 판 생존 시간을 보고합니다. 기존 기록보다 길 때만 갱신·저장합니다.
        /// </summary>
        public void ReportSurvivalTime(string turretId, float seconds)
        {
            if (string.IsNullOrEmpty(turretId) || seconds <= 0f)
                return;

            if (seconds <= GetBestSurvivalTime(turretId))
                return;

            PlayerPrefs.SetFloat(BEST_SURVIVAL_PREFIX + turretId, seconds);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 모든 포탑(터렛·원거리)의 최장 생존 시간 기록을 삭제합니다.
        /// </summary>
        public void ResetAllSurvivalTimes()
        {
            if (DatabaseManager.Instance == null)
                return;

            foreach (var data in DatabaseManager.Instance.GetTurretTrainDatas())
            {
                if (data != null && !string.IsNullOrEmpty(data.Id))
                    PlayerPrefs.DeleteKey(BEST_SURVIVAL_PREFIX + data.Id);
            }

            foreach (var data in DatabaseManager.Instance.GetRangeTrainDatas())
            {
                if (data != null && !string.IsNullOrEmpty(data.Id))
                    PlayerPrefs.DeleteKey(BEST_SURVIVAL_PREFIX + data.Id);
            }

            PlayerPrefs.Save();
        }
        #endregion

        #region Reset
        /// <summary>
        /// 사운드/햅틱 옵션을 기본값으로 되돌립니다.
        /// </summary>
        public void ResetOptions()
        {
            UserOptionDataParser.ResetAll();
            _userOptionData = UserOptionDataParser.Load();

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetBGMVolume(_userOptionData.BgmVolume);
                SoundManager.Instance.SetSFXVolume(_userOptionData.SfxVolume);
            }
        }

        /// <summary>
        /// 업적 진행/달성 데이터를 초기화합니다.
        /// </summary>
        [Button("업적 초기화")]
        public void ResetAchievements()
        {
            Cumic.Achievement.AchievementSaveData.Delete();
            Debug.Log("[UserDataManager] 업적 데이터가 초기화되었습니다.");
        }

        /// <summary>
        /// 모든 저장 데이터를 삭제합니다. (전체 초기화)
        /// </summary>
        public void ResetAllData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            _tutorialSaveData?.ResetAll();
            ClearDiscoveredCollections();
            _userOptionData = UserOptionDataParser.Load();
        }
        #endregion


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

        public bool HasSelectedChoice(string choiceId)
        {
            return _triChoiceData.ContainsKey(choiceId);
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

        public bool IsUpgradeMaxLevel(string upgradeId)
        {
            var upgradeData = DatabaseManager.Instance.GetUpgradeData(upgradeId);
            if (upgradeData == null || upgradeData.MaxUpgradeCount <= 0) return false;

            return GetUpgradeLevel(upgradeId) >= upgradeData.MaxUpgradeCount;
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

        private const float baseExp = 290f;
        private const float expPower = 1.6f;

        public float GetNextLevelUpExp()
        {
            // 레벨업 필요 경험치 = 290 × lv^1.6 (드랍=HP·스폰80% 기준, lv35 ≈ 55분 도달, 후반 정체 없음)
            return baseExp * Mathf.Pow(_currentLevel, expPower);
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
