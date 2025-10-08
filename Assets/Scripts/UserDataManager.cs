using System.Collections.Generic;
using TrainDefense.Game.Events;
using Cumic.Events;
using Cumic;
using UnityEngine;

public class UserDataManager : Singleton<UserDataManager>
{
    private Dictionary<string, int> _triChoiceData = new();
    private int _coin;
    private int _currentExp;
    private int _currentLevel = 1;
    public int Coin => _coin;
    public float ExpPercent => _currentExp / GetNextLevelUpExp();
    public int CurrentLevel => _currentLevel;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        GameEventSystem.Subscribe<AddExpEvent>(AddExp);
        GameEventSystem.Subscribe<TriChoiceSelectEvent>(AddTriChoiceData);
        GameEventSystem.Subscribe<AddCoinEvent>(AddMoney);
        _coin = 0;
    }

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

    private void AddMoney(AddCoinEvent addCoinEvent)
    {
        _coin += addCoinEvent.Coin;
    }

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

    private const float baseExp = 10f;
    private const float powFactor = 1.8f;
    private const float expMultiplier = 1.05f;

    public float GetNextLevelUpExp()
    {
        return baseExp * Mathf.Pow(_currentLevel, powFactor) * Mathf.Pow(expMultiplier, _currentLevel);
    }
}