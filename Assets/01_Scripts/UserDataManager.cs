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
        // 전투 중 오른 레벨 중 아직 카드를 고르지 않은 횟수. 역 도착 때 몰아서 고른다.
        private int _pendingLevelUpCount;
        private UserOptionData _userOptionData = new();
        public int Coin => _coin;
        public float ExpPercent => _currentExp / GetNextLevelUpExp();
        public int CurrentExp => _currentExp;
        public int CurrentLevel => _currentLevel;
        public int PendingLevelUpCount => _pendingLevelUpCount;
        public bool IsLobby { get; private set; }
        public bool IsHapticEnabled => _userOptionData == null || _userOptionData.IsHapticEnabled;
        public bool IsCameraShakeEnabled => _userOptionData == null || _userOptionData.IsCameraShakeEnabled;
        public int ColorblindType => _userOptionData == null ? 0 : _userOptionData.ColorblindType;
        public UserOptionData UserOptionData => _userOptionData;
        public TutorialSaveData TutorialSaveData => _tutorialSaveData;

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
            GameEventSystem.Subscribe<MonsterSpawnedEvent>(OnMonsterSpawned);
            GameEventSystem.Subscribe<TrainSpawnedEvent>(OnTrainSpawned);
            GameEventSystem.Subscribe<GameEnterEvent>(OnGameEnter);
        }

        private void UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<AddExpEvent>(AddExp);
            GameEventSystem.Unsubscribe<TriChoiceSelectEvent>(AddTriChoiceData);
            GameEventSystem.Unsubscribe<ChangeCoinUIEvent>(ChangeCoin);
            GameEventSystem.Unsubscribe<MonsterSpawnedEvent>(OnMonsterSpawned);
            GameEventSystem.Unsubscribe<TrainSpawnedEvent>(OnTrainSpawned);
            GameEventSystem.Unsubscribe<GameEnterEvent>(OnGameEnter);
        }

        private void OnGameEnter(GameEnterEvent gameEnterEvent)
        {
            _coin = 0;
            _currentExp = 0;
            _currentLevel = 1;
            _pendingLevelUpCount = 0;
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

        public void SetCameraShakeEnabled(bool enabled)
        {
            _userOptionData ??= new UserOptionData();
            _userOptionData.IsCameraShakeEnabled = enabled;
            UserOptionDataParser.Save(_userOptionData);
        }

        public void SetColorblindType(int type)
        {
            _userOptionData ??= new UserOptionData();
            _userOptionData.ColorblindType = type;
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
            int exp = addExpEvent.Exp;

            // 스킬 트리 경험치 획득 증가 (%)
            if (SkillTreeManager.Instance != null)
                exp = Mathf.RoundToInt(exp * (1f + SkillTreeManager.Instance.GetValue(TrainDefense.Game.Datas.SkillTreePassiveType.ExpGain) / 100f));

            _currentExp += exp;

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
            _pendingLevelUpCount++;
        }

        /// <summary>역에서 레벨업 카드를 골랐을 때(또는 고를 카드가 없어 넘겼을 때) 밀린 횟수를 줄인다.</summary>
        public void ConsumePendingLevelUps(int count)
        {
            _pendingLevelUpCount = Mathf.Max(0, _pendingLevelUpCount - count);
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

        #region Run Save
        /// <summary>런 세이브용 — 삼중택일 선택 횟수 스냅샷.</summary>
        public IReadOnlyDictionary<string, int> GetTriChoiceCounts() => _triChoiceData;

        /// <summary>런 세이브용 — 상점 업그레이드 레벨 스냅샷.</summary>
        public IReadOnlyDictionary<string, int> GetUpgradeLevels() => _upgradeLevels;

        /// <summary>
        /// 이어하기 복원 — 한 판치 진행도를 저장 당시 값으로 되돌린다.
        /// GameEnterEvent가 모두 처리돼 값이 0으로 리셋된 뒤에 호출되는 것을 전제로 한다.
        /// </summary>
        public void RestoreRunState(
            int coin,
            int currentExp,
            int currentLevel,
            int pendingLevelUpCount,
            Dictionary<string, int> triChoiceCounts,
            Dictionary<string, int> upgradeLevels)
        {
            int beforeCoin = _coin;
            _coin = coin;
            _currentExp = currentExp;
            _currentLevel = Mathf.Max(1, currentLevel);
            _pendingLevelUpCount = Mathf.Max(0, pendingLevelUpCount);


            _triChoiceData.Clear();
            if (triChoiceCounts != null)
            {
                foreach (var pair in triChoiceCounts)
                    _triChoiceData[pair.Key] = pair.Value;
            }

            _upgradeLevels.Clear();
            if (upgradeLevels != null)
            {
                foreach (var pair in upgradeLevels)
                    _upgradeLevels[pair.Key] = pair.Value;
            }

            // 코인 HUD는 이벤트로만 갱신되므로 복원값으로 한 번 흘려준다. (차감이 아니라 표시 동기화)
            GameEventSystem.Publish(new ChangeCoinUIEvent(beforeCoin, _coin));
        }
        #endregion

        // 필요 경험치 = FIXED_EXP + BASE_EXP × EXP_GROWTH_PER_LEVEL^(lv-1)
        // 초반엔 고정분이 커서 레벨이 빨리 오르고(역당 1~2회), 후반엔 늘어나는 부분이 커져 역당 1회 남짓으로 수렴한다.
        // 배율은 봇 9판 실측 구간 수입(역 11~13에서 정체) 기준으로, 수입이 20% 적어도 모든 역에서 1회 이상 오르는 값.
        private const float FIXED_EXP = 550f;
        private const float BASE_EXP = 500f;
        private const float EXP_GROWTH_PER_LEVEL = 1.14f;

        public float GetNextLevelUpExp()
        {
            return GetNextLevelUpExp(_currentLevel);
        }

        // 특정 레벨 기준 필요 경험치. 게임 진입 표시처럼 _currentLevel 리셋 타이밍에 의존하면 안 되는 곳에서 사용.
        public float GetNextLevelUpExp(int level)
        {
            // 역 20개 판에서 레벨업 약 27회: 첫 역 1회, 역 2~10은 1~2회, 역 11부터 1회 남짓. 모든 역에서 1회 이상(규칙이 아니라 곡선으로).
            // 거듭제곱(lv^n)은 역마다 늘어나는 수입을 못 따라가 후반에 몰리거나(n 작을 때) 뜸해져서(n 클 때) 배율 곡선으로 바꿨다.
            return FIXED_EXP + BASE_EXP * Mathf.Pow(EXP_GROWTH_PER_LEVEL, level - 1);
        }
        #endregion

        /// <summary>
        /// 코인 지출 단일 경로. 잔액이 충분할 때만 ChangeCoinUIEvent로 차감하고 true를 반환한다.
        /// (UI가 코인 이벤트를 직접 발행해 검증 없이 음수 코인을 만들 수 없도록 여기로 모은다)
        /// </summary>
        public bool TrySpendCoin(int cost)
        {
            if (cost < 0 || _coin < cost)
                return false;

            GameEventSystem.Publish(new ChangeCoinUIEvent(_coin, _coin - cost));

            return true;
        }

    }
}
