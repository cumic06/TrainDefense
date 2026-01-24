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
        private Dictionary<string, int> _triChoiceData = new();
        
        [ShowInInspector]
        private Dictionary<string, int> _upgradeLevels = new();

        private int _coin;
        private int _currentExp;
        private int _currentLevel = 1;
        public int Coin => _coin;
        public float ExpPercent => _currentExp / GetNextLevelUpExp();
        public int CurrentLevel => _currentLevel;

        private void Start()
        {
            DontDestroyOnLoad(gameObject);

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
        }

        private void UnsubscribeEvents()
        {
            GameEventSystem.Unsubscribe<AddExpEvent>(AddExp);
            GameEventSystem.Unsubscribe<TriChoiceSelectEvent>(AddTriChoiceData);
            GameEventSystem.Unsubscribe<ChangeCoinUIEvent>(ChangeCoin);
            GameEventSystem.Unsubscribe<BuyShopItemEvent>(BuyShopItem);
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

        private const float baseExp = 100f;
        private const float powFactor = 1.8f;
        private const float expMultiplier = 1.05f;

        public float GetNextLevelUpExp()
        {
            return baseExp * Mathf.Pow(_currentLevel, powFactor) * Mathf.Pow(expMultiplier, _currentLevel);
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